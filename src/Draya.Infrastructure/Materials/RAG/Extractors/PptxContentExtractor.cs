using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Draya.Application.Materials.RAG;
using Draya.Domain.Materials;
using A = DocumentFormat.OpenXml.Drawing;

namespace Draya.Infrastructure.Materials.RAG.Extractors;

public class PptxContentExtractor : IContentExtractor
{
    public MaterialType SupportedType => MaterialType.PPTX;

    public Task<ExtractedDocument> ExtractAsync(Stream fileStream, CancellationToken ct)
    {
        var document = new ExtractedDocument();
        var fullTextBuilder = new StringBuilder();
        int currentCharIndex = 0;

        using (PresentationDocument pptDoc = PresentationDocument.Open(fileStream, false))
        {
            var presentationPart = pptDoc.PresentationPart;
            if (presentationPart != null && presentationPart.Presentation.SlideIdList != null)
            {
                int slideIndex = 1;
                foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
                {
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    if (slideId.RelationshipId != null)
                    {
                        var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId.Value!);
                        var slideText = ExtractTextFromSlidePart(slidePart);

                        if (!string.IsNullOrWhiteSpace(slideText))
                        {
                            if (fullTextBuilder.Length > 0)
                            {
                                fullTextBuilder.AppendLine();
                                fullTextBuilder.AppendLine();
                                currentCharIndex += Environment.NewLine.Length * 2;
                            }

                            int textLength = slideText.Length;
                            fullTextBuilder.Append(slideText);

                            document.Segments.Add(new TextSegment
                            {
                                PageOrSlide = slideIndex,
                                Text = slideText,
                                CharStart = currentCharIndex,
                                CharEnd = currentCharIndex + textLength
                            });

                            currentCharIndex += textLength;
                        }
                    }
                    slideIndex++;
                }
            }
        }

        document.FullText = fullTextBuilder.ToString();

        if (string.IsNullOrWhiteSpace(document.FullText))
        {
            throw new InvalidOperationException("Extraction failed: No extractable text found in PPTX document.");
        }

        return Task.FromResult(document);
    }

    private string ExtractTextFromSlidePart(SlidePart slidePart)
    {
        var slideBuilder = new StringBuilder();

        // 1. Extract text from shapes on the slide
        if (slidePart.Slide != null)
        {
            foreach (var textBody in slidePart.Slide.Descendants<A.TextBody>())
            {
                foreach (var text in textBody.Descendants<A.Text>())
                {
                    slideBuilder.Append(text.Text).Append(" ");
                }
                slideBuilder.AppendLine();
            }
        }

        // 2. Extract text from speaker notes
        var notesSlidePart = slidePart.NotesSlidePart;
        if (notesSlidePart?.NotesSlide != null)
        {
            slideBuilder.AppendLine("--- Speaker Notes ---");
            foreach (var textBody in notesSlidePart.NotesSlide.Descendants<A.TextBody>())
            {
                foreach (var text in textBody.Descendants<A.Text>())
                {
                    slideBuilder.Append(text.Text).Append(" ");
                }
                slideBuilder.AppendLine();
            }
        }

        return slideBuilder.ToString().Trim();
    }
}
