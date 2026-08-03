using System;
using System.Collections.Generic;
using System.IO;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Data
{
    public class DataLoader
    {
        public static ArticleGraph LoadFromJson(string filePath)
        {
            var graph = new ArticleGraph();
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

            string json = File.ReadAllText(filePath);
            var articleDictionaries = JsonParser.ParseJsonArray(json);
            var allIds = new HashSet<string>();

            foreach (var item in articleDictionaries)
            {
                var article = new Article
                {
                    Id = GetStringValue(item, "id", ""),
                    Doi = GetStringValue(item, "doi", ""),
                    Title = GetStringValue(item, "title", ""),
                    Year = GetIntValue(item, "year", 0),
                    Authors = GetStringList(item, "authors"),
                    Venue = GetStringValue(item, "venue", ""),
                    Keywords = GetStringList(item, "keywords"),
                    ReferencedWorks = GetStringList(item, "referenced_works"),
                    InJsonReferenceCount = GetIntValue(item, "in_json_reference_count", 0)
                };
                graph.AddArticle(article);
                allIds.Add(article.Id);
            }

            foreach (var article in graph.GetAllArticles())
            {
                foreach (var refId in article.ReferencedWorks)
                {
                    if (allIds.Contains(refId))
                        graph.AddReferenceEdge(article.Id, refId);
                }
            }

            return graph;
        }

        private static string GetStringValue(Dictionary<string, object> dict, string key, string defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        private static int GetIntValue(Dictionary<string, object> dict, string key, int defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is int intVal) return intVal;
                if (int.TryParse(value?.ToString(), out int parsed)) return parsed;
            }
            return defaultValue;
        }

        private static List<string> GetStringList(Dictionary<string, object> dict, string key)
        {
            var result = new List<string>();
            if (dict.TryGetValue(key, out var value))
            {
                if (value == null)
                    return result;

                // List<object> - JsonParser'dan geliyor
                if (value is List<object> objList)
                {
                    foreach (var item in objList)
                    {
                        if (item != null)
                            result.Add(item.ToString());
                    }
                }
                // Object[]
                else if (value is object[] objArray)
                {
                    foreach (var item in objArray)
                    {
                        if (item != null)
                            result.Add(item.ToString());
                    }
                }
                // string[]
                else if (value is string[] strArray)
                {
                    result.AddRange(strArray);
                }
                // List<string>
                else if (value is List<string> strList)
                {
                    result.AddRange(strList);
                }
            }
            return result;
        }
    }
}
