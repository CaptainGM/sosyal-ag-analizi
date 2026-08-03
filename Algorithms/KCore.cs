using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Algorithms
{
    public class KCore
    {
        private ArticleGraph graph;

        public KCore(ArticleGraph graph) { this.graph = graph; }

        public Dictionary<string, int> CalculateCoreNumbers()
        {
            var cores = new Dictionary<string, int>();
            var articles = graph.GetAllArticles();
            var remaining = new HashSet<string>(articles.Select(a => a.Id));

            foreach (var a in articles) cores[a.Id] = 0;

            int k = 0;
            while (remaining.Count > 0)
            {
                var toRemove = new List<string>();
                do
                {
                    toRemove.Clear();
                    foreach (var id in remaining)
                    {
                        int deg = graph.GetNeighbors(id).Count(n => remaining.Contains(n));
                        if (deg <= k) toRemove.Add(id);
                    }
                    foreach (var id in toRemove)
                    {
                        cores[id] = k;
                        remaining.Remove(id);
                    }
                } while (toRemove.Count > 0 && remaining.Count > 0);
                k++;
            }
            return cores;
        }

        public List<Article> GetKCoreArticles(int k)
        {
            var cores = CalculateCoreNumbers();
            return graph.GetAllArticles().Where(a => cores.ContainsKey(a.Id) && cores[a.Id] >= k).ToList();
        }

        public ArticleGraph FindKCore(int k)
        {
            var ids = new HashSet<string>(GetKCoreArticles(k).Select(a => a.Id));
            return graph.CreateSubgraph(ids);
        }

        public int GetDegeneracy()
        {
            var cores = CalculateCoreNumbers();
            return cores.Count > 0 ? cores.Values.Max() : 0;
        }

        public string GetStats(int k)
        {
            var kCore = GetKCoreArticles(k);
            if (kCore.Count == 0) return $"k={k} için core yok.";
            return $"k={k}: {kCore.Count} makale, Degeneracy: {GetDegeneracy()}";
        }
    }
}
