using System;
using System.Collections.Generic;
using System.Linq;

namespace CreativeWrites.Services
{
    /// <summary>
    /// Heuristic-only "AI likeness" estimator. Returns 0-100. For entertainment,
    /// not a real classifier — calibration is intentionally rough.
    /// </summary>
    public static class AiScoreCalculator
    {
        private static readonly string[] LlmPhrases =
        {
            "delve", "tapestry", "in conclusion", "it's important to note",
            "navigate", "embark", "realm", "intricate", "nuanced"
        };

        public static int Score(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return 0;

            var sentences = SplitSentences(body);
            var words = body.Split(
                new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' },
                StringSplitOptions.RemoveEmptyEntries);

            if (sentences.Count < 3 || words.Length < 20) return 15;

            double varianceScore = SentenceLengthVarianceScore(sentences);
            double phraseScore = LlmPhraseScore(body);
            double ttrScore = TypeTokenRatioScore(words);
            double avgLenScore = AverageSentenceLengthScore(sentences);

            double weighted =
                varianceScore * 0.25 +
                phraseScore * 0.30 +
                ttrScore * 0.20 +
                avgLenScore * 0.25;

            return (int)Math.Round(Math.Clamp(weighted, 0, 100));
        }

        private static List<string> SplitSentences(string body) =>
            body.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

        // Lower variance in sentence length → more uniform → trends AI
        private static double SentenceLengthVarianceScore(IReadOnlyList<string> sentences)
        {
            var lengths = sentences
                .Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
                .ToList();
            if (lengths.Count < 2) return 50;
            double mean = lengths.Average();
            double variance = lengths.Sum(l => (l - mean) * (l - mean)) / lengths.Count;
            return Math.Clamp(100 - variance * 2, 0, 100);
        }

        private static double LlmPhraseScore(string body)
        {
            string lower = body.ToLowerInvariant();
            int hits = 0;
            foreach (var phrase in LlmPhrases)
            {
                int idx = 0;
                while ((idx = lower.IndexOf(phrase, idx, StringComparison.Ordinal)) >= 0)
                {
                    hits++;
                    idx += phrase.Length;
                }
            }
            int dashes = body.Count(c => c == '—');
            return Math.Min((hits + dashes) * 15, 100);
        }

        // Lower type-token ratio (more repetition) → trends AI
        private static double TypeTokenRatioScore(string[] words)
        {
            if (words.Length == 0) return 50;
            int unique = new HashSet<string>(
                words.Select(w => w.ToLowerInvariant())).Count;
            double ttr = (double)unique / words.Length;
            return Math.Clamp((0.7 - ttr) * 250, 0, 100);
        }

        // Longer average sentence → trends AI
        private static double AverageSentenceLengthScore(IReadOnlyList<string> sentences)
        {
            double avg = sentences.Average(
                s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            return Math.Clamp((avg - 10) * (100.0 / 15.0), 0, 100);
        }
    }
}
