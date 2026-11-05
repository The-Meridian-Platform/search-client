using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MeridianClient.Models;
using MeridianClient.ViewModels;

namespace MeridianClient.Services;

public class GraphDataMapper
{
    public static string DetectTargetDomain(FindingItemViewModel finding)
    {
        if (finding.IsDisposableEmail && finding.DisposableEmailData != null)
        {
            if (!string.IsNullOrWhiteSpace(finding.DisposableEmailData.Email)) return finding.DisposableEmailData.Email.Trim();
            if (!string.IsNullOrWhiteSpace(finding.DisposableEmailData.Domain)) return finding.DisposableEmailData.Domain.Trim();
        }

        if (finding.IsMxEmail && finding.MxEmailData != null && !string.IsNullOrWhiteSpace(finding.MxEmailData.EmailDomain))
            return finding.MxEmailData.EmailDomain.Trim();

        if (finding.IsGitHubEmail && finding.GitHubEmailData != null && !string.IsNullOrWhiteSpace(finding.GitHubEmailData.Username))
            return finding.GitHubEmailData.Username.Trim();

        if (finding.IsGravatarEmail && finding.GravatarEmailData != null)
        {
            if (!string.IsNullOrWhiteSpace(finding.GravatarEmailData.Username)) return finding.GravatarEmailData.Username.Trim();
            if (!string.IsNullOrWhiteSpace(finding.GravatarEmailData.DisplayName)) return finding.GravatarEmailData.DisplayName.Trim();
        }

        if (finding.IsUsernameGithub && finding.UsernameGithubData != null && !string.IsNullOrWhiteSpace(finding.UsernameGithubData.Username))
            return finding.UsernameGithubData.Username.Trim();

        if (finding.IsUsernameTelegram && finding.UsernameTelegramData != null && !string.IsNullOrWhiteSpace(finding.UsernameTelegramData.Username))
            return finding.UsernameTelegramData.Username.Trim();

        if (finding.IsUsernamePresence && finding.UsernamePresenceData != null && !string.IsNullOrWhiteSpace(finding.UsernamePresenceData.Username))
            return finding.UsernamePresenceData.Username.Trim();

        if (finding.IsIpGeo && finding.IpGeoData != null && !string.IsNullOrWhiteSpace(finding.IpGeoData.Ip))
            return finding.IpGeoData.Ip.Trim();

        if (finding.IsIpReverseDns && finding.IpReverseDnsData != null && !string.IsNullOrWhiteSpace(finding.IpReverseDnsData.Ip))
            return finding.IpReverseDnsData.Ip.Trim();

        if (finding.IsIpShodan && finding.IpShodanData != null && !string.IsNullOrWhiteSpace(finding.IpShodanData.Ip))
            return finding.IpShodanData.Ip.Trim();

        if (finding.IsWhois && finding.WhoisData != null && !string.IsNullOrWhiteSpace(finding.WhoisData.DomainName))
            return finding.WhoisData.DomainName.Trim();

        if (finding.IsDns && finding.DnsData != null && !string.IsNullOrWhiteSpace(finding.DnsData.Name))
            return finding.DnsData.Name.Trim();

        if (finding.IsSubdomain && finding.SubdomainData != null && !string.IsNullOrWhiteSpace(finding.SubdomainData.Subdomain))
            return finding.SubdomainData.Subdomain.Trim();

        if (finding.IsHttpTech && finding.HttpTechData != null && !string.IsNullOrWhiteSpace(finding.HttpTechData.Url))
        {
            if (Uri.TryCreate(finding.HttpTechData.Url, UriKind.Absolute, out var uri))
                return uri.Host;
        }

        return string.Empty;
    }

    private static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return string.Empty;
        return host.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static string NormalizeUsername(string user)
    {
        if (string.IsNullOrWhiteSpace(user)) return string.Empty;
        return user.Trim().TrimStart('@').ToLowerInvariant();
    }

    public static void MapFindingToGraph(
        FindingItemViewModel finding,
        string targetValue,
        Dictionary<string, GraphNode> nodes,
        List<GraphEdge> edges,
        Random random,
        double centerX,
        double centerY,
        bool mergeSharedEntities = false)
    {
        string effectiveTarget = !string.IsNullOrWhiteSpace(targetValue)
            ? targetValue.Trim()
            : DetectTargetDomain(finding);

        if (string.IsNullOrWhiteSpace(effectiveTarget))
        {
            effectiveTarget = "Цель";
        }

        string normalizedTarget = effectiveTarget.ToLowerInvariant().Trim();
        string targetId = $"target:{normalizedTarget}";
        if (!nodes.TryGetValue(targetId, out var targetNode))
        {
            targetNode = new GraphNode(
                targetId,
                effectiveTarget,
                "Основная цель",
                GraphNodeType.Target,
                null,
                new Dictionary<string, string> { { "Цель", effectiveTarget } }
            )
            {
                X = centerX,
                Y = centerY,
                IsPinned = true
            };
            nodes[targetId] = targetNode;
        }

        string QualifyId(string id, GraphNode parent)
        {
            if (mergeSharedEntities) return id;
            if (id.StartsWith("target:")) return id;
            return $"{parent.Id}:{id}";
        }

        GraphNode GetOrCreateNode(string id, string label, string subLabel, GraphNodeType type, GraphNode parent, string? rawJson = null, Dictionary<string, string>? props = null)
        {
            string actualId = QualifyId(id, parent);
            if (nodes.TryGetValue(actualId, out var existing))
            {
                existing.MergeProperties(props);
                return existing;
            }

            double angle = random.NextDouble() * Math.PI * 2;
            double dist = 75 + random.Next(45);
            var node = new GraphNode(actualId, label, subLabel, type, rawJson, props)
            {
                X = parent.X + Math.Cos(angle) * dist,
                Y = parent.Y + Math.Sin(angle) * dist,
                ExpansionDepth = parent.ExpansionDepth + 1
            };
            nodes[actualId] = node;
            return node;
        }

        void AddEdgeIfMissing(GraphNode source, GraphNode target, string label)
        {
            if (source == target) return;
            string edgeId = $"{source.Id}->{target.Id}:{label}";
            if (!edges.Exists(e => e.Id == edgeId))
            {
                edges.Add(new GraphEdge(source, target, label));
            }
        }

        if (finding.IsDns && finding.DnsData != null)
        {
            var dns = finding.DnsData;
            string host = string.IsNullOrWhiteSpace(dns.Name) ? effectiveTarget : dns.Name.Trim();
            string normHost = NormalizeHost(host);
            GraphNode hostNode;

            if (normHost.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                hostNode = targetNode;
            }
            else
            {
                string hostId = $"host:{normHost}";
                bool isSub = normHost.EndsWith("." + normalizedTarget, StringComparison.OrdinalIgnoreCase);
                hostNode = GetOrCreateNode(hostId, host.TrimEnd('.'), isSub ? "Поддомен" : "Хост", GraphNodeType.Subdomain, targetNode);
                AddEdgeIfMissing(targetNode, hostNode, isSub ? "Поддомен" : "Хост цели");
            }

            var props = new Dictionary<string, string>
            {
                { "Тип записи", dns.RecordType },
                { "Значение", dns.Value },
                { "TTL", dns.Ttl?.ToString() ?? "-" }
            };

            switch (dns.RecordType.ToUpperInvariant())
            {
                case "A":
                case "AAAA":
                    var ipNode = GetOrCreateNode($"ip:{dns.Value.Trim().ToLowerInvariant()}", dns.Value.Trim(), $"{dns.RecordType} запись", GraphNodeType.IpAddress, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, ipNode, $"Resolves ({dns.RecordType})");
                    break;

                case "NS":
                    string normNs = NormalizeHost(dns.Value);
                    var nsNode = GetOrCreateNode($"ns:{normNs}", dns.Value.TrimEnd('.'), "DNS сервер", GraphNodeType.NameServer, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, nsNode, "NameServer");
                    break;

                case "MX":
                    string mxHost = dns.Value.Trim();
                    int? mxPrio = dns.Priority;
                    var mxParts = mxHost.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (mxParts.Length >= 2 && int.TryParse(mxParts[0], out var parsedPrio))
                    {
                        mxPrio ??= parsedPrio;
                        mxHost = mxParts[1];
                    }
                    mxHost = NormalizeHost(mxHost);
                    props["Хост"] = mxHost;
                    if (mxPrio.HasValue)
                    {
                        props["Приоритет"] = mxPrio.Value.ToString();
                        props["Значение"] = mxHost;
                    }

                    var mxNode = GetOrCreateNode($"mail:{mxHost}", mxHost, mxPrio.HasValue ? $"MX (Приоритет {mxPrio})" : "Почтовый шлюз", GraphNodeType.MailServer, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, mxNode, mxPrio.HasValue ? $"MX [{mxPrio}]" : "Mail Exchanger");
                    break;

                case "CNAME":
                    string normCname = NormalizeHost(dns.Value);
                    var cnameNode = GetOrCreateNode($"host:{normCname}", dns.Value.TrimEnd('.'), "Псевдоним CNAME", GraphNodeType.Subdomain, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, cnameNode, "CNAME");
                    break;

                default:
                    var otherDns = GetOrCreateNode($"dns:{dns.RecordType}:{dns.Value.Trim()}", $"{dns.RecordType}: {dns.Value.Trim()}", dns.RecordType, GraphNodeType.Other, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, otherDns, dns.RecordType);
                    break;
            }
        }

        if (finding.IsSubdomain && finding.SubdomainData != null)
        {
            var sub = finding.SubdomainData;
            string subName = sub.Subdomain.Trim();
            string normSub = NormalizeHost(subName);
            var props = new Dictionary<string, string>
            {
                { "Поддомен", subName },
                { "Сертификат / Issuer", sub.Issuer },
                { "Действителен с", sub.ValidFrom },
                { "Действителен до", sub.ValidTo }
            };

            var subNode = GetOrCreateNode($"host:{normSub}", subName.TrimEnd('.'), "Поддомен", GraphNodeType.Subdomain, targetNode, finding.RawJson, props);
            AddEdgeIfMissing(targetNode, subNode, "Поддомен");
        }

        if (finding.IsWhois && finding.WhoisData != null)
        {
            var whois = finding.WhoisData;
            string whoisDomain = string.IsNullOrWhiteSpace(whois.DomainName) ? effectiveTarget : whois.DomainName.Trim();
            string normWhoisDomain = NormalizeHost(whoisDomain);
            GraphNode whoisRoot = normWhoisDomain.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase)
                ? targetNode
                : GetOrCreateNode($"host:{normWhoisDomain}", whoisDomain.TrimEnd('.'), "Домен", GraphNodeType.Subdomain, targetNode);

            if (whoisRoot != targetNode && normWhoisDomain.EndsWith("." + normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                AddEdgeIfMissing(targetNode, whoisRoot, "Домен");
            }

            var props = new Dictionary<string, string>
            {
                { "Домен", whois.DomainName },
                { "Регистратор", whois.Registrar },
                { "Организация", whois.RegistrantOrg },
                { "Страна", whois.RegistrantCountry },
                { "Создан", whois.CreatedDate },
                { "Истекает", whois.ExpiresDate },
                { "Статус", whois.Status }
            };

            if (!string.IsNullOrWhiteSpace(whois.Registrar))
            {
                var regNode = GetOrCreateNode($"registrar:{whois.Registrar.Trim().ToLowerInvariant()}", whois.Registrar.Trim(), "Регистратор", GraphNodeType.Registrar, whoisRoot, finding.RawJson, props);
                AddEdgeIfMissing(whoisRoot, regNode, "Регистратор");
            }

            if (!string.IsNullOrWhiteSpace(whois.RegistrantOrg))
            {
                var orgNode = GetOrCreateNode($"org:{whois.RegistrantOrg.Trim().ToLowerInvariant()}", whois.RegistrantOrg.Trim(), "Организация", GraphNodeType.Registrar, whoisRoot, finding.RawJson, props);
                AddEdgeIfMissing(whoisRoot, orgNode, "Владелец");
            }

            foreach (var ns in whois.NameServers)
            {
                if (!string.IsNullOrWhiteSpace(ns))
                {
                    string normNs = NormalizeHost(ns);
                    var nsNode = GetOrCreateNode($"ns:{normNs}", ns.Trim().TrimEnd('.'), "Whois NS", GraphNodeType.NameServer, whoisRoot, null, new Dictionary<string, string> { { "NS", ns } });
                    AddEdgeIfMissing(whoisRoot, nsNode, "Делегирован NS");
                }
            }
        }

        if (finding.IsHttpTech && finding.HttpTechData != null)
        {
            var tech = finding.HttpTechData;
            string host = effectiveTarget;
            if (Uri.TryCreate(tech.Url, UriKind.Absolute, out var uri))
            {
                host = uri.Host;
            }

            string normHost = NormalizeHost(host);
            GraphNode techHostNode;
            if (normHost.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                techHostNode = targetNode;
            }
            else
            {
                bool isSub = normHost.EndsWith("." + normalizedTarget, StringComparison.OrdinalIgnoreCase);
                techHostNode = GetOrCreateNode($"host:{normHost}", host.TrimEnd('.'), isSub ? "Поддомен" : "Хост", GraphNodeType.Subdomain, targetNode);
                AddEdgeIfMissing(targetNode, techHostNode, isSub ? "Веб-хост" : "Хост цели");
            }

            var props = new Dictionary<string, string>
            {
                { "URL", tech.Url },
                { "Статус", tech.StatusBadgeText },
                { "Сервер", tech.Server },
                { "Заголовок", tech.Title },
                { "Powered By", tech.PoweredBy },
                { "Content Type", tech.ContentType }
            };

            if (!string.IsNullOrWhiteSpace(tech.Server))
            {
                var serverNode = GetOrCreateNode($"tech:{tech.Server.Trim().ToLowerInvariant()}", tech.Server.Trim(), "Веб-сервер", GraphNodeType.Technology, techHostNode, finding.RawJson, props);
                AddEdgeIfMissing(techHostNode, serverNode, "Сервер");
            }

            if (!string.IsNullOrWhiteSpace(tech.PoweredBy))
            {
                var poweredNode = GetOrCreateNode($"tech:{tech.PoweredBy.Trim().ToLowerInvariant()}", tech.PoweredBy.Trim(), "Фреймворк", GraphNodeType.Technology, techHostNode, finding.RawJson, props);
                AddEdgeIfMissing(techHostNode, poweredNode, "Стек");
            }

            if (!string.IsNullOrWhiteSpace(tech.Title))
            {
                var titleNode = GetOrCreateNode($"title:{tech.Title.Trim().ToLowerInvariant()}", tech.Title.Trim(), "Веб-страница", GraphNodeType.Other, techHostNode, finding.RawJson, props);
                AddEdgeIfMissing(techHostNode, titleNode, "Заголовок HTML");
            }
        }

        if (finding.IsGitHubEmail && finding.GitHubEmailData != null)
        {
            var gh = finding.GitHubEmailData;
            string username = !string.IsNullOrWhiteSpace(gh.Username) ? gh.Username.Trim() : "user";
            string normUser = NormalizeUsername(username);
            string userLabel = $"@{normUser}";
            var userNode = GetOrCreateNode($"user:{normUser}", userLabel, "Пользователь", GraphNodeType.Profile, targetNode);
            AddEdgeIfMissing(targetNode, userNode, "Обнаружен никнейм");

            var props = new Dictionary<string, string>
            {
                { "Платформа", "GitHub" },
                { "Никнейм", username },
                { "Username", username },
                { "Тип аккаунта", gh.AccountType },
                { "Профиль", gh.ProfileUrl },
                { "ID", gh.Id.ToString() }
            };

            var ghNode = GetOrCreateNode($"profile:github:{normUser}", gh.FormattedUsername, $"GitHub ({gh.AccountType})", GraphNodeType.Profile, userNode, finding.RawJson, props);
            AddEdgeIfMissing(userNode, ghNode, "GitHub профиль");
        }

        if (finding.IsGravatarEmail && finding.GravatarEmailData != null)
        {
            var grav = finding.GravatarEmailData;
            string normUser = !string.IsNullOrWhiteSpace(grav.Username)
                ? NormalizeUsername(grav.Username)
                : NormalizeUsername(grav.DisplayName);

            GraphNode parentUserNode = targetNode;
            if (!string.IsNullOrWhiteSpace(normUser))
            {
                parentUserNode = GetOrCreateNode($"user:{normUser}", $"@{normUser}", "Пользователь", GraphNodeType.Profile, targetNode);
                AddEdgeIfMissing(targetNode, parentUserNode, "Обнаружен никнейм");
            }

            string label = !string.IsNullOrWhiteSpace(grav.DisplayName)
                ? grav.DisplayName
                : (!string.IsNullOrWhiteSpace(grav.Username) ? grav.Username : "Gravatar User");
            string sub = !string.IsNullOrWhiteSpace(grav.Location)
                ? $"Gravatar • {grav.Location}"
                : "Gravatar профиль";

            var props = new Dictionary<string, string>
            {
                { "Платформа", "Gravatar" },
                { "Отображаемое имя", grav.DisplayName },
                { "Никнейм", grav.Username },
                { "Username", grav.Username },
                { "Локация", grav.Location },
                { "О себе", grav.AboutMe },
                { "Профиль", grav.ProfileUrl }
            };

            string nodeId = !string.IsNullOrWhiteSpace(normUser) ? $"profile:gravatar:{normUser}" : $"gravatar:{label.ToLowerInvariant()}";
            var gravNode = GetOrCreateNode(nodeId, label, sub, GraphNodeType.Profile, parentUserNode, finding.RawJson, props);
            AddEdgeIfMissing(parentUserNode, gravNode, "Gravatar профиль");
        }

        if (finding.IsUsernameGithub && finding.UsernameGithubData != null)
        {
            var gh = finding.UsernameGithubData;
            string username = !string.IsNullOrWhiteSpace(gh.Username) ? gh.Username.Trim() : effectiveTarget;
            string normUser = NormalizeUsername(username);
            var userNode = GetOrCreateNode($"user:{normUser}", $"@{normUser}", "Пользователь", GraphNodeType.Profile, targetNode);
            if (userNode != targetNode)
            {
                AddEdgeIfMissing(targetNode, userNode, "Никнейм");
            }

            var props = new Dictionary<string, string>
            {
                { "Платформа", "GitHub" },
                { "Username", gh.Username },
                { "Имя", gh.Name ?? string.Empty },
                { "Компания", gh.Company ?? string.Empty },
                { "Локация", gh.Location ?? string.Empty },
                { "Био", gh.Bio ?? string.Empty },
                { "Репозитории", gh.PublicRepos.ToString() },
                { "Подписчики", gh.Followers.ToString() },
                { "Twitter", gh.Twitter ?? string.Empty },
                { "Профиль", gh.ProfileUrl }
            };

            string label = !string.IsNullOrWhiteSpace(gh.Name) ? gh.Name : $"GitHub (@{normUser})";
            string sub = $"{gh.PublicRepos} репозиториев • {gh.Followers} фолловеров";
            var ghNode = GetOrCreateNode($"profile:github:{normUser}", label, sub, GraphNodeType.Profile, userNode, finding.RawJson, props);
            AddEdgeIfMissing(userNode, ghNode, "GitHub");
        }

        if (finding.IsUsernameTelegram && finding.UsernameTelegramData != null)
        {
            var tg = finding.UsernameTelegramData;
            string username = !string.IsNullOrWhiteSpace(tg.Username) ? tg.Username.Trim() : effectiveTarget;
            string normUser = NormalizeUsername(username);
            var userNode = GetOrCreateNode($"user:{normUser}", $"@{normUser}", "Пользователь", GraphNodeType.Profile, targetNode);
            if (userNode != targetNode)
            {
                AddEdgeIfMissing(targetNode, userNode, "Никнейм");
            }

            var props = new Dictionary<string, string>
            {
                { "Платформа", "Telegram" },
                { "Username", tg.Username },
                { "Заголовок", tg.Title },
                { "Описание", tg.Description ?? string.Empty },
                { "URL", tg.Url }
            };

            string label = !string.IsNullOrWhiteSpace(tg.Title) ? tg.Title : $"Telegram (@{normUser})";
            string sub = "Telegram профиль";
            var tgNode = GetOrCreateNode($"profile:telegram:{normUser}", label, sub, GraphNodeType.Profile, userNode, finding.RawJson, props);
            AddEdgeIfMissing(userNode, tgNode, "Telegram");
        }

        if (finding.IsUsernamePresence && finding.UsernamePresenceData != null)
        {
            var pres = finding.UsernamePresenceData;
            string username = !string.IsNullOrWhiteSpace(pres.Username) ? pres.Username.Trim() : effectiveTarget;
            string normUser = NormalizeUsername(username);
            var userNode = GetOrCreateNode($"user:{normUser}", $"@{normUser}", "Пользователь", GraphNodeType.Profile, targetNode);
            if (userNode != targetNode)
            {
                AddEdgeIfMissing(targetNode, userNode, "Никнейм");
            }

            string platform = !string.IsNullOrWhiteSpace(pres.Platform) ? pres.Platform.Trim() : "Service";
            string normPlatform = platform.ToLowerInvariant();
            var props = new Dictionary<string, string>
            {
                { "Платформа", platform },
                { "Username", pres.Username },
                { "Статус", pres.Exists ? "Обнаружен" : "Не найден" },
                { "URL профиля", pres.Url },
                { "Детали", pres.Details ?? string.Empty }
            };

            string label = $"{platform} (@{normUser})";
            string sub = pres.Exists ? "Профиль подтвержден" : "Сервисный аккаунт";
            var presNode = GetOrCreateNode($"profile:{normPlatform}:{normUser}", label, sub, GraphNodeType.Profile, userNode, finding.RawJson, props);
            AddEdgeIfMissing(userNode, presNode, platform);
        }

        if (finding.IsDisposableEmail && finding.DisposableEmailData != null)
        {
            var disp = finding.DisposableEmailData;
            string label = disp.IsDisposable ? "Временная почта" : "Легитимная почта";
            string sub = !string.IsNullOrWhiteSpace(disp.Domain) ? disp.Domain : "Статус домена";
            var nodeType = disp.IsDisposable ? GraphNodeType.Warning : GraphNodeType.EmailVerified;

            var props = new Dictionary<string, string>
            {
                { "Статус", disp.IsDisposable ? "Одноразовый ящик (Burner)" : "Легитимный домен" },
                { "Домен", disp.Domain },
                { "Категория", disp.Category },
                { "Email", disp.Email }
            };

            string normDom = !string.IsNullOrWhiteSpace(disp.Domain) ? NormalizeHost(disp.Domain) : normalizedTarget;
            string nodeId = $"disposable:{normDom}";
            var dispNode = GetOrCreateNode(nodeId, label, sub, nodeType, targetNode, finding.RawJson, props);
            AddEdgeIfMissing(targetNode, dispNode, disp.IsDisposable ? "Одноразовая почта" : "Легитимная почта");
        }

        if (finding.IsMxEmail && finding.MxEmailData != null)
        {
            var mx = finding.MxEmailData;
            string provName = !string.IsNullOrWhiteSpace(mx.MailProviderName) ? mx.MailProviderName.Trim() : "Почтовый сервер";
            string sub = mx.IsEnterprise ? "Enterprise почта" : "Почтовый шлюз";

            var props = new Dictionary<string, string>
            {
                { "Провайдер", provName },
                { "Домен", mx.EmailDomain },
                { "Enterprise", mx.IsEnterprise ? "Да" : "Нет" },
                { "SPF защита", mx.HasSpfProtection ? "Активна" : "Не обнаружена" },
                { "MX Серверы", mx.FormattedMxServers }
            };

            string normProv = NormalizeHost(provName);
            string provNodeId = $"mail:{normProv}";
            var provNode = GetOrCreateNode(provNodeId, provName, sub, GraphNodeType.MailServer, targetNode, finding.RawJson, props);
            AddEdgeIfMissing(targetNode, provNode, "Почтовый провайдер");

            foreach (var server in mx.MxServers)
            {
                if (!string.IsNullOrWhiteSpace(server))
                {
                    string rawServer = server.Trim();
                    string cleanHost = Regex.Replace(rawServer, @"^\d+\s+", "").TrimEnd('.');
                    string normServerHost = NormalizeHost(cleanHost);

                    if (normServerHost.Equals(normProv, StringComparison.OrdinalIgnoreCase))
                    {
                        provNode.SetProperty("MX Сервер", cleanHost);
                        continue;
                    }

                    var srvProps = new Dictionary<string, string>
                    {
                        { "Хост", cleanHost },
                        { "Запись MX", rawServer }
                    };
                    var srvNode = GetOrCreateNode($"mail:{normServerHost}", cleanHost, "MX сервер", GraphNodeType.MailServer, provNode, null, srvProps);
                    AddEdgeIfMissing(provNode, srvNode, "MX шлюз");
                }
            }
        }

        if (finding.IsIpGeo && finding.IpGeoData != null)
        {
            var geo = finding.IpGeoData;
            string ip = !string.IsNullOrWhiteSpace(geo.Ip) ? geo.Ip.Trim() : effectiveTarget;
            string ipNodeId = $"ip:{ip.ToLowerInvariant()}";

            var props = new Dictionary<string, string>
            {
                { "IP", ip },
                { "Страна", geo.Country },
                { "Код страны", geo.CountryCode },
                { "Город", geo.City },
                { "Регион", geo.Region },
                { "Координаты", geo.Coordinates },
                { "Таймзона", geo.Timezone },
                { "ASN", geo.Asn },
                { "Провайдер (ISP)", geo.Isp },
                { "Организация", geo.Org }
            };

            if (geo.IsHosting) props["Тип сети"] = "Datacenter / Hosting";
            else if (geo.IsProxy) props["Тип сети"] = "Proxy / VPN";
            else if (geo.IsMobile) props["Тип сети"] = "Mobile Network";

            string subLabel = !string.IsNullOrWhiteSpace(geo.FormattedLocation) ? geo.FormattedLocation : "IP геопозиция";
            GraphNode ipNode;
            if (ip.Equals(effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                ipNode = targetNode;
                ipNode.SubLabel = !string.IsNullOrWhiteSpace(geo.Isp)
                    ? $"{geo.FormattedLocation} • {geo.Isp}"
                    : geo.FormattedLocation;
                ipNode.MergeProperties(props);
            }
            else
            {
                ipNode = GetOrCreateNode(ipNodeId, ip, subLabel, GraphNodeType.IpAddress, targetNode, finding.RawJson, props);
                AddEdgeIfMissing(targetNode, ipNode, "IP адрес");
            }

            if (!string.IsNullOrWhiteSpace(geo.Country) || !string.IsNullOrWhiteSpace(geo.City))
            {
                string locCode = !string.IsNullOrWhiteSpace(geo.CountryCode) ? geo.CountryCode.ToLowerInvariant() : "loc";
                string locCity = !string.IsNullOrWhiteSpace(geo.City) ? geo.City.ToLowerInvariant() : "area";
                string locId = $"geo:{locCode}:{locCity}";
                string locLabel = geo.FormattedLocation;
                string locSub = geo.IsHosting ? "Datacenter / Hosting"
                    : (geo.IsProxy ? "Proxy / VPN"
                    : (geo.IsMobile ? "Mobile Network" : (!string.IsNullOrWhiteSpace(geo.Coordinates) ? geo.Coordinates : "Геолокация")));

                var locProps = new Dictionary<string, string>
                {
                    { "Локация", geo.FormattedLocation },
                    { "Страна", geo.Country },
                    { "Город", geo.City },
                    { "Регион", geo.Region },
                    { "Координаты", geo.Coordinates },
                    { "Таймзона", geo.Timezone }
                };
                if (geo.IsHosting) locProps["Тип сети"] = "Datacenter / Hosting";
                else if (geo.IsProxy) locProps["Тип сети"] = "Proxy / VPN";
                else if (geo.IsMobile) locProps["Тип сети"] = "Mobile Network";

                var locNode = GetOrCreateNode(locId, locLabel, locSub, GraphNodeType.Location, ipNode, finding.RawJson, locProps);
                AddEdgeIfMissing(ipNode, locNode, "Геолокация");
            }

            if (geo.HasAsn)
            {
                string asnClean = geo.Asn.Trim();
                string asnNodeId = $"asn:{asnClean.ToLowerInvariant()}";
                string asnSub = !string.IsNullOrWhiteSpace(geo.Isp) ? geo.Isp : (!string.IsNullOrWhiteSpace(geo.Org) ? geo.Org : "Автономная система");
                var asnProps = new Dictionary<string, string>
                {
                    { "ASN", asnClean },
                    { "Провайдер", geo.Isp },
                    { "Организация", geo.Org }
                };
                var asnNode = GetOrCreateNode(asnNodeId, asnClean, asnSub, GraphNodeType.Asn, ipNode, finding.RawJson, asnProps);
                AddEdgeIfMissing(ipNode, asnNode, "AS Сеть");
            }
        }

        if (finding.IsIpReverseDns && finding.IpReverseDnsData != null)
        {
            var rdns = finding.IpReverseDnsData;
            string ip = !string.IsNullOrWhiteSpace(rdns.Ip) ? rdns.Ip.Trim() : effectiveTarget;
            string ipNodeId = $"ip:{ip.ToLowerInvariant()}";

            var props = new Dictionary<string, string>
            {
                { "IP", ip },
                { "ARPA запрос", rdns.ArpaQuery },
                { "Количество хостов", rdns.HostnamesCount.ToString() }
            };

            GraphNode ipNode;
            if (ip.Equals(effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                ipNode = targetNode;
                ipNode.MergeProperties(props);
            }
            else
            {
                ipNode = GetOrCreateNode(ipNodeId, ip, "IP адрес", GraphNodeType.IpAddress, targetNode, finding.RawJson, props);
                AddEdgeIfMissing(targetNode, ipNode, "IP адрес");
            }

            foreach (var host in rdns.Hostnames)
            {
                if (!string.IsNullOrWhiteSpace(host))
                {
                    string cleanHost = host.Trim().TrimEnd('.');
                    string normHost = NormalizeHost(cleanHost);
                    string hostNodeId = $"host:{normHost}";
                    var hostProps = new Dictionary<string, string>
                    {
                        { "Хостнейм (PTR)", cleanHost },
                        { "IP", ip }
                    };
                    var hostNode = GetOrCreateNode(hostNodeId, cleanHost, "Reverse DNS (PTR)", GraphNodeType.Subdomain, ipNode, null, hostProps);
                    AddEdgeIfMissing(ipNode, hostNode, "Resolves to (PTR)");
                }
            }
        }

        if (finding.IsIpShodan && finding.IpShodanData != null)
        {
            var shodan = finding.IpShodanData;
            string ip = !string.IsNullOrWhiteSpace(shodan.Ip) ? shodan.Ip.Trim() : effectiveTarget;
            string ipNodeId = $"ip:{ip.ToLowerInvariant()}";

            var props = new Dictionary<string, string>
            {
                { "IP", ip },
                { "Открытые порты", shodan.FormattedPortsSummary },
                { "Теги", string.Join(", ", shodan.Tags) }
            };
            if (shodan.HasVulns)
            {
                props["Уязвимости (CVE)"] = string.Join(", ", shodan.Vulnerabilities);
            }

            string subLabel = shodan.HasVulns ? $"Shodan • {shodan.VulnsCount} CVE" : "Shodan InternetDB";
            GraphNode ipNode;
            if (ip.Equals(effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                ipNode = targetNode;
                ipNode.MergeProperties(props);
            }
            else
            {
                ipNode = GetOrCreateNode(ipNodeId, ip, subLabel, GraphNodeType.IpAddress, targetNode, finding.RawJson, props);
                AddEdgeIfMissing(targetNode, ipNode, "IP адрес");
            }

            foreach (var host in shodan.Hostnames)
            {
                if (!string.IsNullOrWhiteSpace(host))
                {
                    string cleanHost = host.Trim().TrimEnd('.');
                    string normHost = NormalizeHost(cleanHost);
                    string hostNodeId = $"host:{normHost}";
                    var hostProps = new Dictionary<string, string>
                    {
                        { "Хост Shodan", cleanHost },
                        { "IP", ip }
                    };
                    var hostNode = GetOrCreateNode(hostNodeId, cleanHost, "Shodan Hostname", GraphNodeType.Subdomain, ipNode, null, hostProps);
                    AddEdgeIfMissing(ipNode, hostNode, "Shodan Hostname");
                }
            }

            foreach (var port in shodan.Ports)
            {
                string srv = GetPortServiceName(port);
                string portLabel = $"{port} ({srv})";
                string portNodeId = $"port:{ip.ToLowerInvariant()}:{port}";
                var portProps = new Dictionary<string, string>
                {
                    { "Порт", port.ToString() },
                    { "Сервис", srv },
                    { "Протокол", "TCP" },
                    { "Цель IP", ip }
                };
                var portNode = GetOrCreateNode(portNodeId, portLabel, "Открытый порт", GraphNodeType.Port, ipNode, null, portProps);
                AddEdgeIfMissing(ipNode, portNode, $"Порт {port}");
            }

            foreach (var tag in shodan.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    string cleanTag = tag.Trim();
                    string tagNodeId = $"tag:{cleanTag.ToLowerInvariant()}";
                    var tagProps = new Dictionary<string, string>
                    {
                        { "Тег", cleanTag },
                        { "Цель IP", ip }
                    };
                    var tagNode = GetOrCreateNode(tagNodeId, $"#{cleanTag}", "Сервисный тег", GraphNodeType.Technology, ipNode, null, tagProps);
                    AddEdgeIfMissing(ipNode, tagNode, "Тег");
                }
            }

            foreach (var vuln in shodan.Vulnerabilities)
            {
                if (!string.IsNullOrWhiteSpace(vuln))
                {
                    string cve = vuln.Trim();
                    string vulnNodeId = $"vuln:{cve.ToLowerInvariant()}";
                    var vulnProps = new Dictionary<string, string>
                    {
                        { "Уязвимость", cve },
                        { "Цель IP", ip }
                    };
                    var vulnNode = GetOrCreateNode(vulnNodeId, cve, "Критическая уязвимость", GraphNodeType.Warning, ipNode, null, vulnProps);
                    AddEdgeIfMissing(ipNode, vulnNode, "Уязвимость");
                }
            }
        }
    }

    private static string GetPortServiceName(int port) => port switch
    {
        21 => "FTP",
        22 => "SSH",
        23 => "Telnet",
        25 => "SMTP",
        53 => "DNS",
        80 => "HTTP",
        110 => "POP3",
        143 => "IMAP",
        443 => "HTTPS",
        465 => "SMTPS",
        587 => "Submission",
        853 => "DoT",
        993 => "IMAPS",
        995 => "POP3S",
        1433 => "MSSQL",
        1521 => "Oracle",
        3306 => "MySQL",
        3389 => "RDP",
        5432 => "Postgres",
        5900 => "VNC",
        6379 => "Redis",
        8000 => "HTTP-Alt",
        8080 => "HTTP-Proxy",
        8443 => "HTTPS-Alt",
        8888 => "HTTP-Alt",
        9200 => "Elastic",
        27017 => "Mongo",
        _ => "TCP"
    };
}
