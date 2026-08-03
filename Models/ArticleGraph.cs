using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialNetworkAnalysis.Models
{
    public class ArticleGraph
    {
        private Dictionary<string, Article> articles;
        private Dictionary<string, List<string>> referenceEdges;
        private Dictionary<string, List<string>> incomingReferences;

        public ArticleGraph()
        {
            articles = new Dictionary<string, Article>();
            referenceEdges = new Dictionary<string, List<string>>();
            incomingReferences = new Dictionary<string, List<string>>();
        }

        public void AddArticle(Article article)
        {
            if (!articles.ContainsKey(article.Id))
            {
                articles[article.Id] = article;
                referenceEdges[article.Id] = new List<string>();
                incomingReferences[article.Id] = new List<string>();
            }
        }

        public void AddReferenceEdge(string sourceId, string targetId)
        {
            if (articles.ContainsKey(sourceId) && articles.ContainsKey(targetId))
            {
                if (!referenceEdges[sourceId].Contains(targetId))
                    referenceEdges[sourceId].Add(targetId);
                if (!incomingReferences[targetId].Contains(sourceId))
                    incomingReferences[targetId].Add(sourceId);
            }
        }

        public List<Article> GetAllArticles() => articles.Values.ToList();
        public Article GetArticle(string id) => articles.ContainsKey(id) ? articles[id] : null;
        public int ArticleCount => articles.Count;
        public List<string> GetReferences(string id) => referenceEdges.ContainsKey(id) ? referenceEdges[id].ToList() : new List<string>();
        public List<string> GetCitations(string id) => incomingReferences.ContainsKey(id) ? incomingReferences[id].ToList() : new List<string>();

        public List<Article> GetArticlesSortedById() => articles.Values.OrderBy(a => a.ShortId).ToList();

        public List<(string from, string to)> GetIdOrderEdges()
        {
            var sorted = GetArticlesSortedById();
            var edges = new List<(string, string)>();
            for (int i = 0; i < sorted.Count - 1; i++)
                edges.Add((sorted[i].Id, sorted[i + 1].Id));
            return edges;
        }

        public List<(string from, string to)> GetAllReferenceEdges()
        {
            var edges = new List<(string, string)>();
            foreach (var kvp in referenceEdges)
                foreach (var target in kvp.Value)
                    edges.Add((kvp.Key, target));
            return edges;
        }

        public int GetTotalReferenceEdges()
        {
            return referenceEdges.Values.Sum(l => l.Count);
        }

        public int GetTotalOutgoingReferences()
        {
            return referenceEdges.Values.Sum(l => l.Count);
        }

        public int GetTotalIncomingReferences()
        {
            return incomingReferences.Values.Sum(l => l.Count);
        }

        public (Article article, int count) GetMostCitedArticle()
        {
            Article mostCited = null;
            int maxCitations = 0;
            foreach (var kvp in incomingReferences)
            {
                if (kvp.Value.Count > maxCitations)
                {
                    maxCitations = kvp.Value.Count;
                    mostCited = articles.ContainsKey(kvp.Key) ? articles[kvp.Key] : null;
                }
            }
            return (mostCited, maxCitations);
        }

        public (Article article, int count) GetMostReferencingArticle()
        {
            Article mostReferencing = null;
            int maxReferences = 0;
            foreach (var kvp in referenceEdges)
            {
                if (kvp.Value.Count > maxReferences)
                {
                    maxReferences = kvp.Value.Count;
                    mostReferencing = articles.ContainsKey(kvp.Key) ? articles[kvp.Key] : null;
                }
            }
            return (mostReferencing, maxReferences);
        }

        public int GetDegree(string id)
        {
            int outD = referenceEdges.ContainsKey(id) ? referenceEdges[id].Count : 0;
            int inD = incomingReferences.ContainsKey(id) ? incomingReferences[id].Count : 0;
            return outD + inD;
        }

        public int GetUndirectedDegree(string id)
        {
            return GetNeighborsUndirected(id).Count;
        }

        public List<string> GetNeighbors(string id)
        {
            var neighbors = new HashSet<string>();
            if (referenceEdges.ContainsKey(id)) foreach (var r in referenceEdges[id]) neighbors.Add(r);
            if (incomingReferences.ContainsKey(id)) foreach (var r in incomingReferences[id]) neighbors.Add(r);
            return neighbors.ToList();
        }

        public List<string> GetNeighborsUndirected(string id)
        {
            var neighbors = new HashSet<string>();
            if (referenceEdges.ContainsKey(id))
                foreach (var r in referenceEdges[id])
                    neighbors.Add(r);
            if (incomingReferences.ContainsKey(id))
                foreach (var r in incomingReferences[id])
                    neighbors.Add(r);
            return neighbors.ToList();
        }

        public ArticleGraph CreateSubgraph(HashSet<string> ids)
        {
            var sub = new ArticleGraph();
            foreach (var id in ids)
                if (articles.ContainsKey(id))
                    sub.AddArticle(articles[id]);
            foreach (var id in ids)
                if (referenceEdges.ContainsKey(id))
                    foreach (var target in referenceEdges[id])
                        if (ids.Contains(target))
                            sub.AddReferenceEdge(id, target);
            return sub;
        }

        public ArticleGraph ConvertToUndirected()
        {
            var undirected = new ArticleGraph();
            foreach (var article in articles.Values)
                undirected.AddArticle(article);

            var addedEdges = new HashSet<string>();
            foreach (var kvp in referenceEdges)
            {
                foreach (var target in kvp.Value)
                {
                    string edgeKey1 = kvp.Key + "|" + target;
                    string edgeKey2 = target + "|" + kvp.Key;
                    if (!addedEdges.Contains(edgeKey1) && !addedEdges.Contains(edgeKey2))
                    {
                        undirected.AddReferenceEdge(kvp.Key, target);
                        undirected.AddReferenceEdge(target, kvp.Key);
                        addedEdges.Add(edgeKey1);
                        addedEdges.Add(edgeKey2);
                    }
                }
            }
            return undirected;
        }

        public List<(string from, string to)> GetUndirectedEdges()
        {
            var edges = new List<(string, string)>();
            var addedEdges = new HashSet<string>();

            foreach (var kvp in referenceEdges)
            {
                foreach (var target in kvp.Value)
                {
                    string key1 = kvp.Key.CompareTo(target) < 0 ? kvp.Key + "|" + target : target + "|" + kvp.Key;
                    if (!addedEdges.Contains(key1))
                    {
                        edges.Add((kvp.Key, target));
                        addedEdges.Add(key1);
                    }
                }
            }
            return edges;
        }

        public string GetGraphInfo()
        {
            int edges = referenceEdges.Values.Sum(l => l.Count);
            return $"Makale: {articles.Count}, Kenar: {edges}";
        }

        public string GetDetailedStats()
        {
            int totalArticles = articles.Count;
            int totalEdges = GetTotalReferenceEdges();
            int totalOutgoing = GetTotalOutgoingReferences();
            int totalIncoming = GetTotalIncomingReferences();
            var (mostCited, mostCitedCount) = GetMostCitedArticle();
            var (mostReferencing, mostRefCount) = GetMostReferencingArticle();

            string stats = $"Toplam Makale: {totalArticles}\n";
            stats += $"Toplam Kenar: {totalEdges}\n";
            stats += $"Toplam Verilen Ref: {totalOutgoing}\n";
            stats += $"Toplam Alınan Ref: {totalIncoming}\n";
            if (mostCited != null)
                stats += $"En Çok Atıf Alan: {mostCited.ShortId} ({mostCitedCount})\n";
            if (mostReferencing != null)
                stats += $"En Çok Ref Veren: {mostReferencing.ShortId} ({mostRefCount})";
            return stats;
        }
    }
}
