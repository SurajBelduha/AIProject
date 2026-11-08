using System.Text.RegularExpressions;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class HybridRetrievalService : IRetrievalService
{
    private readonly IApplicationDbContext _context;

    // Common English stopwords to ignore in keyword matching
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't",
        "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by", "can",
        "can't", "cannot", "could", "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't",
        "down", "during", "each", "few", "for", "from", "further", "had", "hadn't", "has", "hasn't", "have",
        "haven't", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself", "him",
        "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into", "is", "isn't",
        "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", "my", "myself", "no", "nor",
        "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", "ourselves", "out",
        "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "so", "some",
        "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there",
        "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through",
        "to", "too", "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll", "we're", "we've",
        "were", "weren't", "what", "what's", "when", "when's", "where", "where's", "which", "while", "who",
        "who's", "whom", "why", "why's", "with", "won't", "would", "wouldn't", "you", "you'd", "you'll", "you're",
        "you've", "your", "yours", "yourself", "yourselves", "per", "also", "using", "within"
    };

    public HybridRetrievalService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RetrievedChunk>> RetrieveRelevantChunksAsync(Guid organizationId, string query, int topK = 4)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<RetrievedChunk>();
        }

        // STRICT MULTI-TENANT ISOLATION:
        // Query strictly filters by OrganizationId at database execution level!
        var chunks = await _context.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.OrganizationId == organizationId)
            .ToListAsync();

        if (chunks.Count == 0)
        {
            return new List<RetrievedChunk>();
        }

        var queryTerms = ExtractTerms(query);
        if (queryTerms.Count == 0)
        {
            queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.ToLowerInvariant())
                              .ToList();
        }

        // Compute hybrid scores for all candidate chunks of this organization
        var scoredChunks = new List<(RetrievedChunk chunk, double score)>();

        foreach (var c in chunks)
        {
            var content = c.Content;
            var docName = c.Document?.OriginalFileName ?? "Document.pdf";

            var (bm25Score, matchingTermsCount) = CalculateBm25Score(queryTerms, content, docName);
            var densityScore = CalculatePhraseDensityScore(query, content);
            var combinedScore = (bm25Score * 0.65) + (densityScore * 0.35);

            // Require either at least 2 distinct matched terms (or 100% of terms if single-term query)
            // or high phrase density, plus a minimum combined score threshold of 0.8
            var termRequirement = queryTerms.Count <= 1 ? (matchingTermsCount >= 1) : (matchingTermsCount >= 2);
            if (termRequirement && combinedScore >= 0.75)
            {
                scoredChunks.Add((new RetrievedChunk
                {
                    ChunkId = c.Id,
                    DocumentId = c.DocumentId,
                    DocumentName = docName,
                    PageNumber = c.PageNumber,
                    Content = content,
                    RelevanceScore = Math.Round(combinedScore, 4)
                }, combinedScore));
            }
        }

        return scoredChunks
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.chunk)
            .ToList();
    }

    private static List<string> ExtractTerms(string text)
    {
        var words = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z0-9_]{2,}\b")
                         .Select(m => m.Value)
                         .Where(w => !StopWords.Contains(w))
                         .Distinct()
                         .ToList();
        return words;
    }

    private static (double score, int matchingTermsCount) CalculateBm25Score(List<string> queryTerms, string content, string docName)
    {
        var lowerContent = content.ToLowerInvariant();
        var lowerDoc = docName.ToLowerInvariant();

        double score = 0;
        int matchingTermsCount = 0;
        var wordsInChunk = lowerContent.Split(new[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':', '!', '?', '-', '(', ')', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
        var chunkLength = wordsInChunk.Length;

        // BM25 parameters
        const double k1 = 1.2;
        const double b = 0.75;
        const double avgDocLength = 60.0;

        foreach (var term in queryTerms)
        {
            var tf = wordsInChunk.Count(w => w.Equals(term, StringComparison.OrdinalIgnoreCase));
            if (tf > 0)
            {
                matchingTermsCount++;
                var termScore = (tf * (k1 + 1)) / (tf + k1 * (1 - b + b * (chunkLength / avgDocLength)));
                score += termScore;
            }

            // Document name boost
            if (lowerDoc.Contains(term))
            {
                score += 1.5;
            }
        }

        return (score, matchingTermsCount);
    }

    private static double CalculatePhraseDensityScore(string query, string content)
    {
        var lowerQuery = query.ToLowerInvariant();
        var lowerContent = content.ToLowerInvariant();

        // Check for exact phrase matches
        if (lowerContent.Contains(lowerQuery))
        {
            return 3.0;
        }

        // Check for 2-gram matches
        var words = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            double matches = 0;
            for (int i = 0; i < words.Length - 1; i++)
            {
                var bigram = words[i] + " " + words[i + 1];
                if (lowerContent.Contains(bigram))
                {
                    matches += 1.0;
                }
            }
            return matches / (words.Length - 1);
        }

        return 0;
    }
}
