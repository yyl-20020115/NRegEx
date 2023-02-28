using System.Text;

namespace NRegEx;

public record class Capture(int Index = 0, int Length = -1, string? Value = null);

public delegate string CaptureEvaluator(Capture capture);
public class Regex
{
    //|()[]{}^$*+?\.
    //there is no place for #
    public readonly static char[] MetaChars
        = { '|', '(', ')', '[', ']', '{', '}', '^', '$', '*', '+', '?', '\\', '.' };

    public static bool IsMetachar(char ch) 
        => Array.IndexOf(MetaChars, ch) >= 0;

    public static string Escape(string input)
    {
        var chars = input.ToArray();
        if((Array.FindIndex(chars, ch => IsMetachar(ch)) is int i) && (-1 == i)) return input;
        var builder = new StringBuilder(input.Length * 3);
        var last = 0;
        while(true)
        {
            builder.Append(chars[last..i]);
            if (i>= chars.Length) break;
            var ch = chars[i++];
            last = i;
            builder.Append('\\');
            builder.Append(ch switch
            {
                '\n' => 'n',
                '\r' => 'r',
                '\t' => 't',
                '\f' => 'f',
                _ => ch,
            });

            var tail = chars[last..];
            if (-1 == (i = Array.FindIndex(tail, ch => IsMetachar(ch))))
            {
                builder.Append(tail);
                break;
            }
            else
            {
                i += last;
            }
        }
        return builder.ToString();
    }

    public static string Unescape(string input)
    {
        var chars = input.ToArray();
        if ((Array.IndexOf(chars, '\\') is int i) && (-1 == i)) return input;
        var last = 0;
        var builder = new StringBuilder(input.Length * 3);
        while(true)
        {
            builder.Append(chars[last..i]);
            if (i == chars.Length) break;
            var ch = chars[last = ++i];
            builder.Append(ch switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'f' => '\f',
                _ => ch,
            });

            last = ch == '\\' ? last + 2 : last + 1;
            var tail = chars[last..];
            if (-1 == (i = Array.IndexOf(tail, '\\')))
            {
                builder.Append(tail);
                break;
            }
            else
            {
                i += last;
            }
        }
        return builder.ToString();
    }

    public static string[] Split(string input, string pattern, int count = 0, int start = 0)
        => new Regex(pattern).Split(input, count, start);

    public static string Replace(string input, string pattern, string replacement, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, replacement, count, start);

    public static string Replace(string input, string pattern, CaptureEvaluator evaluator, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, evaluator, count, start);

    public static bool IsMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsCompletelyMatch(input, start, length);
    public static Capture Match(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).CompletelyMatch(input, start, length);
    public static List<Capture> Matches(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).Matches(input, start, length);
    /// <summary>
    /// We should check easy back tracing regex first
    /// or we'll have to accept the early (not lazy or greedy) result for sure.
    /// 
    /// </summary>
    /// <param name="regex"></param>
    /// <returns></returns>
    //public static bool IsBacktracingFriendly(string regex)
    //    => !string.IsNullOrEmpty(regex)
    //    && Graph.IsBacktracingFriendly(new Regex(regex).Graph);
    public readonly RegExNode Node;
    public readonly Graph Graph;
    public readonly Options Options;
    public readonly string Pattern;
    public readonly string Name;
    public Regex(string pattern, string? name = null, Options options = Options.None)
    {
        this.Pattern = pattern;
        this.Name = name ?? this.Pattern;
        this.Options = options;
        this.Node = RegExDomParser.Parse(this.Name, this.Pattern, this.Options);
        this.Graph = RegExGraphBuilder.Build(this.Node,0,
            (this.Options & Options.FOLD_CASE) == Options.FOLD_CASE);
    }

    public bool IsCompletelyMatch(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        if (length==0 && RegExGraphBuilder.HasPassThrough(this.Graph)) return true;

        var heads = this.Graph.Nodes.Where(n=>n.Inputs.Count==0);
        var nodes = heads.ToHashSet();
        var i = start;
        while (nodes.Count > 0 && i < length)
        {
            var c = input[i];
            var hit = false;
            var copies = nodes.ToArray();
            nodes.Clear();
            foreach (var node in copies)
            {
                var d = node.TryHit(c);
                if (!d.HasValue)
                {
                    node.FetchNodes(nodes, true);
                }
                else if (d.Value)
                {
                    hit = true;
                    node.FetchNodes(nodes, false);
                }
            }
            i += hit ? 1 : 0;
        }

        return i == input.Length && RegExGraphBuilder.HasPassThrough(this.Graph,nodes.ToArray());
    }
    public Capture CompletelyMatch(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start<0 || start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        var s = start;
        var heads = this.Graph.Nodes.Where(n => n.Inputs.Count == 0);
    repeat:

        var nodes = heads?.ToHashSet() ?? new();
        var i = start;
        var m = 0;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    var d = node.TryHit(c);
                    if (d == null)
                    {
                        node.FetchNodes(nodes);
                        continue;
                    }
                    else if (d.Value)
                    {
                        hit = true;
                        //needs all hits
                        nodes.UnionWith(node.Outputs);
                    }
                }
                if (hit)
                {
                    m++;
                    i++;
                }
                else
                {
                    start += m;
                    goto repeat;
                }
            }
        }

        return start > s && nodes.Count == 0
            ? new(start, m, input[start..(start + m)])
            : new(start, -1)
            ;
    }
    public List<Capture> Matches(string input, int start = 0, int length = -1)
    {
        var captures = new List<Capture>();
        while (true)
        {
            var capture = this.CompletelyMatch(input, start, length);
            if (null == capture)
                break;
            else
            {
                if (capture.Index < 0 || capture.Length == 0)
                    break;
                captures.Add(capture);
            }
            if (capture.Index + capture.Length >= length) break;
        }
        return captures;
    }
    public string Replace(string input, string replacement, int count = 0, int start = 0)
        => this.Replace(input, (Capture capture) => replacement, count, start);
    public string Replace(string input, CaptureEvaluator evaluator, int count = 0, int start = 0)
    {
        var matchs = this.Matches(input, start);
        var result = new List<string>();
        var c = start = 0;
        foreach (var match in matchs)
        {
            var delta = match.Index - start;
            if (delta > 0) result.Add(input[start..match.Index]);
            var replacement = evaluator(match) ?? "";
            result.Add(replacement);
            start = match.Index + match.Length;
            if (++c >= count) break;
        }
        if (start < input.Length) result.Add(input[start..]);
        return result.Aggregate((a, b) => a + b);
    }
    public string[] Split(string input, int count = 0, int start = 0)
    {
        var matchs = this.Matches(input, start);
        var result = new List<string>();
        var c = start = 0;
        foreach (var match in matchs)
        {
            var delta = match.Index - start;
            if (delta > 0) result.Add(input[start..match.Index]);
            start = match.Index + match.Length;
            if (++c >= count) break;
        }
        if (start < input.Length) result.Add(input[start..]);
        return result.ToArray();
    }
}
