﻿/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace NRegEx;
public static class RegExGraphVerifier
{
    public static List<Node> LeftShift(this List<Node> nodes)
    {
        if (nodes.Count > 0)
        {
            var first = nodes[0];
            nodes.Add(first);
            nodes.RemoveAt(0);
        }
        return nodes;
    }

    public static bool IsSubPath(this List<Node> path1, List<Node> path2)
    {
        if (path1 == null || path2 == null) return false;
        if (path1.Count < path2.Count) return false;
        for (int i = 0; i < path1.Count; i++)
        {
            var eq = true;
            for (int j = 0; j < path2.Count; j++)
            {
                if (path2[j].Id != path1[j].Id)
                {
                    eq = false;
                    break;
                }
            }
            if (eq) return true;
            path1.LeftShift();
        }

        return false;
    }

    public static bool HasPassage(this Node node1, Node node2)
    {
        //TODO:
        return false;
    }
    public static bool HasSameSourceWith(this Node node1, Node node2)
    {
        if (node1 == null || node2 == null) return true;

        HashSet<Node> n1set = new();
        HashSet<Node> n2set = new();
        while (true)
        {

        }


        return false;
    }
    public static bool HasPassage(this List<Node> nodesMain, List<Node> nodesLocal)
    {
        if (nodesMain is not null && nodesLocal is not null
            && nodesMain.Count > 0 && nodesLocal.Count > 0
            && nodesMain.Count > nodesLocal.Count)
        {
            var nid1 = nodesMain.Min(n => n.Id);
            var nid2 = nodesLocal.Min(n => n.Id);
            for (int i = 0; i < nodesMain.Count; i++)
                if (nodesMain[0].Id != nid1) nodesMain.LeftShift();
            for (int i = 0; i < nodesLocal.Count; i++)
                if (nodesLocal[0].Id != nid2 || nodesLocal[0].IsLink) nodesLocal.LeftShift();

            return nodesMain[0].HasPassage(nodesLocal[0]);
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
    public static bool IsCatastrophicBacktrackingPossible(Regex regex)
        => IsCatastrophicBacktrackingPossible(
            regex.Model, regex.Options);
    public static bool IsCatastrophicBacktrackingPossible(string regex, Options options = Options.PERL_X)
        => IsCatastrophicBacktrackingPossible(
            new RegExDomParser(regex, regex, options).Parse(), options);
    public static bool IsCatastrophicBacktrackingPossible(RegExNode model, Options options = Options.PERL_X)
        => IsCatastrophicBacktrackingPossible(
            new RegExGraphBuilder() { UseMinMaxEdge = true }.Build(model, 0, false),
            (options & Options.DOT_NL) == Options.DOT_NL);
    /*
     *  1.	嵌套量词或者巨大量词（指数）：里外两圈，且从外圈到里圈存在通路
     *  2.	多头交叠（指数）：平行多圈（开头相同或者有交集）
     *  3.	（多项式：0~1）/（指数：1~n）直接或者间接邻接有交集或者有通路（平行选项中一个的结尾是另一个的开始）
     */

    public static bool IsCatastrophicBacktrackingPossible(Graph graph, bool withNewLine = true)
    {
        GraphUtils.ExportAsDot(graph);
        var step = 0;
        var chars = new ConcurrentDictionary<Node, HashSet<int>>();
        var paths = new ConcurrentBag<Path>(graph.Head.Outputs.Select(o => new Path(graph.Head, o)));
        var heads = new ConcurrentDictionary<Node, HashSet<Edge>>();
        var circles = new ConcurrentBag<Path>();
        var count = graph.Nodes.Count;
        CollectNodeChars(graph.Nodes, chars, withNewLine);

        Parallel.ForEach(graph.Nodes, node =>
        {
            heads.GetOrAdd(node, graph.Edges.Where(e => e.Head == node).ToHashSet());
        });

        while (step++ < count && !paths.IsEmpty)
        {
            paths = new(paths.Where(path => path.Length >= step));
            Parallel.ForEach(paths, (path, ls) =>
            {
                var current = path.End;
                if (current != null)
                {
                    foreach (var outEdge in heads[current])
                    {
                        var tail = outEdge.Tail;
                        var target = path.Find(tail);
                        if (target is null)
                            paths.Add(path.Copy(tail));
                        else
                            circles.Add(path.CopyAndCut(target, true));
                    }
                }
            });

            if (circles.Count >= 2)
            {
                circles = new(circles.Distinct(new PathEqualityComparer()));
                if (circles.Count < 2) continue;

                var circle_pairs = new List<(Path pi, Path pj)>();
                var circle_paths = circles.ToArray();
                for (int i = 0; i < circle_paths.Length; i++)
                {
                    var i_circle = circle_paths[i];
                    var i_nodes = i_circle.ComposeNodesList();
                    if (i_nodes.Count == 0) continue;
                    var first_i_node = i_nodes.FirstOrDefault(n => !n.IsLink);

                    for (int j = i + 1; j < circle_paths.Length; j++)
                    {
                        var j_circle = circle_paths[j];
                        var j_nodes = j_circle.ComposeNodesList();
                        if (j_nodes.Count == 0) continue;

                        var first_j_node = j_nodes.FirstOrDefault(n => !n.IsLink);

                        if (i_nodes[0].HasSameSourceWith(j_nodes[0])
                            || first_i_node != null && first_j_node != null
                            && (first_i_node.Id == first_j_node.Id
                            || chars[first_i_node].Overlaps(chars[first_j_node])))
                        {
                            //两个环具有完全相同的开始(同源)
                            //或者两个环的开始具有交集
                            return true;
                        }
                        else if (i_nodes.IsSubPath(j_nodes) && i_nodes.HasPassage(j_nodes))
                        {
                            //看ID最小节点是否互通
                            return true;
                        }
                        else if (j_nodes.IsSubPath(i_nodes) && j_nodes.HasPassage(i_nodes))
                        {
                            //看ID最小节点是否互通
                            return true;
                        }
                        else if (i_circle.HasPathTo(j_circle) || j_circle.HasPathTo(i_circle))
                        {
                            circle_pairs.Add((i_circle, j_circle));
                        }
                    }
                }
                if (circle_pairs.Count > 0)
                {
                    foreach (var (i_circle, j_circle) in circle_pairs)
                    {
                        var i_nodes = i_circle.NodeSet.Where(i => !i.IsLink).ToArray();
                        var j_nodes = j_circle.NodeSet.Where(j => !j.IsLink).ToArray();
                        foreach (var i_node in i_nodes)
                            foreach (var j_node in j_nodes)
                                if (chars[i_node].Overlaps(chars[j_node]))
                                    return true;
                    }
                }
            }
        }
        return false;
    }
    public static ConcurrentDictionary<Node, HashSet<int>> CollectNodeChars(HashSet<Node> nodes, ConcurrentDictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
    {
        Parallel.ForEach(nodes, (node, ls) =>
        {
            if (!node.IsLink && node.CharsArray != null)
            {
                var chars = node.CharsArray.Where(c => Unicode.IsValidUTF32(c)).ToHashSet();
                if (node.Inverted)
                {
                    var set = (withNewLine ? Node.AllChars : Node.AllCharsWithoutNewLine).ToHashSet();
                    set.ExceptWith(chars);
                    chars = set;
                }
                nodeChars.GetOrAdd(node, chars);
            }
        });

        return nodeChars;
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
//   且满足条件β1.followlast ∩ β2.first ≠ \not= ​= ∅.。
// 尾部和头部有交集
//      (ab(ba)(ac)cd){0,1}
//e. 从大量词开始（SLQ）模式：有四种可能的触发条件，其中 n β > n m i n n_β>n_{min} nβ​>nmin​。
//

