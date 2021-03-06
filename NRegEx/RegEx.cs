using System.Text;

namespace NRegEx;

public record class Capture(int Index = 0,int Length = -1, string? Value = null);

public delegate string CaptureEvaluator(Capture capture);
public class Regex
{
    public const string MetaChars = "|()[]{}^$*+?\\ #";
    private static bool IsMetachar(char ch) => MetaChars.IndexOf(ch)>=0;

    public static string Escape(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            if (IsMetachar(input[i]))
            {
                return EscapeImpl(input, i);
            }
        }

        return input;
    }

    private static string EscapeImpl(string input, int i)
    {
        var vsb = new StringBuilder(input.Length*3);

        char ch = input[i];
        vsb.Append(input.AsSpan(0, i));

        do
        {
            vsb.Append('\\');
            switch (ch)
            {
                case '\n':
                    ch = 'n';
                    break;
                case '\r':
                    ch = 'r';
                    break;
                case '\t':
                    ch = 't';
                    break;
                case '\f':
                    ch = 'f';
                    break;
            }

            vsb.Append(ch);
            i++;
            int lastpos = i;

            while (i < input.Length)
            {
                ch = input[i];
                if (IsMetachar(ch))
                {
                    break;
                }

                i++;
            }

            vsb.Append(input.AsSpan(lastpos, i - lastpos));
        } while (i < input.Length);

        return vsb.ToString();
    }

    public static string Unescape(string input)
    {
        int i = input.IndexOf('\\');
        return i >= 0 ?
            UnescapeImpl(input, i) :
            input;
    }

    private static string UnescapeImpl(string input, int i)
    {
        var vsb = new StringBuilder(input.Length * 3);

        vsb.Append(input.AsSpan(0, i));
        do
        {
            i++;
            if (i == input.Length - 1)
            {
                vsb.Append(input[i]);
            }
            else //i<input.Length -1
            {
                char ch = input[i];
                if (ch == '\\')
                {
                    i++;
                    ch = input[i];
                    switch (ch)
                    {
                        case 'n':
                            ch = '\n';
                            break;
                        case 'r':
                            ch = '\r';
                            break;
                        case 't':
                            ch = '\t';
                            break;
                        case 'f':
                            ch = '\f';
                            break;
                        default:
                            if (IsMetachar(ch))
                            {
                                //ch is ok
                            }
                            else
                            {
                                i--;
                                //ch not changed
                            }
                            break;
                    }


                    vsb.Append(ch);
                } 
            }
 

            int lastpos = i;
            while (i < input.Length && input[i] != '\\')
            {
                i++;
            }

            vsb.Append(input.AsSpan(lastpos, i - lastpos));
        } while (i < input.Length);

        return vsb.ToString();
    }

    public static string[] Split(string input, string pattern, int count = 0, int start =0)
        => new Regex(pattern).Split(input, count, start);

    public static string Replace(string input, string pattern,string replacement, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, replacement, count, start);

    public static string Replace(string input, string pattern, CaptureEvaluator evaluator, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, evaluator, count, start);

    public static bool IsMatch(string input, string pattern, int start = 0, int length = -1) 
        => new Regex(pattern).IsMatch(input, start,length);
    public static Capture Match(string input, string pattern, int start = 0, int length = -1) 
        => new Regex(pattern).Match(input, start, length);
    public static List<Capture> Matches(string input, string pattern, int start = 0, int length = -1) 
        => new Regex(pattern).Matches(input, start, length);
    /// <summary>
    /// We should check easy back tracing regex first
    /// or we'll have to accept the early (not lazy or greedy) result for sure.
    /// 
    /// </summary>
    /// <param name="regex"></param>
    /// <returns></returns>
    public static bool IsBacktracingFriendly(string regex)
        => !string.IsNullOrEmpty(regex) 
        && new Regex(regex).Graph.IsBacktracingFriendly();
    
    protected string regex = "";
    protected string name = "";
    protected Graph? graph;
    public Graph Graph =>graph??=this.Build();
    public string Pattern => regex;
    public string Name => name;
    public Regex(string regex, string? name = null)
    {
        this.regex = regex;
        this.name = name ?? this.regex;
    }

    protected Graph Build() => new RegExParser(this.name).FullParse(this.regex);

    public bool IsMatch(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start)+","+nameof(length));

        var heads = this.Graph.Heads;        
        var nodes = heads.ToHashSet();

        var i = start;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            if (copies.All(copy => copy.IsVirtual))
            {
                nodes.UnionWith(copies.SelectMany(n => n.Outputs));
            }
            else
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    if (node.Hit(c))
                    {
                        hit = true;
                        //needs all hits
                        nodes.UnionWith(node.Outputs);
                    }
                }
                if (hit) i++;
            }
        }

        return i == input.Length && (nodes.Count == 0 || nodes.Any(n => n.Outputs.Count == 0));
    }
    public Capture Match(string input,int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "," + nameof(length));
        var s = start; 
        var heads = this.Graph.Heads;
    repeat:

        var nodes = heads?.ToHashSet() ?? new ();
        var i = start;
        var m = 0;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            if (copies.All(copy => copy.IsVirtual))
            {
                nodes.UnionWith(copies.SelectMany(n => n.Outputs));
            }
            else
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    if (node.Hit(c))
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
                    start+=m;
                    goto repeat;
                }
            }
        }

        return start > s && nodes.Count==0
            ? new (start, m, input[start..(start+m)])
            : new (start,-1);
    }
    public List<Capture> Matches(string input, int start = 0,int length = -1)
    {
        var captures = new List<Capture>();
        while(true)
        {
            var capture = this.Match(input, start, length);
            if (null == capture)
            {
                break;
            }
            else
            {
                if (capture.Index<0 || capture.Length == 0) 
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

        start = 0;
        var c = 0;
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

        return result.Aggregate((a,b)=>a+b);
    }
    public string[] Split(string input, int count = 0, int start = 0)
    {
        var matchs = this.Matches(input, start);
        var result = new List<string>();

        start = 0;
        var c = 0;
        foreach (var match in matchs)
        {
            var delta = match.Index - start;
            if (delta>0) result.Add(input[start..match.Index]);
            start =match.Index + match.Length;
            if (++c >= count) break;
        }
        if (start < input.Length) result.Add(input[start..]);

        return result.ToArray();
    }
}

