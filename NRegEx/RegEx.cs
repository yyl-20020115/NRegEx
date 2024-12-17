/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace NRegEx;

public delegate string CaptureEvaluator(Capture capture);

public partial class Regex
{
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

    public Regex(string pattern, string? name = null, Options options = Options.PERL_X)
    {
        this.Pattern = pattern;
        this.Name = name ?? this.Pattern;
        this.Options = options;
        var Parser = new RegExDomParser(this.Name, this.Pattern, this.Options);
        this.Model = Parser.Parse();
        this.NamedGroups = new(Parser.NamedGroups);
        var Builder = new RegExGraphBuilder();
        this.Graph = Builder.Build(this.Model, 0,
            (this.Options & Options.CASE_INSENSITIVE) == Options.CASE_INSENSITIVE)
            .CleanEdges();

        this.BackRefPoints = Builder.BackRefPoints;
        this.GroupTypes = Builder.GroupTypes;
        this.GroupGraphs = Builder.GroupGraphs;
        this.ConditionsGraphs = Builder.ConditionsGraphs;
    }
    public bool IsMatch(string input, int first = 0, int length = -1, bool reversely = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (first < 0 || first > input.Length) throw new ArgumentOutOfRangeException(nameof(first));
        if (length < 0) length = input.Length - first;
        if (first + length > input.Length) throw new ArgumentOutOfRangeException(nameof(first) + "_" + nameof(length));

        var (Runes, Start, Length) = Utils.ToRunes(input, first);
        Node? last = null;
        int sp = 0, ep = 0;
        return IsMatchInternal(this.Graph, Runes, Start, Length, ref sp, ref ep, ref last, null, reversely ? -1 : 1, this.Options, this.TryWithGroups);
    }
    public bool IsFullyMatch(string input, int start = 0, int length = -1, bool reversely = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (start < 0 || start > input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length - start;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "_" + nameof(length));
        Node? last = null;
        var (Runes, Start, Length) = Utils.ToRunes(input, start);
        var paths = new HashSet<CountablePath>();

        return IsMatchCore(this.Graph, Runes, Start, Length, ref start, ref last, true, paths, null, reversely ? -1 : 1, this.Options, this.TryWithGroups);
    }
    
    protected static bool TryHitHode(Node node, RegExIndicators indicators)
    {
        if (!node.IsLink)
        {
            foreach (var kv in indicators.IndicatorsDict)
                if (Check(indicators.Indicators[kv.Key], node, kv.Value))
                    return true;
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
        var (Runes, Start, Length) = Utils.ToRunes(input, start);
        var ret = IsMatchInternal(this.Graph, Runes, Start, Length, ref sp, ref ep, ref last, groups, direction, this.Options, this.TryWithGroups);
        if (ret)
        {
            var name = this.Name;
            if (last?.Parent?.SourceNode is RegExNode r)
                name = r.PatternName ?? name;
            return CreateMatch(this, input, name, sp, ep, groups, direction, this.NamedGroups);
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

    protected static bool IsMatchInternal(Graph graph, Rune[] input, int first, int length, ref int sp, ref int ep, ref Node? last, ListLookups<int, List<int>>? groups, int direction, Options options, TryWithGroupsFunction function)
    {
        direction = RegexHelpers.FixDirection(direction);
        var tail = first + length;
        var start = direction >= 0 ? first : tail - 1;

        int i = IsMatchFindStart(graph, input, first, tail, direction);
        int o = sp = ep = i;

        while (i >= first && i < tail)
        {
            length -= RegexHelpers.Abs(i - start);
            var paths = new HashSet<CountablePath>();
            if (IsMatchCore(graph, input, i, length, ref o, ref last, false, paths, groups, direction, options, function))
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
    protected static int IsMatchFindStart(Graph graph, Rune[] input, int first, int tail, int direction)
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
                                if (sub.TryHit(input[i].Value) == true)
                                {
                                    return i;
                                }
                            }
                        }
                    }
                    else if (node.TryHit(input[i].Value) == true)
                    {
                        return i;
                    }
                }
            }
        }
        return -1;
    }
    
    protected static bool IsMatchCore(Graph graph, Rune[] input, int first, int length, ref int i, ref Node? last, bool strict, HashSet<CountablePath> paths, ListLookups<int, List<int>>? groups, int direction, Options options, TryWithGroupsFunction? function)
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
        var edges = new HashSet<Edge>();
        //var paths = new HashSet<CountablePath>
        paths.UnionWith(heads.Select(head => new CountablePath(head)));
        var steps = 0;
        while (nodes.Count > 0 && i >= start && i <= end)
        {
            steps++;

            var c = input[i];
            var hits = new HashSet<Node>();
            var quit = false;
            var copies = nodes.ToArray();
            nodes.Clear();

            var removes = new HashSet<Node>();
            foreach (var path in paths.ToArray())
            {
                foreach (var node in copies)
                {
                    if (path.CopyWith(node) is CountablePath cp)
                    {
                        paths.Add(cp);
                        //发现重复多次的边
                        //如果已经发现，则减去1，否则设定
                        //如果减到0，则去掉此节点
                        if (graph.RepeativeEdges.TryGetValue((path.Tail!, node), out var repeat))
                        {
                            if (cp.CountableEdge != repeat)
                            {
                                cp.CountableEdge = repeat;
                                cp.MinRepeats = repeat.MinRepeats;
                                cp.MaxRepeats = repeat.MaxRepeats;
                            }
                            if (cp.CountableEdge != null && !cp.TryPassingOnceAndClear())
                            {
                                removes.Add(node);
                            }
                        }
                    }
                }
            }
            //extend paths
            //paths.UnionWith(paths.SelectMany(path => copies.Select(n => path.CopyWith(n) as CountablePath)));
            if (removes.Count > 0)
            {
                nodes.ExceptWith(removes);
            }
            //all paths are invalid
            if (nodes.Count == 0) return false;
            copies = nodes.ToArray();
            foreach (var node in copies)
            {
                //this is for BEGIN_LINE etc
                if (node.IsIndicator && TryHitHode(node, indicators))
                {
                    //hit = true;
                    //no advance
                    quit |= node.Indicator == END_INDICATOR();

                    node.FetchNodes(nodes, true, direction);
                    last = node;
                }
                else
                {
                    var d = node.TryHit(c.Value);
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
                            var ret = function(node, input, i, groups, backs, edges, direction);
                            if (ret == false)
                                return false;
                        }

                        hits.Add(node);
                        node.FetchNodes(nodes, false, direction);
                        last = node;
                    }
                }
            }

            if (hits.Count > 0)
            {
                //only keep the hit ones
                paths.RemoveWhere(path => !hits.Contains(path.Tail!));

                i += direction;
                //if (quit &&(i < start || i > end))
                //    return true;
                indicators.UpdateIndicators(input, i, first, tail, direction);
            }
        }
        if (backs.Count > 0)
        {
            foreach (var back in backs)
            {
                back?.Parent?.RestoreNode(back);
            }
        }
        if (edges.Count > 0)
        {
            graph.RemoveEdges(edges);
        }
        //if any path is uncompleted ({2,} if hits<2)
        //we treat it as failed to match
        if (paths.Any(p => p.IsUncompleted))
            return false;
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
    protected static Match CreateMatch(Regex source, string input, string name, int sp, int ep, ListLookups<int, List<int>>? groups, int direction, DualDictionary<string, int> namedGroups)
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
