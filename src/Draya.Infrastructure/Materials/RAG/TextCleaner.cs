using System.Text;
using System.Text.RegularExpressions;
using Draya.Application.Materials.RAG;

namespace Draya.Infrastructure.Materials.RAG;

public class TextCleaner : ITextCleaner
{
    // Regex to match one or more whitespace characters (including newlines and tabs)
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    
    // Regex to match a hyphen at the end of a line (e.g., word splits across lines)
    private static readonly Regex HyphenatedLineBreakRegex = new(@"(\w+)-\s*\n\s*(\w+)", RegexOptions.Compiled);

    public string Clean(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        // 1. Normalize Unicode (NFC - Normalization Form Canonical Composition)
        // This is especially important for Arabic text where characters can be represented in multiple ways.
        var text = rawText.Normalize(NormalizationForm.FormC);

        // 2. Remove hidden/control characters (keeping basic whitespace like \n, \t, \r)
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (!char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
            {
                sb.Append(c);
            }
        }
        text = sb.ToString();

        // 3. Fix hyphenated line breaks (e.g., "informa-\ntion" -> "information")
        text = HyphenatedLineBreakRegex.Replace(text, "$1$2");

        // 4. Collapse consecutive whitespace into a single space
        text = WhitespaceRegex.Replace(text, " ");

        return text.Trim();
    }
}
