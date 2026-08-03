using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Algorithms
{
    public class BetweennessCentrality
    {
        private ArticleGraph graph;

        public BetweennessCentrality(ArticleGraph graph)
        {
            this.graph = graph;
        }

        public Dictionary<string, double> Calculate()
        {
            var articles = graph.GetAllArticles();
            var betweenness = new Dictionary<string, double>();

            foreach (var a in articles)
                betweenness[a.Id] = 0.0;

            foreach (var source in articles)
            {
                var stack = new Stack<string>();
                var predecessors = new Dictionary<string, List<string>>();
                var sigma = new Dictionary<string, double>();
                var distance = new Dictionary<string, int>();

                foreach (var a in articles)
                {
                    predecessors[a.Id] = new List<string>();
                    sigma[a.Id] = 0.0;
                    distance[a.Id] = -1;
                }

                sigma[source.Id] = 1.0;
                distance[source.Id] = 0;

                var queue = new Queue<string>();
                queue.Enqueue(source.Id);

                while (queue.Count > 0)
                {
                    var v = queue.Dequeue();
                    stack.Push(v);

                    foreach (var w in graph.GetNeighborsUndirected(v))
                    {
                        if (distance[w] < 0)
                        {
                            queue.Enqueue(w);
                            distance[w] = distance[v] + 1;
                        }

                        if (distance[w] == distance[v] + 1)
                        {
                            sigma[w] += sigma[v];
                            predecessors[w].Add(v);
                        }
                    }
                }

                var delta = new Dictionary<string, double>();
                foreach (var a in articles)
                    delta[a.Id] = 0.0;

                while (stack.Count > 0)
                {
                    var w = stack.Pop();
                    foreach (var v in predecessors[w])
                    {
                        delta[v] += (sigma[v] / sigma[w]) * (1.0 + delta[w]);
                    }
                    if (w != source.Id)
                    {
                        betweenness[w] += delta[w];
                    }
                }
            }

            foreach (var a in articles)
                betweenness[a.Id] /= 2.0;

            return betweenness;
        }

        public Dictionary<string, double> CalculateNormalized()
        {
            var betweenness = Calculate();
            int n = graph.ArticleCount;
            double factor = (n > 2) ? 2.0 / ((n - 1) * (n - 2)) : 1.0;

            var normalized = new Dictionary<string, double>();
            foreach (var kvp in betweenness)
                normalized[kvp.Key] = kvp.Value * factor;

            return normalized;
        }

        public List<(Article article, double centrality)> GetAllCentralNodes()
        {
            var betweenness = Calculate();
            return betweenness
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => (graph.GetArticle(kvp.Key), kvp.Value))
                .Where(x => x.Item1 != null)
                .ToList();
        }

        public List<(Article article, double centrality)> GetTopCentralNodes(int count)
        {
            var betweenness = Calculate();
            return betweenness
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => (graph.GetArticle(kvp.Key), kvp.Value))
                .Where(x => x.Item1 != null)
                .ToList();
        }
    }
}
