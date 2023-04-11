/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NRegEx;
public enum CBTResultTypes
{
    //未检测到CBT
    Undetected = 0,
    //双环连通CBT
    ConnectedLoops = 1,
    //双环平行CBT
    ParallelLoops = 2,
    //环环相套CBT
    NestedLoops = 3,
    //单环逃逸CBT
    SingleEscapedLoop = 4,
}
/// <summary>
/// CBT测试结果
/// </summary>
/// <param name="Type">CBT类型</param>
/// <param name="Position">CBT出现在正则表达式中的开始位置（包括）</param>
/// <param name="Length">CBT出现在正则表达式中的结束位置（不包括）</param>
/// <param name="Regex">CBT涉及的正则表达式</param>
/// <param name="Attacker">用于攻击的字符串</param>
public record CBTResult(CBTResultTypes Type, string Regex, int Position = -1, int Length = 0, int NodeId = -1, string Attacker = "");

public static class RegExGraphCBTDetector
{
    public class BvIntArrayEqualityComparer : IEqualityComparer<(bool bv,int[] ints)>
    {
        public bool Equals((bool bv, int[] ints) x, (bool bv, int[] ints) y)
            => x.bv == y.bv && Enumerable.SequenceEqual(x.ints, y.ints);

        public int GetHashCode([DisallowNull] (bool bv, int[] ints) o)
        {
            var hash = o.bv ? 1 : 0;
            for (var i = 0; i < o.ints.Length; i++)
            {
                hash ^= o.ints[i];
                hash *= 31;
            }

            return hash;
        }


    }
    private static void CollectNodeChars(HashSet<Node> nodes, ConcurrentDictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
    {
        var parts = new ConcurrentDictionary<(bool bv, int[] ints), HashSet<int>>(new BvIntArrayEqualityComparer());
        Parallel.ForEach(nodes, (node, ls) =>
        {
            if (!node.IsLink && node.CharsArray != null)
            {
                if(!parts.TryGetValue((node.Inverted,node.CharsArray),out var chars))
                {
                    chars = node.CharsArray.Where(c => Unicode.IsValidUTF32(c)).ToHashSet();
                    if (node.Inverted)
                    {
                        var set = (withNewLine ? Node.AllChars : Node.AllCharsWithoutNewLine).ToHashSet();
                        set.ExceptWith(chars);
                        chars = set;
                    }
                    parts.GetOrAdd((node.Inverted, node.CharsArray), chars);
                }
                nodeChars.GetOrAdd(node, chars);
            }
        });
    }

    private static bool HasPathTo(this Node main, Node other, bool dual = true)
    {
        var visited = new HashSet<Node>();
        var nodes = main.Outputs.ToHashSet();
        while (nodes.Count > 0)
        {
            var nodeCopy = nodes.ToArray();
            nodes.Clear();
            foreach (var node in nodes)
            {
                if (node == other)
                    return true;
                else if (visited.Add(node))
                {
                    nodes.UnionWith(node.Outputs);
                }
            }
        }

        return dual && other.HasPathTo(main, false);
    }
    private static bool HasPathTo(this List<Node> mainNodes, List<Node> otherNodes)
    {
        foreach (var main in mainNodes)
            foreach (var other in otherNodes)
                if (HasPathTo(main, other))
                    return true;
        return false;
    }
    private static List<Node> LeftShift(this List<Node> nodes)
    {
        if (nodes.Count > 0)
        {
            var first = nodes[0];
            nodes.Add(first);
            nodes.RemoveAt(0);
        }
        return nodes;
    }

    private static bool HasSubpath(this List<Node> longerPath, List<Node> shorterPath)
    {
        if (longerPath == null || shorterPath == null) return false;
        if (longerPath.Count < shorterPath.Count) return false;

        var copyLongerPath = longerPath.ToList();
        for (int i = 0; i < copyLongerPath.Count; i++)
        {
            var eq = true;
            for (int j = 0; j < shorterPath.Count; j++)
            {
                if (shorterPath[j].Id != copyLongerPath[j].Id)
                {
                    eq = false;
                    break;
                }
            }
            if (eq) return true;
            copyLongerPath.LeftShift();
        }

        return false;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodesLocal"></param>
    /// <param name="chars"></param>
    /// <param name="chs"></param>
    /// <returns></returns>
    private static bool HasRun(this List<Node> nodesLocal, ConcurrentDictionary<Node, HashSet<int>> chars, HashSet<int> chs)
    {
        foreach (var node in nodesLocal)
        {
            if (!node.IsLink
                && chars.TryGetValue(node, out var ch)
                && !ch.Overlaps(chs))
                return false;
        }
        return true;
    }
    private static bool HasBackEscape(this Node node, HashSet<Node> circle_nodes, ConcurrentDictionary<Node, HashSet<int>> chars)
    {
        if (circle_nodes == null || !circle_nodes.Contains(node)) return false;
        if (!chars.TryGetValue(node, out var chs)) return false;
        var nid = node.Id;
        var visited = new HashSet<Node>() { node };
        var nodes = node.Inputs.ToHashSet();
        do
        {
            var nodeCopy = nodes.ToArray();
            nodes.Clear();
            foreach (var n in nodeCopy)
            {
                //测试当前节点是否从当前环逃逸
                if (!visited.Add(n)) continue;
                if (!n.IsLink && chars.TryGetValue(n, out var ch))
                {
                    //不相交，断开，无CBT，false
                    if (!ch.Overlaps(chs))
                    {
                        return false;
                    }
                    else
                    { //环外相交，CBT，true
                        if (!circle_nodes.Contains(n))
                        {
                            return true;
                        }
                    }
                }
                nodes.UnionWith(n.Inputs.Where(i => i.Id <= nid));
            }
        } while (nodes.Count > 0);

        //finally mid == 0 is not necessory
        return false;
    }
    private static bool HasPassage(this List<Node> nodesMain, List<Node> nodesLocal, ConcurrentDictionary<Node, HashSet<int>> chars)
    {
        if (nodesMain is not null && nodesLocal is not null
            && nodesMain.Count > 0 && nodesLocal.Count > 0
            && nodesMain.Count > nodesLocal.Count)
        {
            var head_main = nodesMain[0];
            var first_local = nodesLocal.FirstOrDefault(n => !n.IsLink) ?? nodesLocal[0];

            if (first_local != null && chars.TryGetValue(first_local, out var chs))
            {
                var visited = new HashSet<Node>();
                var nodes = first_local.Inputs.ToHashSet();

                while (nodes.Count > 0)
                {
                    var nodesCopy = nodes.ToArray();
                    nodes.Clear();

                    foreach (var node in nodesCopy)
                    {
                        if (node == head_main)
                        {
                            //需要确认nodesMain整个都与chs有交集，若有断开
                            //则不符合CBT
                            return nodesMain.HasRun(chars, chs);
                        }
                        else if (visited.Add(node)
                            && (node.IsLink || chars.TryGetValue(node, out var nhs)
                            && nhs.Overlaps(chs)))
                        {
                            nodes.UnionWith(node.Inputs);
                        }
                    }
                }
            }
        }
        return false;
    }
    /// <summary>
    /// 判断对于NFA（非PNFA）引擎是否可能出现灾难性回溯
    /// 判别标准：
    ///     1. 一个非链接有效输入同时属于两个或者两个以上的环
    ///     2. 一个非链接有效输入和另一个非链接有效输入各自在不同的环中，两者具有通路（或者部分重叠），且输入具有非空交集。
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static CBTResult DetectCatastrophicBacktracking(Regex regex)
        => DetectCatastrophicBacktracking(
            regex.Model, regex.Options);
    public static CBTResult DetectCatastrophicBacktracking(string regex, Options options = Options.PERL_X)
    {
        Node.Reset();
        return DetectCatastrophicBacktracking(
            new RegExDomParser(regex, regex, options).Parse(), options);
    }
    public static CBTResult DetectCatastrophicBacktracking(RegExNode model, Options options = Options.PERL_X)
    {
        Node.Reset();
        return DetectCatastrophicBacktracking(
            new RegExGraphBuilder() { UseMinMaxEdge = true }.Build(model, 0, false),
            (options & Options.DOT_NL) == Options.DOT_NL);
    }
    /*
     *  1.	嵌套量词或者巨大量词（指数）：里外两圈，且从外圈到里圈存在通路
     *  2.	多头交叠（指数）：平行多圈（开头相同或者有交集）
     *  3.	（多项式：0~1）/（指数：1~n）直接或者间接邻接有交集或者有通路（平行选项中一个的结尾是另一个的开始）
     *      比如说：任意字符一次或多次重复的循环之前的所有字符显然都已经可以包含在其中了，所以都应当能够引发CBT
     */

    public static CBTResult DetectCatastrophicBacktracking(Graph graph, bool withNewLine = true)
    {
        //GraphUtils.ExportAsDot(graph);
        var steps = 0;
        var chars = new ConcurrentDictionary<Node, HashSet<int>>();
        var heads = new ConcurrentDictionary<Node, HashSet<Edge>>();
        var paths = new ConcurrentBag<Path>(graph.Head.Outputs.Select(o => new Path(graph.Head, o)));
        var circles = new ConcurrentBag<Path>();
        var count = graph.Nodes.Count;

        CollectNodeChars(graph.Nodes, chars, withNewLine);
        Parallel.ForEach(graph.Nodes, node =>
        {
            heads.GetOrAdd(node, graph.Edges.Where(e => e.Head == node).ToHashSet());
        });

        while (steps++ < count && !paths.IsEmpty)
        {
            paths = new(paths.Where(path => path.Length >= steps));
            Parallel.ForEach(paths, (path, ls) =>
            {
                var current = path.End;
                if (current != null)
                {
                    foreach (var outEdge in heads[current])
                    {
                        var tail = outEdge.Tail;
                        var target = path.Find(tail);
                        if (target == null)
                            paths.Add(path.CopyWith(tail));
                        else
                            circles.Add(path.CopyUntil(target, true));
                    }
                }
                else
                {
                    ls.Break();
                }
            });

            if (!(circles = new(circles.Distinct(new PathEqualityComparer()))).IsEmpty)
            {
                var circle_pairs = new List<(List<Node> pi, List<Node> pj)>();
                var circle_paths = circles.ToArray();
                for (int i = 0; i < circle_paths.Length; i++)
                {
                    var i_circle = circle_paths[i];
                    var i_nodes = i_circle.ComposeNodesList();
                    if (i_nodes.Count == 0) continue;
                    var first_i_node = i_nodes.FirstOrDefault(n => !n.IsLink);
                    var last_i_node = i_nodes.LastOrDefault(n => !n.IsLink);

                    for (int j = i + 1; j < circle_paths.Length; j++)
                    {
                        var j_circle = circle_paths[j];
                        var j_nodes = j_circle.ComposeNodesList();
                        if (j_nodes.Count == 0) continue;
                        var first_j_node = j_nodes.FirstOrDefault(n => !n.IsLink);
                        var last_j_node = j_nodes.LastOrDefault(n => !n.IsLink);

                        if (i_nodes[0] == j_nodes[0]
                            && (first_i_node == first_j_node
                            || first_i_node == last_j_node
                            || last_i_node == first_j_node
                            || first_i_node != null && first_j_node != null && chars[first_i_node].Overlaps(chars[first_j_node])
                            //|| first_i_node != null && last_j_node != null && chars[first_i_node].Overlaps(chars[last_j_node])
                            //|| last_i_node != null && first_j_node != null && chars[last_i_node].Overlaps(chars[first_j_node])
                            ))
                        {
                            //两个环具有完全相同的开始(同源)
                            //或者两个环的开始/结尾具有交集
                            return new(CBTResultTypes.ParallelLoops, graph.Name, graph.Position, graph.Length, i_nodes[0].Id);
                        }
                        var longer_nodes = (i_nodes.Count >= j_nodes.Count ? i_nodes : j_nodes);
                        var shorter_nodes = (i_nodes.Count < j_nodes.Count ? i_nodes : j_nodes);
                        //大环套小环模式
                        if (longer_nodes.HasSubpath(shorter_nodes))
                        {
                            //如果不能贯通需要查看后面的部分，贯通的话直接认定CBT
                            if (longer_nodes.HasPassage(shorter_nodes, chars))
                                return new(CBTResultTypes.NestedLoops, graph.Name, graph.Position, graph.Length, longer_nodes[0].Id);
                            else
                                continue;
                        }
                        else
                        {
                            var i_circle_nodes = i_circle.ComposeNodesList();
                            var j_circle_nodes = j_circle.ComposeNodesList();
                            if (i_circle_nodes[0] == j_circle_nodes[0]
                             && i_circle_nodes[^1] == j_circle_nodes[^1])
                            {
                                //平行序列。不应被认为具有实现CBT的可能性。 
                                continue;
                            }
                            else if (i_circle_nodes.HasPathTo(j_circle_nodes))
                            {
                                circle_pairs.Add((
                                    i_circle_nodes,
                                    j_circle_nodes));
                            }
                        }
                    }
                }

                if (circle_pairs.Count > 0)
                {
                    foreach (var (i_circle, j_circle) in circle_pairs)
                        foreach (var i_node in i_circle)
                            foreach (var j_node in j_circle)
                                if (!i_node.IsLink && !j_node.IsLink
                                    && chars[i_node].Overlaps(chars[j_node]))
                                    return new(CBTResultTypes.ConnectedLoops, graph.Name, graph.Position, graph.Length, i_node.Id);
                }
            }
        }
        //至此两个环相关的情况已经完全排除，仅需要测试单环的逃逸情况
        if (!circles.IsEmpty)
        {
            foreach (var i_circle in circles)
            {
                var i_nodes = i_circle.ComposeNodesList();
                var first_i_node = i_nodes.FirstOrDefault(n => !n.IsLink);
                if (first_i_node != null && first_i_node.HasBackEscape(i_nodes.ToHashSet(), chars))
                    return new(CBTResultTypes.SingleEscapedLoop, graph.Name, graph.Position, graph.Length,first_i_node.Id);
            }
        }
        return new(CBTResultTypes.Undetected, graph.Name, graph.Position, graph.Length);
    }
}

//a. 嵌套量词（NQ）模式：具有嵌套量词的正则表达式。
//   包围模式，外部完全包含内部
//      (\d+)+ 但是(a\d+)+不是：考虑前缀情况
//b.指数重叠析取（EOD）模式：β = (…(β1 | β2 |…| βk)…){ mβ,nβ} 其中nβ > 1，且满足以下两个条件之一：
//   同步开头模式，开始位置相同，长度不一定相同
//
// 1. 头部有交集
//      (ab|ac|ad){2,}
// 2. 一个的头部和另一个的尾部有交集
//      (ab|bc|cd){2,}
//!!!! c 和 d 可以合并
//c. 指数重叠相邻（EOA）模式：β=(…(β1β2)…){mβ,nβ}，其中nβ> 1,满足以下两个条件之一
//    相邻两个部分有交集
//
// 1 头部和尾部有交集
//      (ab(ab)(bc)cd){2,}
// 2 尾部和头部有交集
//      (ab(ba)(ac)cd){2,}
//d. 多项式重叠相邻（POA）模式：：β=(…(β1β2)…){mβ,nβ}，其中nβ <= 1，
//   且满足条件β1.follow last ∩ β2.first ≠ \not= ​= ∅.。
// 尾部和头部有交集
//      (ab(ba)(ac)cd){0,1}
//e. 从大量词开始（SLQ）模式：有四种可能的触发条件，其中 n β > n m i n n_β>n_{min} nβ​>nmin​。
//

