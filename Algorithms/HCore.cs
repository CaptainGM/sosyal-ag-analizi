using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Algorithms
{
    public class HCore
    {
        private ArticleGraph graph;

        public HCore(ArticleGraph graph) { this.graph = graph; }

        public (int hIndex, List<Article> hCore, double hMedian) CalculateArticleHIndex(string articleId)
        {
            var citingArticles = graph.GetCitations(articleId);
            if (citingArticles.Count == 0)
                return (0, new List<Article>(), 0);

            var citingWithCounts = new List<(Article article, int citationCount)>();
            foreach (var citingId in citingArticles)
            {
                var article = graph.GetArticle(citingId);
                if (article != null)
                {
                    int count = graph.GetCitations(citingId).Count;
                    citingWithCounts.Add((article, count));
                }
            }

            citingWithCounts = citingWithCounts.OrderByDescending(x => x.citationCount).ToList();

            int hIndex = 0;
            for (int i = 0; i < citingWithCounts.Count; i++)
            {
                if (citingWithCounts[i].citationCount >= i + 1)
                    hIndex = i + 1;
                else
                    break;
            }

            var hCoreArticles = citingWithCounts.Take(hIndex).Select(x => x.article).ToList();

            double hMedian = 0;
            if (hIndex > 0)
            {
                var hCoreCitations = citingWithCounts.Take(hIndex).Select(x => x.citationCount).ToList();
                if (hCoreCitations.Count % 2 == 1)
                    hMedian = hCoreCitations[hCoreCitations.Count / 2];
                else
                    hMedian = (hCoreCitations[hCoreCitations.Count / 2 - 1] + hCoreCitations[hCoreCitations.Count / 2]) / 2.0;
            }

            return (hIndex, hCoreArticles, hMedian);
        }

        public List<Article> FindHCore(int h)
        {
            return graph.GetAllArticles().Where(a => a.CitationCount >= h).ToList();
        }

        public int CalculateHIndex()
        {
            var counts = graph.GetAllArticles().Select(a => a.CitationCount).OrderByDescending(c => c).ToList();
            int hIndex = 0;
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] >= i + 1) hIndex = i + 1;
                else break;
            }
            return hIndex;
        }

        public ArticleGraph CreateHCoreSubgraph(int h)
        {
            var ids = new HashSet<string>(FindHCore(h).Select(a => a.Id));
            return graph.CreateSubgraph(ids);
        }

        public ArticleGraph CreateArticleHCoreSubgraph(string articleId)
        {
            var (hIndex, hCore, hMedian) = CalculateArticleHIndex(articleId);
            var ids = new HashSet<string>(hCore.Select(a => a.Id));
            ids.Add(articleId);
            return graph.CreateSubgraph(ids);
        }

        public string GetStats(int h)
        {
            var hCore = FindHCore(h);
            if (hCore.Count == 0) return $"h={h} için makale yok.";
            double avg = hCore.Average(a => a.CitationCount);
            int max = hCore.Max(a => a.CitationCount);
            return $"h={h}: {hCore.Count} makale, Ort: {avg:F1}, Max: {max}";
        }
    }
}
