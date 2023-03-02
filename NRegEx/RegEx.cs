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
    protected readonly Dictionary<int, Graph> BackReferencePoints;
    protected readonly Dictionary<int, GroupType> GroupTypes;
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
        this.BackReferencePoints = Builder.BackReferencesPoints;
        this.GroupTypes = Builder.GroupTypes;
    }
    public bool IsMatch(string input, int first = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (first < 0 || first > input.Length) throw new ArgumentOutOfRangeException(nameof(first));
        if (length < 0) length = input.Length - first;
        if (first + length > input.Length) throw new ArgumentOutOfRangeException(nameof(first) + "_" + nameof(length));

        Node? last = null;
        int sp = 0, ep = 0;
        return this.IsMatchInternal(input, first, length, ref sp, ref ep, ref last, null, reversely ? -1 : 1);
    }
    public bool IsFullyMatch(string input, int start = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        Node? last = null;
        return IsMatchInternal(input, start, length, ref start, ref last, true, null, reversely ? -1 : 1);
    }
    protected bool IsMatchInternal(string input, int first, int length, ref int sp, ref int ep, ref Node? last, ListLookups<int, List<int>>? groups, int direction)
    {
        direction = FixDirection(direction);
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
        direction = FixDirection(direction);
        if (first < tail)
        {
            var last = tail - 1;
            direction = Math.Sign(direction);
            var heads = this.Graph.Nodes.Where(
                n => !(direction >= 0 ? n.HasInput : n.HasOutput)).ToArray();
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
                    if (node.IsIndicator)
                    {
                        if (this.TryHitHode(node))
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
    protected int END_INDICATOR =>
                       ((this.Options & Options.ONE_LINE) == Options.ONE_LINE)
                        ? RegExTextReader.END_LINE : RegExTextReader.END_TEXT;
    protected bool IsMatchInternal(string input, int first, int length, ref int i, ref Node? last, bool strict, ListLookups<int, List<int>>? groups, int direction)
    {
        if (length == 0 && GraphUtils.HasPassThrough(this.Graph)) return true;
        direction = Math.Sign(direction);
        var start = first;
        var tail = first + length;
        var end = tail - 1;
        if (direction < 0) (start, end) = (end, start);

        i = start;
        this.UpdateIndicators(input, i, first, tail, direction);
        var heads = this.Graph.Nodes.Where(n => !(direction >= 0 ? n.HasInput : n.HasOutput));
        var nodes = heads.ToHashSet();
        var backs = new HashSet<Node>();
        while (nodes.Count > 0 && i >= start && i <= end)
        {
            var c = input[i];
            var hit = false;
            var copies = nodes.ToArray();
            nodes.Clear();
            var quit = false;
            foreach (var node in copies)
            {
                //this is for BEGIN_LINE etc
                if (node.IsIndicator && this.TryHitHode(node))
                {
                    //hit = true;
                    //no advance
                    quit |= (node.Indicator == END_INDICATOR);

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
                            this.TryWithGroup(node, input, i, groups, backs);

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
                this.UpdateIndicators(input, i, first, tail, direction);
            }
        }
        if (backs.Count > 0)
        {
            this.Graph.UnlinkNodes(backs);
        }

        if (strict)
        {
            return ((direction >= 0 ? (i == end + 1) : (i == start - 1))
                && GraphUtils.HasPassThrough(this.Graph, nodes, direction));
        }
        else
        {
            return ((direction >= 0 ? (i <= end + 1) : (i >= start - 1))
                && (nodes.Count == 0 || GraphUtils.HasPassThrough(this.Graph, nodes, direction)));
        }
    }

    protected void TryWithGroup(Node node, string input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs)
    {
        if (groups != null)
        {
            foreach (var index in node.Groups)
            {
                if (this.GroupTypes.TryGetValue(index, out var type))
                {
                    switch (type)
                    {
                        case GroupType.NotGroup:
                        case GroupType.NotCaptiveGroup:
                            break;
                        case GroupType.NormalGroup:
                        case GroupType.AtomicGroup:
                            EmitPosition(node, i, groups[index]);
                            if (this.BackReferencePoints.TryGetValue(index, out var graph))
                                backs.Add(graph.InsertPointBeforeTail(new(input[i])));
                            break;
                        case GroupType.ForwardNegativeGroup:
                            throw new NotSupportedException(nameof(GroupType.ForwardNegativeGroup));
                        case GroupType.ForwardPositiveGroup:
                            throw new NotSupportedException(nameof(GroupType.ForwardPositiveGroup));
                        case GroupType.BackwardPositiveGroup:
                            throw new NotSupportedException(nameof(GroupType.BackwardPositiveGroup));
                        case GroupType.BackwardNegativeGroup:
                            throw new NotSupportedException(nameof(GroupType.BackwardNegativeGroup));
                    }
                }
            }
        }
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

    protected void UpdateIndicators(string input, int i, int first, int tail, int direction)
    {
        direction = FixDirection(direction);
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
    public Match Match(string input, int start = 0, int length = -1, bool reversely = false)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));

        int direction = FixDirection(reversely ? -1 : 1);
        Node? last = null;
        var groups = new ListLookups<int, List<int>>();
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
    protected Match BuildMatch(string input, string name, int sp, int ep, ListLookups<int, List<int>>? groups, int direction)
    {
        //direction
        (sp, ep) = (Math.Min(sp, ep), Math.Max(sp, ep));

        var match = new Match(this, input, true, name, sp, ep, input[sp..ep]);

        if (groups != null)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var _name = this.NamedGroups.TryGetValue(i, out var n) ? n : i.ToString();
                var group = new Group(true,
                    Name: _name);
                foreach (var capture in groups[i])
                {
                    if (capture.Count > 0)
                        group.Captures.Add(new Capture(
                            $"{_name}-{group.Count}",
                            capture.Min(),
                            capture.Max() + 1,
                            new string(capture
                                .Where(p => p != -1)
                                .Select(p => input[p]).ToArray())));
                }
                if (group.Count > 0)
                    match.Groups.Add(group);
            }
        }
        return match;
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

                length -= Math.Abs((reversely? match.InclusiveStart : match.ExclusiveEnd) - first);
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
    private static int FixDirection(int direction) => direction >= 0 ? 1 : -1;
    
}
