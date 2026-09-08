using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Draya.Application.Materials.RAG;
using Draya.Domain.Materials;

namespace Draya.Infrastructure.Materials.RAG.Extractors;

public class DocxContentExtractor : IContentExtractor
{
    public MaterialType SupportedType => MaterialType.DOCX;

    public Task<ExtractedDocument> ExtractAsync(Stream fileStream, CancellationToken ct)
    {
        var document = new ExtractedDocument();
        var fullTextBuilder = new StringBuilder();
        int currentCharIndex = 0;

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(fileStream, false))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    var text = paragraph.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Append newline if not first paragraph
                        if (fullTextBuilder.Length > 0)
                        {
                            fullTextBuilder.AppendLine();
                            currentCharIndex += Environment.NewLine.Length;
                        }

                        int textLength = text.Length;
                        fullTextBuilder.Append(text);

                        // DOCX has no reliable page numbers, so we treat it as one continuous segment (Page 1)
                        // Or we can just log the paragraph as a segment with page=null
                        document.Segments.Add(new TextSegment
                        {
                            PageOrSlide = null, // Unknown page
                            Text = text,
                            CharStart = currentCharIndex,
                            CharEnd = currentCharIndex + textLength
                        });

                        currentCharIndex += textLength;
                    }
                }
            }
        }

        document.FullText = fullTextBuilder.ToString();

        if (string.IsNullOrWhiteSpace(document.FullText))
        {
            throw new InvalidOperationException("Extraction failed: No extractable text found in DOCX document.");
        }

        return Task.FromResult(document);
    }
}
