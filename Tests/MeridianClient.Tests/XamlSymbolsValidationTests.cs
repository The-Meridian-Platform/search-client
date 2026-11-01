using System.IO;
using System.Text.RegularExpressions;
using Wpf.Ui.Controls;

namespace MeridianClient.Tests;

public class XamlSymbolsValidationTests
{
    [Fact]
    public void AllUsedSymbolsInXaml_MustExistInSymbolRegularEnum()
    {
        var xamlFiles = Directory.GetFiles(@"..\..\..\..\..", "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\bin\") && !f.Contains(@"\obj\"))
            .ToList();

        Assert.NotEmpty(xamlFiles);

        var validSymbols = Enum.GetNames<SymbolRegular>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingSymbols = new List<string>();

        // Regex for Symbol="..." and SymbolIcon ...
        var regexes = new[]
        {
            new Regex(@"Symbol\s*=\s*""?([A-Za-z0-9_]+)""?"),
            new Regex(@"ui:SymbolIcon\s+([A-Za-z0-9_]+)[\s\r\n>\}]")
        };

        foreach (var file in xamlFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var regex in regexes)
            {
                var matches = regex.Matches(content);
                foreach (Match m in matches)
                {
                    var symbol = m.Groups[1].Value;
                    if (!validSymbols.Contains(symbol))
                    {
                        missingSymbols.Add($"{Path.GetFileName(file)}: {symbol}");
                    }
                }
            }
        }

        Assert.Empty(missingSymbols);
    }
}
