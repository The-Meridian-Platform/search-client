using System;
using System.Collections.Generic;

namespace MeridianClient.Models;

public class ParsedSshKeyItem
{
    public string KeyType { get; set; } = string.Empty;
    public string FingerprintSha256 { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

public class ParsedUsernameSshKeysData
{
    public string Username { get; set; } = string.Empty;
    public IReadOnlyList<ParsedSshKeyItem> Keys { get; set; } = Array.Empty<ParsedSshKeyItem>();
    public IReadOnlyList<string> DiscoveredEmails { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DiscoveredHostnames { get; set; } = Array.Empty<string>();

    public int KeyCount => Keys?.Count ?? 0;
    public bool HasKeys => KeyCount > 0;
    public bool HasEmails => DiscoveredEmails != null && DiscoveredEmails.Count > 0;
    public bool HasHostnames => DiscoveredHostnames != null && DiscoveredHostnames.Count > 0;
}
