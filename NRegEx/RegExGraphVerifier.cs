/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

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

    public static bool HasPassage(this Node from, Node to)
    {
        //TODO:
        return false;
    }
    public static bool HasPassage(this List<Node> nodesMain, List<Node> nodesLocal)
    {
        if(nodesMain is not null && nodesLocal is not null 
            && nodesMain.Count>0 && nodesLocal.Count>0
            && nodesMain.Count>nodesLocal.Count){
            var nid1 = nodesMain.Min(n => n.Id);
            var nid2 = nodesLocal.Min(n => n.Id);
            for(int i = 0; i < nodesMain.Count; i++)
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
            new RegExGraphBuilder().Build(model, 0, false),
            (options & Options.DOT_NL) == Options.DOT_NL);
    public static bool IsCatastrophicBacktrackingPossible(Graph graph, bool withNewLine = true)
    {
        GraphUtils.ExportAsDot(graph);
        var step = 0;
        var nodeChars = new ConcurrentDictionary<Node, HashSet<int>>();
        var paths = new ConcurrentBag<Path>(graph.Head.Outputs.Select(o => new Path(graph.Head, o)));
        var heads = new ConcurrentDictionary<Node, HashSet<Edge>>();
        var circles = new ConcurrentBag<Path>();
        var count = graph.Nodes.Count;
        CollectNodeChars(graph.Nodes, nodeChars, withNewLine);

        //foreach (var node in graph.Nodes)
        Parallel.ForEach(graph.Nodes, node =>
        {
            heads.GetOrAdd(node, graph.Edges.Where(e => e.Head == node).ToHashSet());
        });

        while (step++ < count && !paths.IsEmpty)
        {
            paths = new(paths.Where(path => path.Length >= step));
            paths.AsParallel().ForAll(path =>
            {
                var current = path.End;
                if (current != null)
                {
                    foreach (var outEdge in heads[current])// graph.Edges.Where(e => e.Head == current)
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
                    for (int j = i + 1; j < circle_paths.Length; j++)
                    {
                        var cli = circle_paths[i];
                        var clj = circle_paths[j];
                        var nli = cli.NodesList;
                        var nlj = clj.NodesList;
                        if (nli.IsSubPath(nlj) && nlj.HasPassage(nli))
                        {
                            //看ID最小节点是否互通
                            return true;
                        }
                        else if (nlj.IsSubPath(nli) && nli.HasPassage(nlj))
                        {
                            //看ID最小节点是否互通
                            return true;
                        }
                        else if (cli.HasPathTo(clj) || clj.HasPathTo(cli))
                        {
                            circle_pairs.Add((cli, clj));
                        }

                    }
                }
                if (circle_pairs.Count > 0)
                {
                    foreach (var (pi, pj) in circle_pairs)
                    //Parallel.ForEach(pairs,(n,s) =>
                    {
                        var ri = pi.NodeSet.Where(i => !i.IsLink).ToArray();
                        var rj = pj.NodeSet.Where(j => !j.IsLink).ToArray();
                        foreach (var inode in ri)
                        {
                            foreach (var jnode in rj)
                            {
                                if (nodeChars[inode].Overlaps(nodeChars[jnode]))
                                {
                                    return true;
                                }
                            }
                        }
                    }//);
                }
            }
        }
        return false;
    }
    public static ConcurrentDictionary<Node, HashSet<int>> CollectNodeChars(HashSet<Node> nodes, ConcurrentDictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
    {
        Parallel.ForEach(nodes, node =>
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
//      (\d+)+ 
//b.指数重叠析取（EOD）模式：β = (…(β1 | β2 |…| βk)…){ mβ,nβ} 其中nβ > 1，且满足以下两个条件之一：
// 1. 头部有交集
//      (ab|ac|ad){2,}
// 2. 一个的头部和另一个的尾部有交集
//      (ab|bc|cd){2,}
//c. 指数重叠相邻（EOA）模式：β=(…(β1β2)…){mβ,nβ}，其中nβ> 1,满足以下两个条件之一
// 1 头部和尾部有交集
//      (ab(ab)(bc)cd){2,}
// 2 尾部和头部有交集
//      (ab(ba)(ac)cd){2,}
//d. 多项式重叠相邻（POA）模式：：β=(…(β1β2)…){mβ,nβ}，其中nβ <= 1，且满足条件β1.followlast ∩ β2.first ≠ \not= ​= ∅.。
// 尾部和头部有交集
//      (ab(ba)(ac)cd){0,1}
//e. 从大量词开始（SLQ）模式：有四种可能的触发条件，其中 n β > n m i n n_β>n_{min} nβ​>nmin​。
//