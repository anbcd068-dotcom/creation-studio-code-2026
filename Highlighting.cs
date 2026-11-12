using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace CreationStudio2026;

/// <summary>Builds and caches a dark-theme syntax definition.</summary>
public static class Highlighting
{
    private static readonly Dictionary<string, IHighlightingDefinition> Cache = new();

    public static IHighlightingDefinition Get(LanguageDef language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var key = $"{language.Name}|{language.Comment}|{language.Keywords}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;

        var keywordXml = string.Concat(language.Keywords
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .Select(word => $"<Word>{Escape(word)}</Word>"));

        var commentXml = string.IsNullOrWhiteSpace(language.Comment)
            ? string.Empty
            : $"<Rule color=\"Comment\">{Escape(Regex.Escape(language.Comment) + @".*")}</Rule>";

        var keywordsXml = keywordXml.Length == 0
            ? string.Empty
            : $"<Keywords color=\"Keyword\">{keywordXml}</Keywords>";

        var xml = $"""
            <SyntaxDefinition name="CreationStudio" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
              <Color name="Comment" foreground="#6A9955"/>
              <Color name="String" foreground="#CE9178"/>
              <Color name="Keyword" foreground="#569CD6" fontWeight="bold"/>
              <Color name="Number" foreground="#B5CEA8"/>
              <RuleSet>
                {commentXml}
                <Span color="String"><Begin>"</Begin><End>"</End></Span>
                <Span color="String"><Begin>'</Begin><End>'</End></Span>
                {keywordsXml}
                <Rule color="Number">\b0[xX][0-9a-fA-F]+|\b\d+(\.\d+)?</Rule>
              </RuleSet>
            </SyntaxDefinition>
            """;

        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader);
        var definition = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
        Cache[key] = definition;
        return definition;
    }
}
