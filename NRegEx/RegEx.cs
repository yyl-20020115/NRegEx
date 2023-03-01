namespace NRegEx;

public record class Capture(int Start = 0, int End = -1, string? Value = null);

public delegate string CaptureEvaluator(Capture capture);
public class Regex
{
    public static string[] Split(string input, string pattern, int count = 0, int start = 0)
        => new Regex(pattern).Split(input, count, start);

    public static string Replace(string input, string pattern, string replacement, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, replacement, count, start);

    public static string Replace(string input, string pattern, CaptureEvaluator evaluator, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, evaluator, count, start);

    public static bool IsMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsMatch(input, start, length);
    public static bool IsFullyMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsFullyMatch(input, start, length);
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
    //public static bool IsBacktracingFriendly(string regex)
    //    => !string.IsNullOrEmpty(regex)
    //    && Graph.IsBacktracingFriendly(new Regex(regex).Graph);
    public readonly RegExNode Model;
    public readonly Graph Graph;
    public readonly Options Options;
    public readonly string Pattern;
    public readonly string Name;
    public Regex(string pattern, string? name = null, Options options = Options.PERL)
    {
        this.Pattern = pattern;
        this.Name = name ?? this.Pattern;
        this.Options = options;
        this.Model = RegExDomParser.Parse(this.Name, this.Pattern, this.Options);
        this.Graph = RegExGraphBuilder.Build(this.Model, 0,
            (this.Options & Options.FOLD_CASE) == Options.FOLD_CASE);
    }
    public bool IsMatch(string input, int start = 0, int length = -1)
    {
        int sp = 0, ep = 0;
        return this.IsMatchInternal(input, start, length,ref sp, ref ep);
    }
    protected bool IsMatchInternal(string input, int start, int length,ref int sp, ref int ep)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));

        int i = this.MatchFindStart(input, start);
        int o = sp = ep = i;
        while (i >= 0 && i < length)
        {
            if (this.IsMatchInternal(input, i, length, ref o, false))
            {
                ep = o;
                return true;
            }
            else if (o > i)
                i = this.MatchFindStart(input, o);
            else
                break;
        }
        return false;
    }
    protected int MatchFindStart(string input, int start)
    {
        //^should always starts with start of input
        if (this.Pattern.StartsWith('^') && start>0)
        {
            return -1;
        }
        else if (start < input.Length)
        {
            var heads = this.Graph.Nodes.Where(n => n.Inputs.Count == 0).ToArray();
            var nodes = new HashSet<Node>();
            foreach (var head in heads)
            {
                head.FetchNodes(nodes, true);
            }
            for(int i = start; i < input.Length; i++)
            {
                if (nodes.Any(n => n.TryHit(input[i]).GetValueOrDefault()))
                    return i;
            }
        }
        return -1;
    }
    public bool IsFullyMatch(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        return IsMatchInternal(input, start, length, ref start, true);
    }
    protected bool IsMatchInternal(string input, int start, int length, ref int i, bool strict)
    {
        if (length == 0 && RegExGraphBuilder.HasPassThrough(this.Graph)) return true;
        i = start;
        var heads = this.Graph.Nodes.Where(n => n.Inputs.Count == 0);
        var nodes = heads.ToHashSet();
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
        return strict
            ? i == input.Length
                && RegExGraphBuilder.HasPassThrough(this.Graph, nodes.ToArray())
            : i <= input.Length
                && (nodes.Count==0 || RegExGraphBuilder.HasPassThrough(this.Graph, nodes.ToArray()));
    }
    public Capture? Match(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));

        int sp = 0, ep = 0;
        return this.IsMatchInternal(input, start, length, ref sp, ref ep) 
            ? new Capture(sp, ep, input[sp..ep]) : null;
    }

    public List<Capture> Matches(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));

        var captures = new List<Capture>();
        while (true)
        {
            var capture = this.Match(input, start, length);
            if (null == capture)
                break;
            else
            {
                if (capture.Start < 0 || capture.End<0||capture.End<capture.Start)
                    break;
                captures.Add(capture);

                start = capture.Start;
                length -= start;
            }
            if (capture.End >= input.Length) break;
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
            var delta = match.Start - start;
            if (delta > 0) result.Add(input[start..match.Start]);
            var replacement = evaluator(match) ?? "";
            result.Add(replacement);
            start = match.End;
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
            var delta = match.Start - start;
            if (delta > 0) result.Add(input[start..match.Start]);
            start = match.End;
            if (++c >= count) break;
        }
        if (start < input.Length) result.Add(input[start..]);
        return result.ToArray();
    }
}
