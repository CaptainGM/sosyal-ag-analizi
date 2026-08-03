using System.Collections.Generic;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Algorithms
{
    public class BFS
    {
        private ArticleGraph graph;

        public BFS(ArticleGraph graph) { this.graph = graph; }

        public List<string> FindShortestPath(string start, string end)
        {
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
                return new List<string>();

            if (start == end)
                return new List<string> { start };

            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            var parent = new Dictionary<string, string>();

            queue.Enqueue(start);
            visited.Add(start);
            parent[start] = null;

            while (queue.Count > 0)
            {
                string curr = queue.Dequeue();
                var neighbors = graph.GetNeighbors(curr);

                foreach (var n in neighbors)
                {
                    if (!visited.Contains(n))
                    {
                        visited.Add(n);
                        parent[n] = curr;
                        queue.Enqueue(n);

                        if (n == end)
                        {
                            var path = new List<string>();
                            string c = end;
                            while (c != null)
                            {
                                path.Add(c);
                                c = parent[c];
                            }
                            path.Reverse();
                            return path;
                        }
                    }
                }
            }
            return new List<string>();
        }

        public List<string> GetReachable(string start, int maxDepth = -1)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<(string id, int d)>();
            var result = new List<string>();

            queue.Enqueue((start, 0));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (curr, d) = queue.Dequeue();
                result.Add(curr);
                if (maxDepth >= 0 && d >= maxDepth) continue;
                foreach (var n in graph.GetNeighbors(curr))
                {
                    if (!visited.Contains(n))
                    {
                        visited.Add(n);
                        queue.Enqueue((n, d + 1));
                    }
                }
            }
            return result;
        }
    }
}
