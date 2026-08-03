using System.Collections.Generic;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Algorithms
{
    public class DFS
    {
        private ArticleGraph graph;

        public DFS(ArticleGraph graph) { this.graph = graph; }

        public List<string> TraverseFrom(string start)
        {
            var visited = new HashSet<string>();
            var result = new List<string>();
            DFSRec(start, visited, result);
            return result;
        }

        public List<List<string>> FindConnectedComponents()
        {
            var visited = new HashSet<string>();
            var components = new List<List<string>>();
            foreach (var a in graph.GetAllArticles())
            {
                if (!visited.Contains(a.Id))
                {
                    var comp = new List<string>();
                    DFSRec(a.Id, visited, comp);
                    components.Add(comp);
                }
            }
            return components;
        }

        public bool HasCycle()
        {
            var visited = new HashSet<string>();
            var stack = new HashSet<string>();
            foreach (var a in graph.GetAllArticles())
                if (CycleDFS(a.Id, visited, stack))
                    return true;
            return false;
        }

        public List<List<string>> FindAllCycles()
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();

            foreach (var article in graph.GetAllArticles())
            {
                if (!visited.Contains(article.Id))
                {
                    var path = new List<string>();
                    var pathSet = new HashSet<string>();
                    FindCyclesDFS(article.Id, visited, path, pathSet, cycles);
                }
            }

            return cycles;
        }

        private void FindCyclesDFS(string id, HashSet<string> globalVisited,
            List<string> path, HashSet<string> pathSet, List<List<string>> cycles)
        {
            if (pathSet.Contains(id))
            {
                // Döngü bulundu - döngüyü çıkar
                int cycleStart = path.IndexOf(id);
                var cycle = new List<string>();
                for (int i = cycleStart; i < path.Count; i++)
                    cycle.Add(path[i]);
                cycle.Add(id); // Döngüyü kapat

                // Aynı döngüyü tekrar eklememek için kontrol
                if (cycles.Count < 20) // Max 20 döngü
                    cycles.Add(cycle);
                return;
            }

            if (globalVisited.Contains(id)) return;

            globalVisited.Add(id);
            path.Add(id);
            pathSet.Add(id);

            foreach (var refId in graph.GetReferences(id))
            {
                FindCyclesDFS(refId, globalVisited, path, pathSet, cycles);
            }

            path.RemoveAt(path.Count - 1);
            pathSet.Remove(id);
        }

        private void DFSRec(string id, HashSet<string> visited, List<string> result)
        {
            visited.Add(id);
            result.Add(id);
            foreach (var n in graph.GetNeighbors(id))
                if (!visited.Contains(n))
                    DFSRec(n, visited, result);
        }

        private bool CycleDFS(string id, HashSet<string> visited, HashSet<string> stack)
        {
            if (stack.Contains(id)) return true;
            if (visited.Contains(id)) return false;
            visited.Add(id);
            stack.Add(id);
            foreach (var r in graph.GetReferences(id))
                if (CycleDFS(r, visited, stack))
                    return true;
            stack.Remove(id);
            return false;
        }
    }
}
