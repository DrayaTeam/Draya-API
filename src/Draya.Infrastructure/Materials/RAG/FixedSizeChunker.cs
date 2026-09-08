using System.Security.Cryptography;
using System.Text;
using Draya.Application.Materials.RAG;

namespace Draya.Infrastructure.Materials.RAG;

public class FixedSizeChunker : IChunker
{
    private const int MaxChunkSize = 1000;
    private const int OverlapSize = 150;

    public List<TextChunk> Chunk(ExtractedDocument document)
    {
        var chunks = new List<TextChunk>();

        if (string.IsNullOrWhiteSpace(document.FullText))
        {
            return chunks;
        }

        if (document.FullText.Length <= MaxChunkSize)
        {
            // The whole document fits in one chunk
            chunks.Add(CreateChunk(document.FullText, 0, document.FullText.Length, 0, document));
            return chunks;
        }

        int currentIndex = 0;
        int chunkIndex = 0;

        while (currentIndex < document.FullText.Length)
        {
            // Determine the ideal end index based on max chunk size
            int idealEndIndex = Math.Min(currentIndex + MaxChunkSize, document.FullText.Length);
            int actualEndIndex = idealEndIndex;

            // If we are not at the end of the document, try to find a nice boundary to split at
            if (idealEndIndex < document.FullText.Length)
            {
                actualEndIndex = FindBestSplitIndex(document.FullText, currentIndex, idealEndIndex);
            }

            // Create the chunk
            string chunkText = document.FullText.Substring(currentIndex, actualEndIndex - currentIndex);
            chunks.Add(CreateChunk(chunkText, currentIndex, actualEndIndex, chunkIndex, document));

            // Move the current index forward for the next chunk, minus the overlap
            // Ensure we move forward by at least 1 character to avoid infinite loops if overlap is somehow messed up
            currentIndex = actualEndIndex - OverlapSize;
            
            // Safety check: if overlap pushes us back to where we started or before, force progress
            if (currentIndex <= chunks.Last().CharStart)
            {
                currentIndex = chunks.Last().CharStart + 1; // Force at least 1 char progress
            }

            chunkIndex++;
        }

        return chunks;
    }

    private int FindBestSplitIndex(string text, int startIndex, int idealEndIndex)
    {
        // Try to find a paragraph break (\n\n or \n) within a reasonable window from the end
        int minAcceptableEnd = idealEndIndex - 200; // Look back up to 200 chars for a good boundary
        if (minAcceptableEnd < startIndex) minAcceptableEnd = startIndex + 1;

        // 1. Look for double newline (paragraph boundary)
        int lastDoubleNewLine = text.LastIndexOf("\n\n", idealEndIndex - 1, idealEndIndex - minAcceptableEnd, StringComparison.Ordinal);
        if (lastDoubleNewLine >= minAcceptableEnd) return lastDoubleNewLine;

        // 2. Look for single newline
        int lastNewLine = text.LastIndexOf('\n', idealEndIndex - 1, idealEndIndex - minAcceptableEnd);
        if (lastNewLine >= minAcceptableEnd) return lastNewLine;

        // 3. Look for sentence boundary (period followed by space)
        int lastSentence = text.LastIndexOf(". ", idealEndIndex - 1, idealEndIndex - minAcceptableEnd, StringComparison.Ordinal);
        if (lastSentence >= minAcceptableEnd) return lastSentence + 1; // Include the period

        // 4. Look for word boundary (space)
        int lastSpace = text.LastIndexOf(' ', idealEndIndex - 1, idealEndIndex - minAcceptableEnd);
        if (lastSpace >= minAcceptableEnd) return lastSpace;

        // If no good boundary found, just hard split at the ideal end index
        return idealEndIndex;
    }

    private TextChunk CreateChunk(string text, int startChar, int endChar, int chunkIndex, ExtractedDocument document)
    {
        // Trim the chunk text to ensure clean chunks, but keep char indices matching original raw text
        string cleanedText = text.Trim();
        
        // Find page boundaries for this chunk based on the segments
        int? pageStart = null;
        int? pageEnd = null;

        if (document.Segments.Any())
        {
            var overlappingSegments = document.Segments
                .Where(s => s.CharStart <= endChar && s.CharEnd >= startChar && s.PageOrSlide.HasValue)
                .OrderBy(s => s.CharStart)
                .ToList();

            if (overlappingSegments.Any())
            {
                pageStart = overlappingSegments.First().PageOrSlide;
                pageEnd = overlappingSegments.Last().PageOrSlide;
            }
        }

        return new TextChunk
        {
            ChunkIndex = chunkIndex,
            Text = cleanedText,
            CharStart = startChar,
            CharEnd = endChar,
            PageStart = pageStart, // Will be enhanced when we update ExtractedDocument
            PageEnd = pageEnd,
            ChunkHash = ComputeSha256(cleanedText)
        };
    }

    private string ComputeSha256(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            foreach (byte t in bytes)
            {
                builder.Append(t.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
