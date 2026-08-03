using System;
using System.Collections.Generic;

namespace SocialNetworkAnalysis.Models
{
    public class Article
    {
        public string Id { get; set; }
        public string Doi { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public List<string> Authors { get; set; }
        public string Venue { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> ReferencedWorks { get; set; }
        public int InJsonReferenceCount { get; set; }

        public string ShortId => ExtractShortId();
        public string AuthorAbbreviation => GetAuthorAbbreviation();
        public int CitationCount => InJsonReferenceCount;
        public string DisplayText => $"{ShortId} - {Title}";

        public Article()
        {
            Id = string.Empty;
            Doi = string.Empty;
            Title = string.Empty;
            Venue = string.Empty;
            Authors = new List<string>();
            Keywords = new List<string>();
            ReferencedWorks = new List<string>();
        }

        private string ExtractShortId()
        {
            if (string.IsNullOrEmpty(Id)) return "";
            int lastSlash = Id.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < Id.Length - 1)
            {
                string workId = Id.Substring(lastSlash + 1);
                if (workId.StartsWith("W")) return workId.Substring(1);
                return workId;
            }
            return Id;
        }

        private string GetAuthorAbbreviation()
        {
            if (Authors == null || Authors.Count == 0) return "?";
            string firstAuthor = Authors[0];
            if (string.IsNullOrEmpty(firstAuthor)) return "?";
            string[] parts = firstAuthor.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                string lastName = parts[parts.Length - 1];
                return lastName.Substring(0, Math.Min(3, lastName.Length)).ToUpper();
            }
            return firstAuthor.Substring(0, 1).ToUpper();
        }

        public override string ToString() => $"{ShortId} - {Title} ({Year})";
        public override bool Equals(object obj) => obj is Article other && Id == other.Id;
        public override int GetHashCode() => Id?.GetHashCode() ?? 0;
    }
}
