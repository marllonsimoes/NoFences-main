using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoFences.Core.Util
{
    public static class AcfParser
    {
        public static dynamic Parse(string acfContent)
        {
            using (var reader = new StringReader(acfContent))
            {
                var root = ParseObject(reader);
                var json = JsonConvert.SerializeObject(root, Formatting.Indented);
                return JsonConvert.DeserializeObject<dynamic>(json);
            }
        }

        private static Dictionary<string, object> ParseObject(StringReader reader)
        {
            var result = new Dictionary<string, object>();
            string key = null;

            while (true)
            {
                var token = ReadToken(reader);
                if (token == null)
                    break;

                if (token == "{")
                {
                    if (key == null)
                        throw new FormatException("Unexpected '{' without key");
                    result[key] = ParseObject(reader);
                    key = null;
                }
                else if (token == "}")
                {
                    break;
                }
                else
                {
                    if (key == null)
                    {
                        key = token;
                    }
                    else
                    {
                        result[key] = token;
                        key = null;
                    }
                }
            }

            return result;
        }

        private static string ReadToken(StringReader reader)
        {
            SkipWhitespace(reader);

            int next = reader.Peek();
            if (next == -1)
                return null;

            if (next == '{' || next == '}')
            {
                reader.Read();
                return ((char)next).ToString();
            }

            if (next == '"')
            {
                return ReadQuotedString(reader);
            }

            // Unexpected token — skip line or stop
            reader.ReadLine();
            return null;
        }

        private static string ReadQuotedString(StringReader reader)
        {
            var sb = new StringBuilder();
            bool escape = false;

            if (reader.Read() != '"')
                throw new FormatException("Expected '\"' at start of string");

            while (true)
            {
                int c = reader.Read();
                if (c == -1)
                    throw new FormatException("Unterminated string");

                if (escape)
                {
                    sb.Append((char)c);
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append((char)c);
                }
            }

            return sb.ToString();
        }

        private static void SkipWhitespace(StringReader reader)
        {
            while (true)
            {
                int c = reader.Peek();
                if (c == -1)
                    return;

                if (char.IsWhiteSpace((char)c))
                {
                    reader.Read();
                    continue;
                }

                break;
            }
        }
    }
}
