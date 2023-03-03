using System;
using System.Text;
using System.Xml.Linq;

namespace NRegEx;

public delegate string CaptureEvaluator(Capture capture);
public class Regex
{
    public static string[] Split(string input, string pattern, int start = 0, int length = -1, bool reverselySearch = false)
        => new Regex(pattern).Split(input, start, length, reverselySearch);
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
    protected readonly Dictionary<int, Graph> BackRefPoints;
    protected readonly Dictionary<int, Graph> GroupGraphs;
    protected readonly Dictionary<int, GroupType> GroupTypes;
    protected readonly ListLookups<int, Graph> ConditionsGraphs;

    public RegExNode Model { get; protected set; }
    public Graph Graph { get; protected set; }

    public Regex(string pattern, string? name = null, Options options = Options.PERL)
    {
        this.Pattern = pattern;
        this.Name = name ?? this.Pattern;
        this.Options = options;
        var Parser = new RegExDomParser(this.Name, this.Pattern, this.Options);
        this.Model = Parser.Parse();
        this.NamedGroups = new(Parser.NamedGroups);
        var Builder = new RegExGraphBuilder();
        this.Graph = Builder.Build(this.Model, 0,
            (this.Options & Options.CASE_INSENSITIVE) == Options.CASE_INSENSITIVE);
        this.BackRefPoints = Builder.BackRefPoints;
        this.GroupTypes = Builder.GroupTypes;
        this.GroupGraphs = Builder.GroupGraphs;
        this.ConditionsGraphs = Builder.ConditionsGraphs;
    }
    public bool IsMatch(string input, int first = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (first < 0 || first > input.Length) throw new ArgumentOutOfRangeException(nameof(first));
        if (length < 0) length = input.Length - first;
        if (first + length > input.Length) throw new ArgumentOutOfRangeException(nameof(first) + "_" + nameof(length));

        Node? last = null;
        int sp = 0, ep = 0;
        return IsMatchInternal(this.Graph, input, first, length, ref sp, ref ep, ref last, null, reversely ? -1 : 1, this.Options, this.TryWithGroups);
    }
    public bool IsFullyMatch(string input, int start = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        Node? last = null;
        return IsMatchCore(this.Graph, input, start, length, ref start, ref last, true, null, reversely ? -1 : 1, this.Options, this.TryWithGroups);
    }
    public delegate bool? TryWithGroupsFunction(Node node, string input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, int direction);
    protected bool? TryWithGroups(Node node, string input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, int direction)
    {
        if (groups != null)
        {
            foreach (var index in node.Groups)
            {
                if (this.GroupTypes.TryGetValue(index, out var type))
                {
                    this.TryWithGroup(node, input, i, groups, backs, direction, index, type);
                }
            }
        }
        return null;
    }

    protected bool? TryWithGroup(Node node, string input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, int direction,int index, GroupType type)
    {
        switch (type)
        {
            case GroupType.NotGroup:
            case GroupType.NotCaptiveGroup:
                break;
            case GroupType.NormalGroup:
            case GroupType.AtomicGroup:
                if (groups != null)
                {
                    EmitPosition(node, i, groups[index]);
                    if (this.BackRefPoints.TryGetValue(index, out var graph) && graph != null)
                    {
                        graph.InsertPointBeforeTail(new(input[i]) { Parent = graph });
                        backs.Add(graph.Nodes.Single(n => n.Groups.Contains(i)));
                    }
                }
                break;
            case GroupType.LookAroundConditionGroup:
            case GroupType.NamedBackReferenceConditionGroup:
            case GroupType.IndexedBackReferenceConditionGroup:
                {
                    //TODO: how to do conditions 
                }
                break;
            case GroupType.BackReferenceCondition:
                {
                    //TODO: how to do conditions 
                }
                break;
            default:
                return this.TryWithConditionGroup(node, input, i, groups, backs, direction, index, type);
        }
        return null;
    }
    protected bool? TryWithConditionGroup(Node node, string input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, int direction, int index, GroupType type)
    {
        switch (type)
        {
            case GroupType.ForwardPositiveGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return VerifyMatchCore(g, input, 0, input.Length, i, direction, Options);
                    }
                }
                break;
            case GroupType.ForwardNegativeGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return !VerifyMatchCore(g, input, 0, input.Length, i, direction, Options);
                    }
                }
                break;
            case GroupType.BackwardPositiveGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return VerifyMatchCore(g, input, 0, input.Length, i, -direction, Options);
                    }
                }
                break;
            case GroupType.BackwardNegativeGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return !VerifyMatchCore(g, input, 0, input.Length, i, -direction, Options);
                    }
                }
                break;
        }
        return null;
    }

    protected static void EmitPosition(Node node, int i, ICollection<List<int>> captures)
    {
        if ((node.Ending & Ending.Start) == Ending.Start
            || captures.Count == 0)
        {
            captures.Add(new());
        }
        captures.Last().Add(i);
    }
    protected static bool TryHitHode(Node node, RegExIndicators indicators)
    {
        if (!node.IsLink)
        {
            foreach (var kv in indicators.IndicatorsDict)
            {
                if (Check(indicators.Indicators[kv.Key], node, kv.Value))
                    return true;
            }
        }
        return false;
    }
    protected static bool Check(bool value, Node node, int other)
        => value && Check(node.CharsArray, other);
    protected static bool Check(int[]? values, int i)
        => values is not null && Array.IndexOf(values, i) >= 0;
    public Match Match(string input, int start = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));

        int direction = RegexHelpers.FixDirection(reversely ? -1 : 1);
        Node? last = null;
        var groups = new ListLookups<int, List<int>>();
        int sp = 0, ep = 0;
        var ret = IsMatchInternal(this.Graph, input, start, length, ref sp, ref ep, ref last, groups, direction, this.Options, this.TryWithGroups);
        if (ret)
        {
            var name = this.Name;
            if (last?.Parent?.SourceNode is RegExNode r)
                name = r.PatternName ?? name;
            return BuildMatch(this, input, name, sp, ep, groups, direction,this.NamedGroups);
        }
        return new Match(this, input, false);
    }
    public List<Match> Matches(string input, int first = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (first < 0 || first > input.Length) throw new ArgumentOutOfRangeException(nameof(first));
        if (length < 0) length = input.Length - first;
        if (first + length > input.Length) throw new ArgumentOutOfRangeException(nameof(first) + "_" + nameof(length));
        var tail = first + length;
        var matches = new List<Match>();
        while (true)
        {
            var match = this.Match(input, first, length, reversely);
            if (!match.Success)
                break;
            else
            {
                matches.Add(match);

                length -= Math.Abs((reversely ? match.InclusiveStart : match.ExclusiveEnd) - first);
                first = reversely ? match.InclusiveStart : match.ExclusiveEnd;
            }
            if (!reversely && match.ExclusiveEnd >= tail) break;
            else if (reversely && match.InclusiveStart <= first) break;
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
            var builder = new StringBuilder();
            var delta = match.InclusiveStart - start;
            if (delta > 0) builder.Append(input[start..match.InclusiveStart]);
            var replacement = evaluator(match) ?? "";
            builder.Append(replacement);
            start = match.ExclusiveEnd;
            if (start < input.Length) builder.Append(input[start..]);
            return builder.Length > 0 ? builder.ToString() : input;
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
        var builder = new StringBuilder();
        foreach (var match in matches)
        {
            var delta = match.InclusiveStart - start;
            if (delta > 0) builder.Append(input[start..match.InclusiveStart]);
            var replacement = evaluator(match) ?? "";
            builder.Append(replacement);
            start = match.ExclusiveEnd;
        }
        if (start < input.Length) builder.Append(input[start..]);
        return builder.Length>0? builder.ToString() : input;
    }
    public string[] Split(string input, int first = 0, int length = -1, bool reverselySearch = false)
        => this.Split(input, this.Matches(input, first, length, reverselySearch));
    public string[] Split(string input, List<Match> matches)
    {
        var first = 0;
        var result = new List<string>();
        foreach (var match in matches)
        {
            var delta = match.InclusiveStart - first;
            if (delta > 0) result.Add(input[first..match.InclusiveStart]);
            first = match.ExclusiveEnd;
        }
        if (first < input.Length) result.Add(input[first..]);
        return result.ToArray();
    }

    protected static bool IsMatchInternal(Graph graph, string input, int first, int length, ref int sp, ref int ep, ref Node? last, ListLookups<int, List<int>>? groups, int direction, Options options, TryWithGroupsFunction function)
    {
        direction = RegexHelpers.FixDirection(direction);
        var tail = first + length;
        var start = direction >= 0 ? first : tail - 1;

        int i = IsMatchFindStart(graph, input, first, tail, direction);

        int o = sp = ep = i;

        while (i >= first && i < tail)
        {
            length -= RegexHelpers.Abs(i - start);

            if (IsMatchCore(graph, input, i, length, ref o, ref last, false, groups, direction, options, function))
            {
                ep = o;
                return true;
            }
            else if (direction >= 0 && o > i)
            {
                //we can not use o, we can only use i+1
                //because o can bring mistakes
                i = IsMatchFindStart(graph, input, i + 1, tail, direction);
            }
            else if (direction < 0 && o < i)
            {
                //we can not use o, we can only use i-1
                //because o can bring mistakes
                i = IsMatchFindStart(graph, input, i - 1, tail, direction);
            }
            else
                break;
        }
        return false;
    }
    protected static int IsMatchFindStart(Graph graph, string input, int first, int tail, int direction)
    {
        var indicators = new RegExIndicators();

        direction = RegexHelpers.FixDirection(direction);
        if (first < tail)
        {
            var last = tail - 1;
            direction = Math.Sign(direction);
            var heads = graph.Nodes.Where(
                n => !(direction >= 0 ? n.HasInput : n.HasOutput)).ToArray();
            var nodes = new HashSet<Node>();
            foreach (var head in heads)
            {
                head.FetchNodes(nodes, true, direction);
            }
            if (direction < 0) (first, last) = (last, first);
            for (int i = first; i != last + direction; i += direction)
            {
                indicators.UpdateIndicators(input, i, first, tail, direction);
                foreach (var node in nodes)
                {
                    //by pass indicators if indicator matches
                    if (node.IsIndicator)
                    {
                        if (TryHitHode(node, indicators))
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
                    else if (node.TryHit(input[i]) == true)
                    {
                        return i;
                    }
                }
            }
        }
        return -1;
    }
    protected static bool VerifyMatchCore(
        Graph graph, string input, int first, int length,
        int i, int direction, Options options)
    {
        Node? last = null;
        return IsMatchCore(graph, input, first, length, ref i, ref last, false, null,direction, options, null);
    }

    protected static bool IsMatchCore(Graph graph, string input, int first, int length, ref int i, ref Node? last, bool strict, ListLookups<int, List<int>>? groups, int direction, Options options, TryWithGroupsFunction function)
    {
        if (length == 0 && GraphUtils.HasPassThrough(graph)) return true;
        var indicators = new RegExIndicators();

        direction = RegexHelpers.FixDirection(direction);

        var start = first;
        var tail = first + length;
        var end = tail - 1;
        if (direction < 0) (start, end) = (end, start);

        i = start;
        indicators.UpdateIndicators(input, i, first, tail, direction);
        var heads = graph.Nodes.Where(n => !(direction >= 0 ? n.HasInput : n.HasOutput));
        var nodes = heads.ToHashSet();
        var backs = new HashSet<Node>();
        while (nodes.Count > 0 && i >= start && i <= end)
        {
            var c = input[i];
            var hit = false;
            var quit = false;
            var copies = nodes.ToArray();
            nodes.Clear();
            foreach (var node in copies)
            {
                //this is for BEGIN_LINE etc
                if (node.IsIndicator && TryHitHode(node, indicators))
                {
                    //hit = true;
                    //no advance
                    quit |= (node.Indicator == END_INDICATOR());

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
                        if (groups != null && function != null)
                        {
                            //if fail, quit matching 
                            //to support look around
                            var ret = function(node, input, i, groups, backs, direction);
                            if (ret == false)
                                return false;
                        }

                        hit = true;
                        node.FetchNodes(nodes, false, direction);
                        last = node;
                    }
                }
            }
            if (hit)
            {
                i += direction;
                //if (quit &&(i < start || i > end))
                //    return true;
                indicators.UpdateIndicators(input, i, first, tail, direction);
            }
        }
        if (backs.Count > 0)
        {
            foreach(var back in backs)
            {
                back?.Parent?.RestoreNode(back);
            }
        }

        if (strict)
        {
            return ((direction >= 0 ? (i == end + 1) : (i == start - 1))
                && GraphUtils.HasPassThrough(graph, nodes, direction));
        }
        else
        {
            return ((direction >= 0 ? (i <= end + 1) : (i >= start - 1))
                && (nodes.Count == 0 || GraphUtils.HasPassThrough(graph, nodes, direction)));
        }

        int END_INDICATOR() =>
                       ((options & Options.ONE_LINE) == Options.ONE_LINE)
                        ? RegExTextReader.END_LINE : RegExTextReader.END_TEXT;
    }
    protected static Match BuildMatch(Regex source, string input, string name, int sp, int ep, ListLookups<int, List<int>>? groups, int direction, DualDictionary<string, int> namedGroups)
    {
        //direction
        (sp, ep) = (Math.Min(sp, ep), Math.Max(sp, ep));

        var match = new Match(source, input, true, name, sp, ep, input[sp..ep]);

        if (groups != null)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var _name = namedGroups.TryGetValue(i, out var n) ? n : i.ToString();
                var group = new Group(true,
                    Name: _name);
                foreach (var capture in groups[i])
                {
                    if (capture.Count > 0)
                        group.Captures.Add(new Capture(
                            $"{_name}-{group.Count}",
                            capture.Min(),
                            capture.Max() + 1,
                            //NOTICE: we should use distinct 
                            //because it's possible to get
                            //two same index while emitting
                            new string(capture
                                .Where(p => p != -1).Distinct() //!!!
                                .Select(p => input[p]).ToArray())));
                }
                if (group.Count > 0)
                    match.Groups.Add(group);
            }
        }
        return match;
    }

}
