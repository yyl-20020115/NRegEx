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
    public readonly DualDictionary<string, int> NamedGroups;
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
        this.NamedGroups = new(Parser.NamedGroups);
        this.RequestTextBegin = Parser.RequestTextBegin;
        this.RequestTextEnd = Parser.RequestTextEnd;
        this.Graph = RegExGraphBuilder.Build(this.Model, 0,
            (this.Options & Options.FOLD_CASE) == Options.FOLD_CASE);
    }
    public bool IsMatch(string input, int start = 0, int length = -1, int direction = 1)
    {
        Node? last = null;
        int sp = 0, ep = 0;
        return this.IsMatchInternal(input, start, length, ref sp, ref ep, ref last, null, Math.Sign(direction));
    }
    protected bool IsMatchInternal(string input, int first, int length, ref int sp, ref int ep, ref Node? last, ListLookups<int, int>? groups = null, int direction = 1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (first < 0 || first > input.Length) throw new ArgumentOutOfRangeException(nameof(first));
        if (length < 0) length = input.Length - first;
        if (first + length > input.Length) throw new ArgumentOutOfRangeException(nameof(first) + "_" + nameof(length));
        direction = Math.Sign(direction);
        var tail = first + length;
        var start = direction >= 0 ? first : tail - 1;

        int i = this.MatchFindStart(input, first, tail, direction);

        int o = sp = ep = i;

        while (i >= first && i < tail)
        {
            length -= Math.Abs(i - start);

            if (this.IsMatchInternal(input, i, length, ref o, ref last, false, groups, direction))
            {
                ep = o;
                return true;
            }
            else if (direction >= 0 && o > i)
            {
                //we can not use o, we can only use i+1
                //because o can bring mistakes
                i = this.MatchFindStart(input, i + 1, tail, direction);
            }
            else if (direction < 0 && o < i)
            {
                //we can not use o, we can only use i-1
                //because o can bring mistakes
                i = this.MatchFindStart(input, i - 1, tail, direction);
            }
            else
                break;
        }
        return false;
    }
    protected int MatchFindStart(string input, int first, int tail, int direction)
    {
        if (first < tail)
        {
            var last = tail - 1;
            direction = Math.Sign(direction);
            var heads = this.Graph.Nodes.Where(n => !(direction >= 0 ? n.HasInput : n.HasOutput)).ToArray();
            var nodes = new HashSet<Node>();
            foreach (var head in heads)
            {
                head.FetchNodes(nodes, true, direction);
            }
            if (direction < 0) (first, last) = (last, first);
            for (int i = first; i != last + direction; i += direction)
            {
                this.UpdateIndicators(input, i, first, tail, direction);
                foreach (var node in nodes)
                {
                    //by pass indicators if indicator matches
                    if (node.IsIndicator && this.TryHitHode(node))
                    {
                        var subs = new HashSet<Node>();
                        node.FetchNodes(subs, true, direction);
                        foreach (var sub in subs)
                        {
                            if (sub.TryHit(input[i]) == true)
                            {
                                return i;
                            }
                        }
                    }
                }
            }
        }
        return -1;
    }
    public bool IsFullyMatch(string input, int start = 0, int length = -1, int direction = 1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        Node? last = null;
        return IsMatchInternal(input, start, length, ref start, ref last, true, null, direction);
    }
    protected bool IsMatchInternal(string input, int first, int length, ref int i, ref Node? last, bool strict, ListLookups<int, int>? groups = null, int direction = +1)
    {
        if (length == 0 && RegExGraphBuilder.HasPassThrough(this.Graph)) return true;
        direction = Math.Sign(direction);
        var start = first;
        var tail = first + length;
        var end = tail - 1;
        if (direction < 0)
        {
            (start, end) = (end, start);
        }

        i = start;
        this.UpdateIndicators(input, i, first, tail, direction);
        var heads = this.Graph.Nodes.Where(n => !(direction >= 0 ? n.HasInput : n.HasOutput));
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
                    //hit = true;
                    //no advance
                    node.FetchNodes(nodes, true, direction);
                    last = node;
                }
                else
                {
                    var d = node.TryHit(c);
                    if (d is null)
                    {
                        node.FetchNodes(nodes, true, direction);
                    }
                    else if (d.Value)
                    {
                        if (groups != null)
                            foreach (var cap in node.Groups)
                                groups[cap].Add(i);
                        hit = true;
                        node.FetchNodes(nodes, false, direction);
                        last = node;
                    }
                }
            }
            if (hit)
            {
                i += direction;
                this.UpdateIndicators(input, i, first, tail, direction);
            }
        }
        return strict //this means having $ in the end
            ? ((i == tail - 1)
                && RegExGraphBuilder.HasPassThrough(this.Graph, nodes, direction))
            : ((i > start && i < tail)
                && (nodes.Count == 0 ||
                        RegExGraphBuilder.HasPassThrough(this.Graph, nodes, direction)));
    }

    protected bool[] Indicators = new bool[8];

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

    protected void UpdateIndicators(string input, int i, int first, int tail, int direction = +1)
    {

        int start = first;
        if (!(i >= first && i < tail)) return;

        int end = tail - 1;
        char? Last = i > start && i < tail ? input[i - direction] : null;
        char? This = i >= start && i < tail ? input[i + 0] : null;
        char? Next = i < end ? input[i + direction] : null;

        if (direction < 0) (start, end) = (end, first);

        this.Indicators[BeginTextIndex] = i == start;
        this.Indicators[EndTextIndex] = i == end;

        this.Indicators[BeginLineIndex] = Last is '\n' or null;
        this.Indicators[EndLineIndex] = This == '\n';

        this.Indicators[BeginWordIndex]
            = (Last is null || !Unicode.IsRuneWord(Last.Value))
            && (This is not null && Unicode.IsRuneWord(This.Value));

        this.Indicators[EndWordIndex]
            = (This is not null && Unicode.IsRuneWord(This.Value))
            && (Next is null || !Unicode.IsRuneWord(Next.Value));

        this.Indicators[WordBoundaryIndex]
            = this.Indicators[BeginWordIndex]
            || this.Indicators[EndWordIndex]
            ;

        this.Indicators[NotWordBoundaryIndex]
            = !this.Indicators[WordBoundaryIndex];
    }
    protected bool TryHitHode(Node node)
    {
        if (!node.IsLink)
        {
            foreach (var kv in this.IndicatorsDict)
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
    public Match Match(string input, int start = 0, int length = -1, int direction = 1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        direction = Math.Abs(direction);
        Node? last = null;
        var groups = new ListLookups<int, int>();
        int sp = 0, ep = 0;
        var ret = this.IsMatchInternal(input, start, length, ref sp, ref ep, ref last, groups, direction);
        if (ret)
        {
            var name = this.Name;
            if (last?.Parent?.SourceNode is RegExNode r)
                name = r.PatternName ?? name;
            return this.BuildMatch(input, name, sp, ep, groups, direction);
        }
        return new Match(this, input, false);
    }
    protected Match BuildMatch(string input, string name, int sp, int ep, ListLookups<int, int>? groups, int direction)
    {
        var match = new Match(this, input, true, name, sp, ep, input[sp..ep]);
        //group 0
        match.Groups.Add(match);

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
                foreach (var thisCapturePositions in SplitList(thisGroupPositions.ToArray(), direction))
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
    protected static int[][] SplitList(int[] list, int direction = 1)
    {
        var parts = new List<int[]>();

        direction = Math.Abs(direction);

        if (list.Length <= 1) return new int[][] { list };
        retry:
        for (int i = 0; i < list.Length; i++)
        {
            int c = list[i + 0];
            int d = list[i + 1];
            if (d != c + direction)
            {
                parts.Add(list[..(i + 1)]);
                list = list[i..];
                goto retry;
            }
        }
        if (direction < 0)
        {
            var copy = parts.ToArray();
            parts.Clear();
            foreach (var part in copy)
            {
                Array.Reverse(part);
                parts.Add(part);
            }
        }
        return parts.ToArray();

    }
    public List<Match> Matches(string input, int first = 0, int length = -1, int direction = +1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (first < 0 || first > input.Length) throw new ArgumentOutOfRangeException(nameof(first));
        if (length < 0) length = input.Length - first;
        if (first + length > input.Length) throw new ArgumentOutOfRangeException(nameof(first) + "_" + nameof(length));
        direction = Math.Abs(direction);
        var tail = first + length;
        var matches = new List<Match>();
        while (true)
        {
            var match = this.Match(input, first, length, direction);
            if (!match.Success)
                break;
            else
            {
                matches.Add(match);

                length -= Math.Abs(match.ExclusiveEnd - first);
                first = direction >= 0 ? match.ExclusiveEnd : match.InclusiveStart;
            }
            if (direction >= 0 && match.ExclusiveEnd >= tail) break;
            else if (direction < 0 && match.InclusiveStart <= first) break;
        }
        return matches;
    }
    public string ReplaceFirst(string input, string replacement, int start = 0)
        => this.ReplaceFirst(input, (Capture capture) => replacement, start);
    public string ReplaceFirst(string input, string replacement, Match match, int start = 0)
        => this.ReplaceFirst(input, (Capture capture) => replacement, match, start);
    public string ReplaceFirst(string input, CaptureEvaluator evaluator, int start = 0)
        => this.ReplaceFirst(input, evaluator, this.Match(input, start), start);
    public string ReplaceFirst(string input, CaptureEvaluator evaluator, Match match, int start = 0)
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
    public string ReplaceAll(string input, string replacement, List<Match> matches, int start = 0)
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
    public string[] Split(string input, List<Match> matches, int start = 0)
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
