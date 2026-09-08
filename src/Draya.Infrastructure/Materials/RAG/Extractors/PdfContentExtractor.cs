using System.Text;
using Draya.Application.Materials.RAG;
using Draya.Domain.Materials;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Draya.Infrastructure.Materials.RAG.Extractors;

public class PdfContentExtractor : IContentExtractor
{
    public MaterialType SupportedType => MaterialType.PDF;

    public Task<ExtractedDocument> ExtractAsync(Stream fileStream, CancellationToken ct)
    {
        var document = new ExtractedDocument();
        var fullTextBuilder = new StringBuilder();
        int currentCharIndex = 0;

        using (var pdf = PdfDocument.Open(fileStream))
        {
            foreach (var page in pdf.GetPages())
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                var pageText = ContentOrderTextExtractor.GetText(page);
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    // Add a newline between pages if we are appending
                    if (fullTextBuilder.Length > 0)
                    {
                        fullTextBuilder.AppendLine();
                        fullTextBuilder.AppendLine();
                        currentCharIndex += Environment.NewLine.Length * 2;
                    }

                    int textLength = pageText.Length;
                    fullTextBuilder.Append(pageText);

                    document.Segments.Add(new TextSegment
                    {
                        PageOrSlide = page.Number,
                        Text = pageText,
                        CharStart = currentCharIndex,
                        CharEnd = currentCharIndex + textLength
                    });

                    currentCharIndex += textLength;
                }
            }
        }

        document.FullText = fullTextBuilder.ToString();

        if (string.IsNullOrWhiteSpace(document.FullText))
        {
            throw new InvalidOperationException("Extraction failed: No extractable text found in PDF document.");
        }

        return Task.FromResult(document);
    }
}
