using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace NRegEx;

public delegate string CaptureEvaluator(Capture capture);
public class Regex
{
    public static string[] Split(string input, string pattern, int start = 0)
        => new Regex(pattern).Split(input, start);
    public static string ReplaceFirst(string input, string pattern, string replacement, int start = 0)
        => new Regex(pattern).ReplaceFirst(input, replacement, start);

    public static string ReplaceFirst(string input, string pattern, CaptureEvaluator evaluator, int start = 0)
        => new Regex(pattern).ReplaceFirst(input, evaluator, start);

    public static string ReplaceAll(string input, string pattern, string replacement, int start = 0)
        => new Regex(pattern).ReplaceAll(input, replacement, start);

    public static string ReplaceAll(string input, string pattern, CaptureEvaluator evaluator, int start = 0)
        => new Regex(pattern).ReplaceAll(input, evaluator, start);

    public static bool IsMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsMatch(input, start, length);
    public static bool IsFullyMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsFullyMatch(input, start, length);
    public static Match Match(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).Match(input, start, length);
    public static List<Match> Matches(string input, string pattern, int start = 0, int length = -1)
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
    public readonly Options Options;
    public readonly string Pattern;
    public readonly string Name;
    public readonly DualDictionary<string, int> NamedGroups =new();
    public RegExNode Model { get; protected set; }
    public Graph Graph { get; protected set; }

    protected readonly bool RequestTextBegin;
    protected readonly bool RequestTextEnd;

    public Regex(string pattern, string? name = null, Options options = Options.PERL)
    {
        this.Pattern = pattern;
        this.Name = name ?? this.Pattern;
        this.Options = options;
        var Parser = new RegExDomParser(this.Name, this.Pattern, this.Options);
        this.Model = Parser.Parse();
        foreach(var pair in Parser.NamedGroups)
        {
            this.NamedGroups.Add(pair.Key, pair.Value);
        }
        this.RequestTextBegin = Parser.RequestTextBegin;
        this.RequestTextEnd = Parser.RequestTextEnd;
        this.Graph = RegExGraphBuilder.Build(this.Model, 0,
            (this.Options & Options.FOLD_CASE) == Options.FOLD_CASE);
    }
    public bool IsMatch(string input, int start = 0, int length = -1)
    {
        Node? last = null;
        int sp = 0, ep = 0;
        return this.IsMatchInternal(input, start, length, ref sp, ref ep, ref last);
    }
    protected bool IsMatchInternal(string input, int start, int length, ref int sp, ref int ep,ref Node? last,ListLookups<int,int>? groups = null)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        if (this.RequestTextBegin && start > 0) throw new ArgumentOutOfRangeException(nameof(start));

        var tail = start + length;

        int i = this.MatchFindStart(input, start, tail);
        int o = sp = ep = i;
        while (i >= 0 && i < tail)
        {
            length -= (i - start);
            if (this.IsMatchInternal(input, i, length, ref o,ref last, false,groups))
            {
                ep = o;
                return true;
            }
            else if (o > i)
            {
                //we can not use o, we can only use i+1
                //because o can bring mistakes
                i = this.MatchFindStart(input, i + 1, tail);
            }
            else
                break;
        }
        return false;
    }
    protected int MatchFindStart(string input, int start, int tail)
    {
        if (start < tail)
        {
            var heads = this.Graph.Nodes.Where(n => n.Inputs.Count == 0).ToArray();
            var nodes = new HashSet<Node>();
            foreach (var head in heads)
            {
                head.FetchNodes(nodes, true);
            }
            for (int i = start; i < tail; i++)
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
        if (this.RequestTextBegin && start > 0) throw new ArgumentOutOfRangeException(nameof(start));
        Node? last = null;
        return IsMatchInternal(input, start, length, ref start,ref last, true);
    }
    protected bool IsMatchInternal(string input, int start, int length, ref int i,ref Node? last, bool strict, ListLookups<int, int>? groups = null)
    {
        if (length == 0 && RegExGraphBuilder.HasPassThrough(this.Graph)) return true;
        var tail = start + length;
        i = start;
        this.UpdateIndicators(input, i, start, length);
        var heads = this.Graph.Nodes.Where(n => n.Inputs.Count == 0);
        var nodes = heads.ToHashSet();
        while (nodes.Count > 0 && i < tail)
        {
            var c = input[i];
            var hit = false;
            var copies = nodes.ToArray();
            nodes.Clear();
            foreach (var node in copies)
            {
                //this is for BEGIN_LINE etc
                if (this.TryHitHode(node))
                {
                    hit = true;
                    last = node.FetchNodes(nodes, false);
                }
                else
                {
                    var d = node.TryHit(c);
                    if (!d.HasValue)
                    {
                        node.FetchNodes(nodes, true);
                    }
                    else if (d.Value)
                    {
                        if (groups != null)
                            foreach (var cap in node.Captures)
                                groups[cap].Add(i);
                        hit = true;
                        last = node.FetchNodes(nodes, false);
                    }
                }
            }
            if (hit)
            {
                this.UpdateIndicators(input,++i,start,length);
            }
        }
        return strict || this.RequestTextEnd //this means having $ in the end
            ? i == input.Length
                && RegExGraphBuilder.HasPassThrough(this.Graph, nodes.ToArray())
            : i <= input.Length
                && (nodes.Count == 0 || RegExGraphBuilder.HasPassThrough(this.Graph, nodes.ToArray()));
    }

    protected bool[] Indicators =new bool[8];

    protected const int BeginTextIndex = 0;
    protected const int EndTextIndex = 1;
    protected const int BeginLineIndex = 2;
    protected const int EndLineIndex = 3;
    protected const int BeginWordIndex = 4;
    protected const int EndWordIndex = 5;
    protected const int WordBoundaryIndex = 6;
    protected const int NotWordBoundaryIndex = 7;

    protected Dictionary<int, int> IndicatorsDict = new()
    {
        [BeginTextIndex] = RegExTextReader.BEGIN_TEXT,
        [EndTextIndex] = RegExTextReader.END_TEXT,
        [BeginLineIndex] = RegExTextReader.BEGIN_LINE,
        [EndLineIndex] = RegExTextReader.END_LINE,
        [BeginWordIndex] = RegExTextReader.BEGIN_WORD,
        [EndWordIndex] = RegExTextReader.END_WORD,
        [WordBoundaryIndex] = RegExTextReader.WORD_BOUNDARY,
        [NotWordBoundaryIndex] = RegExTextReader.NOT_WORD_BOUNDARY,
    };
    protected char? last = null;
    protected void UpdateIndicators(string input, int i, int start, int length)
    {
        int tail = start + length;
        char? Last = i > 0 && i < tail ? input[i - 1] : null;
        char? This = i >= 0 && i < tail ? input[i + 0] : null;
        char? Next = i + 1 < tail ? input[i + 1] : null;

        this.Indicators[BeginTextIndex] = i == start;
        this.Indicators[EndTextIndex] = i == tail - 1;

        this.Indicators[BeginLineIndex] = Last == '\n';
        this.Indicators[EndLineIndex] = This == '\n';
        
        this.Indicators[BeginWordIndex] 
            = (Last is null || !Unicode.IsRuneWord(Last.Value)) 
            && (This is not null && Unicode.IsRuneWord(This.Value));

        this.Indicators[EndWordIndex] 
            = (This is not null && Unicode.IsRuneWord(This.Value))
            && (Next is null || !Unicode.IsRuneWord(Next.Value));

        this.Indicators[WordBoundaryIndex] 
            =  this.Indicators[BeginWordIndex]
            || this.Indicators[EndWordIndex]
            ;

        this.Indicators[NotWordBoundaryIndex] 
            = !this.Indicators[WordBoundaryIndex];
    }
    protected bool TryHitHode(Node node)
    {
        if (!node.IsLink)
        {   
            foreach(var kv in this.IndicatorsDict)
            {
                if (Check(this.Indicators[kv.Key], node, kv.Value))
                    return true;
            }
        }
        return false;
    }
    protected static bool Check(bool value, Node node, int other)
        => value && Check(node.CharsArray, other);
    protected static bool Check(int[]? values, int i)
        => values is not null && Array.IndexOf(values, i) >= 0;
    public Match Match(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        if (this.RequestTextBegin && start > 0) throw new ArgumentOutOfRangeException(nameof(start));
        Node? last = null;
        var groups = new ListLookups<int,int>();
        int sp = 0, ep = 0;
        var ret = this.IsMatchInternal(input, start, length, ref sp, ref ep, ref last,groups);
        if (ret)
        {
            var name = this.Name;
            if (last?.Parent?.SourceNode is RegExNode r)
                name = r.PatternName ?? name;

            //TODO: build matches
            return this.BuildMatch(input, name, sp, ep, groups);;
        }
        return new Match(this,input, false);
    }
    protected Match BuildMatch(string input, string name, int sp, int ep, ListLookups<int, int>? groups)
    {
        var match = new Match(this, input, true, name, sp, ep, input[sp..ep]);
        if (groups != null)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var groupName = this.NamedGroups.TryGetValue(i, out var n) ? n : i.ToString();
                var group = new Group(true,
                    Name: groupName);

                match.Groups.Add(group);    

                var thisGroupPositions = groups[i];
                int c = 0;
                foreach (var thisCapturePositions in SplitList(thisGroupPositions.ToArray()))
                {
                    var builder = new StringBuilder();
                    foreach (var position in thisCapturePositions)
                    {
                        builder.Append(input[position]);
                    }
                    group.Captures.Add(new Capture(
                        $"{groupName}-{c}", 
                        thisCapturePositions.Min(), 
                        thisCapturePositions.Max() + 1, 
                        builder.ToString()));
                }
            }
        }
        return match;
    }
    protected static int[][] SplitList(int[] list)
    {
        var parts = new List<int[]>();

        if (list.Length <= 1) return new int[][] { list };
    retry:
        for (int i = 0; i < list.Length; i++)
        {
            int c = list[i + 0];
            int d = list[i + 1];
            if (d != c + 1)
            {
                parts.Add(list[..(i+1)]);
                list = list[i..];
                goto retry;
            }
        }
        return parts.ToArray();

    }
    public List<Match> Matches(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        if (this.RequestTextBegin && start > 0) throw new ArgumentOutOfRangeException(nameof(start));
        var tail = start + length;
        var matches = new List<Match>();
        while (true)
        {
            var match = this.Match(input, start, length);
            if (null == match)
                break;
            else
            {
                if (match.InclusiveStart < 0 || match.ExclusiveEnd < 0 || match.ExclusiveEnd < match.InclusiveStart)
                    break;
                matches.Add(match);

                length -= (match.ExclusiveEnd - start);
                start = match.ExclusiveEnd;
            }
            if (match.ExclusiveEnd >= tail) break;
        }
        return matches;
    }
    public string ReplaceFirst(string input, string replacement, int start = 0)
        => this.ReplaceFirst(input, (Capture capture) => replacement, start);
    public string ReplaceFirst(string input, string replacement,Match match, int start = 0)
        => this.ReplaceFirst(input, (Capture capture) => replacement, match, start);
    public string ReplaceFirst(string input, CaptureEvaluator evaluator, int start = 0)
        => this.ReplaceFirst(input, evaluator, this.Match(input, start), start);
    public string ReplaceFirst(string input, CaptureEvaluator evaluator,Match match, int start = 0)
    {
        if (match is not null)
        {
            var result = new List<string>();
            var delta = match.InclusiveStart - start;
            if (delta > 0) result.Add(input[start..match.InclusiveStart]);
            var replacement = evaluator(match) ?? "";
            result.Add(replacement);
            start = match.ExclusiveEnd;
            if (start < input.Length) result.Add(input[start..]);
            return result.Aggregate((a, b) => a + b);
        }
        return input;
    }

    public string ReplaceAll(string input, string replacement, int start = 0)
        => this.ReplaceAll(input, (Capture capture) => replacement, start);
    public string ReplaceAll(string input, string replacement,List<Match> matches, int start = 0)
        => this.ReplaceAll(input, (Capture capture) => replacement, matches, start);
    public string ReplaceAll(string input, CaptureEvaluator evaluator, int start = 0) 
        => ReplaceAll(input, evaluator, Matches(input, start), start);
    public string ReplaceAll(string input, CaptureEvaluator evaluator, List<Match> matches, int start = 0)
    {
        var result = new List<string>();
        foreach (var match in matches)
        {
            var delta = match.InclusiveStart - start;
            if (delta > 0) result.Add(input[start..match.InclusiveStart]);
            var replacement = evaluator(match) ?? "";
            result.Add(replacement);
            start = match.ExclusiveEnd;
        }
        if (start < input.Length) result.Add(input[start..]);
        return result.Count > 0 ? result.Aggregate((a, b) => a + b) : input;
    }
    public string[] Split(string input, int start = 0) 
        => this.Split(input, this.Matches(input, start), start);
    public string[] Split(string input,List<Match> matches, int start = 0)
    {
        var result = new List<string>();
        foreach (var match in matches)
        {
            var delta = match.InclusiveStart - start;
            if (delta > 0) result.Add(input[start..match.InclusiveStart]);
            start = match.ExclusiveEnd;
        }
        if (start < input.Length) result.Add(input[start..]);
        return result.ToArray();
    }
}
