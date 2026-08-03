using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocialNetworkAnalysis.Data
{
    public static class JsonParser
    {
        public static List<Dictionary<string, object>> ParseJsonArray(string json)
        {
            var result = new List<Dictionary<string, object>>();
            json = json.Trim();

            if (!json.StartsWith("[") || !json.EndsWith("]"))
                throw new FormatException("JSON array '[' ile başlamalı ve ']' ile bitmelidir.");

            string content = json.Substring(1, json.Length - 2);
            var objects = SplitJsonObjects(content);

            foreach (var obj in objects)
            {
                var dict = ParseJsonObject(obj);
                if (dict.Count > 0)
                    result.Add(dict);
            }

            return result;
        }

        private static List<string> SplitJsonObjects(string content)
        {
            var result = new List<string>();
            int braceCount = 0;
            int bracketCount = 0;
            int quoteCount = 0;
            var sb = new StringBuilder();

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                char prev = i > 0 ? content[i - 1] : '\0';

                // String içindeyse
                if (c == '"' && prev != '\\')
                    quoteCount++;

                // String dışında
                if (quoteCount % 2 == 0)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && sb.Length > 0)
                        {
                            result.Add(sb.ToString() + "}");
                            sb.Clear();
                            continue;
                        }
                    }
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                }

                if (braceCount > 0)
                    sb.Append(c);
            }

            return result.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        public static Dictionary<string, object> ParseJsonObject(string json)
        {
            var result = new Dictionary<string, object>();
            json = json.Trim();

            if (!json.StartsWith("{") || !json.EndsWith("}"))
                return result;

            string content = json.Substring(1, json.Length - 2);
            var pairs = SplitJsonPairs(content);

            foreach (var pair in pairs)
            {
                var (key, value) = ParseJsonPair(pair);
                if (!string.IsNullOrEmpty(key))
                    result[key] = value;
            }

            return result;
        }

        private static List<string> SplitJsonPairs(string content)
        {
            var result = new List<string>();
            int braceCount = 0;
            int bracketCount = 0;
            int quoteCount = 0;
            var sb = new StringBuilder();

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                char prev = i > 0 ? content[i - 1] : '\0';

                if (c == '"' && prev != '\\')
                    quoteCount++;

                if (quoteCount % 2 == 0)
                {
                    if (c == '{' || c == '[')
                    {
                        if (c == '{') braceCount++;
                        else bracketCount++;
                    }
                    else if (c == '}' || c == ']')
                    {
                        if (c == '}') braceCount--;
                        else bracketCount--;
                    }
                    else if (c == ',' && braceCount == 0 && bracketCount == 0)
                    {
                        if (sb.Length > 0)
                            result.Add(sb.ToString());
                        sb.Clear();
                        continue;
                    }
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        private static (string key, object value) ParseJsonPair(string pair)
        {
            int colonIndex = FindFirstColon(pair);
            if (colonIndex < 0)
                return (null, null);

            string keyPart = pair.Substring(0, colonIndex).Trim();
            string valuePart = pair.Substring(colonIndex + 1).Trim();

            string key = ExtractString(keyPart);
            object value = ParseJsonValue(valuePart);

            return (key, value);
        }

        private static int FindFirstColon(string s)
        {
            int quoteCount = 0;
            int braceCount = 0;
            int bracketCount = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                char prev = i > 0 ? s[i - 1] : '\0';

                if (c == '"' && prev != '\\')
                    quoteCount++;

                if (quoteCount % 2 == 0)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                    else if (c == ':' && braceCount == 0 && bracketCount == 0)
                        return i;
                }
            }

            return -1;
        }

        private static object ParseJsonValue(string value)
        {
            value = value.Trim();

            if (value == "null") return null;
            if (value == "true") return true;
            if (value == "false") return false;

            if (value.StartsWith("\"") && value.EndsWith("\""))
                return ExtractString(value);

            if (value.StartsWith("[") && value.EndsWith("]"))
                return ParseSimpleArray(value);

            if (value.StartsWith("{") && value.EndsWith("}"))
                return ParseJsonObject(value);

            if (int.TryParse(value, out int intVal))
                return intVal;

            if (double.TryParse(value.Replace(",", "."), out double doubleVal))
                return doubleVal;

            return value;
        }

        private static List<object> ParseSimpleArray(string arrayJson)
        {
            var result = new List<object>();
            arrayJson = arrayJson.Trim();

            if (!arrayJson.StartsWith("[") || !arrayJson.EndsWith("]"))
                return result;

            string content = arrayJson.Substring(1, arrayJson.Length - 2).Trim();

            if (string.IsNullOrEmpty(content))
                return result;

            // Array elemanlarını ayır
            var elements = SplitArrayElements(content);

            foreach (var element in elements)
            {
                var parsed = ParseJsonValue(element.Trim());
                if (parsed != null)
                    result.Add(parsed);
            }

            return result;
        }

        private static List<string> SplitArrayElements(string content)
        {
            var result = new List<string>();
            int braceCount = 0;
            int bracketCount = 0;
            int quoteCount = 0;
            var sb = new StringBuilder();

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                char prev = i > 0 ? content[i - 1] : '\0';

                if (c == '"' && prev != '\\')
                    quoteCount++;

                if (quoteCount % 2 == 0)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                    else if (c == ',' && braceCount == 0 && bracketCount == 0)
                    {
                        if (sb.Length > 0)
                            result.Add(sb.ToString().Trim());
                        sb.Clear();
                        continue;
                    }
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                result.Add(sb.ToString().Trim());

            return result;
        }

        private static string ExtractString(string jsonString)
        {
            jsonString = jsonString.Trim();

            if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                jsonString = jsonString.Substring(1, jsonString.Length - 2);

            // Escape sequence'leri çöz
            jsonString = jsonString.Replace("\\\"", "\"");
            jsonString = jsonString.Replace("\\\\", "\\");
            jsonString = jsonString.Replace("\\n", "\n");
            jsonString = jsonString.Replace("\\r", "\r");
            jsonString = jsonString.Replace("\\t", "\t");

            return jsonString;
        }
    }
}
