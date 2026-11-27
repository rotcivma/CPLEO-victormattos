using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Propulsor.Core.Save
{
    /// <summary>
    /// JSON mínimo e sem dependências (funciona igual no .NET e no Unity).
    /// Objetos → Dictionary&lt;string, object&gt;, listas → List&lt;object&gt;,
    /// números → double, além de string, bool e null.
    /// </summary>
    public static class MiniJson
    {
        // ---------------- escrita ----------------

        public static string Serialize(object value, bool pretty = true)
        {
            var sb = new StringBuilder();
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object v, bool pretty, int indent)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case IDictionary<string, object> obj:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in obj)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        NewLine(sb, pretty, indent + 1);
                        WriteString(sb, kv.Key);
                        sb.Append(pretty ? ": " : ":");
                        Write(sb, kv.Value, pretty, indent + 1);
                    }
                    if (!first) NewLine(sb, pretty, indent);
                    sb.Append('}');
                    break;
                }
                case System.Collections.IEnumerable list:
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (var item in list)
                    {
                        if (!first) sb.Append(pretty ? ", " : ",");
                        first = false;
                        Write(sb, item, pretty, indent + 1);
                    }
                    sb.Append(']');
                    break;
                }
                case float f: WriteNumber(sb, f); break;
                case double d: WriteNumber(sb, d); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                default: throw new ArgumentException("Tipo não suportado em JSON: " + v.GetType());
            }
        }

        private static void WriteNumber(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append('0'); return; }
            sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void NewLine(StringBuilder sb, bool pretty, int indent)
        {
            if (!pretty) return;
            sb.Append('\n');
            sb.Append(' ', indent * 2);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------------- leitura ----------------

        /// <exception cref="FormatException">JSON inválido.</exception>
        public static object Parse(string json)
        {
            if (json == null) throw new FormatException("JSON nulo");
            int i = 0;
            var v = ParseValue(json, ref i);
            SkipWs(json, ref i);
            if (i != json.Length) throw new FormatException($"Conteúdo extra na posição {i}");
            return v;
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("Fim inesperado");
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return ParseString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber(s, ref i);
                    throw new FormatException($"Caractere inesperado '{c}' na posição {i}");
            }
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var obj = new Dictionary<string, object>();
            i++; // {
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new FormatException($"Chave esperada na posição {i}");
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException($"':' esperado na posição {i}");
                i++;
                obj[key] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("Objeto não fechado");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return obj; }
                throw new FormatException($"',' ou '}}' esperado na posição {i}");
            }
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var list = new List<object>();
            i++; // [
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("Lista não fechada");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return list; }
                throw new FormatException($"',' ou ']' esperado na posição {i}");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // "
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("Escape \\u incompleto");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException($"Escape inválido \\{e}");
                }
            }
            throw new FormatException("Texto não fechado");
        }

        private static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (!double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                throw new FormatException($"Número inválido na posição {start}");
            return d;
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw new FormatException($"'{word}' esperado na posição {i}");
            i += word.Length;
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        // ---------------- leitura tolerante ----------------

        public static int GetInt(this IDictionary<string, object> o, string key, int fallback = 0) =>
            o != null && o.TryGetValue(key, out var v) && v is double d ? (int)Math.Round(d) : fallback;

        public static float GetFloat(this IDictionary<string, object> o, string key, float fallback = 0f) =>
            o != null && o.TryGetValue(key, out var v) && v is double d ? (float)d : fallback;

        public static double GetDouble(this IDictionary<string, object> o, string key, double fallback = 0) =>
            o != null && o.TryGetValue(key, out var v) && v is double d ? d : fallback;

        public static bool GetBool(this IDictionary<string, object> o, string key, bool fallback = false) =>
            o != null && o.TryGetValue(key, out var v) && v is bool b ? b : fallback;

        public static string GetString(this IDictionary<string, object> o, string key, string fallback = null) =>
            o != null && o.TryGetValue(key, out var v) && v is string s ? s : fallback;

        public static IDictionary<string, object> GetObject(this IDictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as IDictionary<string, object> : null;

        public static List<object> GetList(this IDictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as List<object> : null;
    }
}
