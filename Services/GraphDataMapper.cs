using System;
using System.Collections.Generic;
using MeridianClient.Models;
using MeridianClient.ViewModels;

namespace MeridianClient.Services;

public class GraphDataMapper
{
    public static string DetectTargetDomain(FindingItemViewModel finding)
    {
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

    public static void MapFindingToGraph(
        FindingItemViewModel finding,
        string targetValue,
        Dictionary<string, GraphNode> nodes,
        List<GraphEdge> edges,
        Random random,
        double centerX,
        double centerY)
    {
        string effectiveTarget = !string.IsNullOrWhiteSpace(targetValue)
            ? targetValue.Trim()
            : DetectTargetDomain(finding);

        if (string.IsNullOrWhiteSpace(effectiveTarget))
        {
            effectiveTarget = "Цель";
        }

        // 1. Ensure Target root node exists
        string targetId = $"target:{effectiveTarget.ToLowerInvariant().Trim()}";
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
                IsPinned = true // Keep root steady near center
            };
            nodes[targetId] = targetNode;
        }

        // Helper to get or create node around parent
        GraphNode GetOrCreateNode(string id, string label, string subLabel, GraphNodeType type, GraphNode parent, string? rawJson = null, Dictionary<string, string>? props = null)
        {
            if (nodes.TryGetValue(id, out var existing))
            {
                return existing;
            }

            // Spawn near parent with random jitter
            double angle = random.NextDouble() * Math.PI * 2;
            double dist = 75 + random.Next(45);
            var node = new GraphNode(id, label, subLabel, type, rawJson, props)
            {
                X = parent.X + Math.Cos(angle) * dist,
                Y = parent.Y + Math.Sin(angle) * dist
            };
            nodes[id] = node;
            return node;
        }

        void AddEdgeIfMissing(GraphNode source, GraphNode target, string label)
        {
            string edgeId = $"{source.Id}->{target.Id}:{label}";
            if (!edges.Exists(e => e.Id == edgeId))
            {
                edges.Add(new GraphEdge(source, target, label));
            }
        }

        // 2. Map DNS findings
        if (finding.IsDns && finding.DnsData != null)
        {
            var dns = finding.DnsData;
            string host = string.IsNullOrWhiteSpace(dns.Name) ? effectiveTarget : dns.Name.Trim();
            GraphNode hostNode;

            if (host.Equals(effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                hostNode = targetNode;
            }
            else if (host.EndsWith("." + effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                hostNode = GetOrCreateNode($"subdomain:{host.ToLowerInvariant()}", host, "Поддомен", GraphNodeType.Subdomain, targetNode);
                AddEdgeIfMissing(hostNode, targetNode, "Поддомен");
            }
            else
            {
                // Different host or secondary domain
                hostNode = GetOrCreateNode($"host:{host.ToLowerInvariant()}", host, "Хост", GraphNodeType.Subdomain, targetNode);
                if (host.Contains(effectiveTarget, StringComparison.OrdinalIgnoreCase))
                {
                    AddEdgeIfMissing(hostNode, targetNode, "Хост цели");
                }
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
                    var ipNode = GetOrCreateNode($"ip:{dns.Value}", dns.Value, $"{dns.RecordType} запись", GraphNodeType.IpAddress, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, ipNode, $"Resolves ({dns.RecordType})");
                    break;

                case "NS":
                    var nsNode = GetOrCreateNode($"ns:{dns.Value.ToLowerInvariant()}", dns.Value, "DNS сервер", GraphNodeType.NameServer, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, nsNode, "NameServer");
                    break;

                case "MX":
                    var mxNode = GetOrCreateNode($"mx:{dns.Value.ToLowerInvariant()}", dns.Value, "Почтовый шлюз", GraphNodeType.MailServer, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, mxNode, "Mail Exchanger");
                    break;

                case "CNAME":
                    var cnameNode = GetOrCreateNode($"host:{dns.Value.ToLowerInvariant()}", dns.Value, "Псевдоним CNAME", GraphNodeType.Subdomain, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, cnameNode, "CNAME");
                    break;

                default:
                    var otherDns = GetOrCreateNode($"dns:{dns.RecordType}:{dns.Value}", $"{dns.RecordType}: {dns.Value}", dns.RecordType, GraphNodeType.Other, hostNode, finding.RawJson, props);
                    AddEdgeIfMissing(hostNode, otherDns, dns.RecordType);
                    break;
            }
        }

        // 3. Map Subdomain findings
        if (finding.IsSubdomain && finding.SubdomainData != null)
        {
            var sub = finding.SubdomainData;
            string subName = sub.Subdomain.Trim();
            var props = new Dictionary<string, string>
            {
                { "Поддомен", subName },
                { "Сертификат / Issuer", sub.Issuer },
                { "Действителен с", sub.ValidFrom },
                { "Действителен до", sub.ValidTo }
            };

            var subNode = GetOrCreateNode($"subdomain:{subName.ToLowerInvariant()}", subName, "Поддомен", GraphNodeType.Subdomain, targetNode, finding.RawJson, props);
            AddEdgeIfMissing(subNode, targetNode, "Поддомен");
        }

        // 4. Map WHOIS findings
        if (finding.IsWhois && finding.WhoisData != null)
        {
            var whois = finding.WhoisData;
            string whoisDomain = string.IsNullOrWhiteSpace(whois.DomainName) ? effectiveTarget : whois.DomainName.Trim();
            GraphNode whoisRoot = whoisDomain.Equals(effectiveTarget, StringComparison.OrdinalIgnoreCase)
                ? targetNode
                : GetOrCreateNode($"domain:{whoisDomain.ToLowerInvariant()}", whoisDomain, "Домен", GraphNodeType.Target, targetNode);

            if (whoisRoot != targetNode && whoisDomain.EndsWith("." + effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                AddEdgeIfMissing(whoisRoot, targetNode, "Домен");
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
                var regNode = GetOrCreateNode($"registrar:{whois.Registrar.ToLowerInvariant()}", whois.Registrar, "Регистратор", GraphNodeType.Registrar, whoisRoot, finding.RawJson, props);
                AddEdgeIfMissing(whoisRoot, regNode, "Регистратор");
            }

            if (!string.IsNullOrWhiteSpace(whois.RegistrantOrg))
            {
                var orgNode = GetOrCreateNode($"org:{whois.RegistrantOrg.ToLowerInvariant()}", whois.RegistrantOrg, "Организация", GraphNodeType.Registrar, whoisRoot, finding.RawJson, props);
                AddEdgeIfMissing(whoisRoot, orgNode, "Владелец");
            }

            foreach (var ns in whois.NameServers)
            {
                if (!string.IsNullOrWhiteSpace(ns))
                {
                    var nsNode = GetOrCreateNode($"ns:{ns.ToLowerInvariant()}", ns, "Whois NS", GraphNodeType.NameServer, whoisRoot, null, new Dictionary<string, string> { { "NS", ns } });
                    AddEdgeIfMissing(whoisRoot, nsNode, "Делегирован NS");
                }
            }
        }

        // 5. Map HTTP Tech findings
        if (finding.IsHttpTech && finding.HttpTechData != null)
        {
            var tech = finding.HttpTechData;
            string host = effectiveTarget;
            if (Uri.TryCreate(tech.Url, UriKind.Absolute, out var uri))
            {
                host = uri.Host;
            }

            GraphNode techHostNode;
            if (host.Equals(effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                techHostNode = targetNode;
            }
            else if (host.EndsWith("." + effectiveTarget, StringComparison.OrdinalIgnoreCase))
            {
                techHostNode = GetOrCreateNode($"subdomain:{host.ToLowerInvariant()}", host, "Поддомен", GraphNodeType.Subdomain, targetNode);
                AddEdgeIfMissing(techHostNode, targetNode, "Веб-хост");
            }
            else
            {
                techHostNode = GetOrCreateNode($"host:{host.ToLowerInvariant()}", host, "Хост", GraphNodeType.Subdomain, targetNode);
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
                var serverNode = GetOrCreateNode($"tech:{tech.Server.ToLowerInvariant()}", tech.Server, "Веб-сервер", GraphNodeType.Technology, techHostNode, finding.RawJson, props);
                AddEdgeIfMissing(techHostNode, serverNode, "Сервер");
            }

            if (!string.IsNullOrWhiteSpace(tech.PoweredBy))
            {
                var poweredNode = GetOrCreateNode($"tech:{tech.PoweredBy.ToLowerInvariant()}", tech.PoweredBy, "Фреймворк", GraphNodeType.Technology, techHostNode, finding.RawJson, props);
                AddEdgeIfMissing(techHostNode, poweredNode, "Стек");
            }

            if (!string.IsNullOrWhiteSpace(tech.Title))
            {
                var titleNode = GetOrCreateNode($"title:{tech.Title.ToLowerInvariant()}", tech.Title, "Веб-страница", GraphNodeType.Other, techHostNode, finding.RawJson, props);
                AddEdgeIfMissing(techHostNode, titleNode, "Заголовок HTML");
            }
        }
    }
}
