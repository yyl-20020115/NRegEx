/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using static NRegEx.Path;

namespace NRegEx;
public static class RegExGraphVerifier
{

    public static List<Node> Shuffle(this List<Node> nodes)
    {
        if (nodes.Count > 0)
        {
            var first = nodes[0];
            nodes.RemoveAt(0);
            nodes.Add(first);
        }
        return nodes;
    }
    public static bool IsSubPath(this List<Node> path1, List<Node> path2)
    {
        if (path1 == null || path2 == null) return false;
        for(int i = 0; i < path1.Count; i++)
        {
            var eq = true;
            for(int j = 0; j < path2.Count; j++)
            {
                if (path2[j] != path1[j])
                {
                    eq = false;
                    break;
                }
            }
            if (eq) return true;
            path1.Shuffle();
        }

        return false;
    }
    /// <summary>
    /// 判断对于NFA（非PNFA）引擎是否可能出现灾难性回溯
    /// 判别标准：
    ///     1. 一个非链接有效输入同时属于两个或者两个以上的环
    ///     2. 一个非链接有效输入和另一个非链接有效输入各自在不同的环中，两者具有通路，且输入具有非空交集。
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool IsCatastrophicBacktrackingPossible(Regex regex)
        => IsCatastrophicBacktrackingPossible(
            (regex.Model), (regex.Options & Options.DOT_NL) == Options.DOT_NL);
    public static bool IsCatastrophicBacktrackingPossible(string regex, bool withNewLine = true)
        => IsCatastrophicBacktrackingPossible(
            (new RegExDomParser(regex,regex,Options.PERL).Parse()), withNewLine);

    public static RegExNode MaskModel(RegExNode model, HashSet<RegExNode>? visited = null)
    {
        //1. clean +?
        //2. remove ^ and $
        //3. remove \b \B 
        //4. remove lookarounds
        //5. remove backreference
        //6. remove noncaptives
        if ((visited ??= new()).Add(model))
        {
            var isRemoved = false;

            switch (model.Type)
            {
                case TokenTypes.BeginLine:
                case TokenTypes.EndLine:
                case TokenTypes.BeginText:
                case TokenTypes.EndText:
                case TokenTypes.WordBoundary:
                case TokenTypes.NotWordBoundary:
                    isRemoved = true;
                    break;
                case TokenTypes.Group:
                    switch (model.GroupType)
                    {
                        case GroupType.NotCaptiveGroup:
                        case GroupType.ForwardPositiveGroup:
                        case GroupType.ForwardNegativeGroup:
                        case GroupType.BackwardPositiveGroup:
                        case GroupType.BackwardNegativeGroup:
                        case GroupType.LookAroundConditionGroup:
                        case GroupType.BackReferenceCondition:
                        case GroupType.BackReferenceConditionGroup:
                            isRemoved = true;
                            break;
                    }

                    break;
            }
            if (!isRemoved)
            {
                var children = model.Children.ToArray();

                foreach (var child in children)
                {
                    MaskModel(child, visited);
                }
            }
            else
            {
                model.IsRemoved = true;
            }
        }
        return model;
    }


    public static bool IsCatastrophicBacktrackingPossible(RegExNode model, bool withNewLine = true)
        => IsCatastrophicBacktrackingPossible(
            new RegExGraphBuilder().Build(model, 0,false),
            withNewLine);

    public static bool IsCatastrophicBacktrackingPossible(Graph graph, bool withNewLine = true)
    {
        var step = 0;
        var nodeChars = new Dictionary<Node, HashSet<int>>();
        var circles = new ConcurrentBag<Path>();
        var paths = new ConcurrentBag<Path>(graph.Head.Outputs.Select(o => new Path(graph.Head, o)));
        var count = graph.Nodes.Count;
        var heads = new ConcurrentDictionary<Node, HashSet<Edge>>();
        CollectNodeChars(graph.Nodes, nodeChars, withNewLine);

        //foreach (var node in graph.Nodes)
        Parallel.ForEach(graph.Nodes, node =>
        {
            heads.GetOrAdd(node, graph.Edges.Where(e => e.Head == node).ToHashSet());
        });

        while (step++ < count && !paths.IsEmpty)
        {
            paths = new (paths.Where(path => path.Length >= step));
            paths.AsParallel().ForAll(path =>
            {
                var current = path.End;
                if (current != null)
                {
                    foreach (var outEdge in heads[current])// graph.Edges.Where(e => e.Head == current)
                    {
                        var tail = outEdge.Tail;
                        var index = path.IndexOf(tail);
                        if (index <= 0)
                            paths.Add(path.Copy(tail));
                        else
                            circles.Add(path.Copy().CutFrom(index));
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

                        if (cli.Nodes.IsSubPath(clj.Nodes))
                        {
                            //GraphUtils.ExportAsDot(graph);
                            return true;
                        }
                        else if (clj.Nodes.IsSubPath(cli.Nodes))
                        {
                            return true;
                        }
                        else if (cli.HasPathTo(clj) || clj.HasPathTo(cli))
                        {
                            circle_pairs.Add((cli, clj));
                        }
                        
                    }
                }
                if(circle_pairs.Count > 0)
                {
                    foreach (var n in circle_pairs)
                    //Parallel.ForEach(pairs,(n,s) =>
                    {
                        var ri = n.pi.NodeSet.Where(i => !i.IsLink).ToArray();
                        var rj = n.pj.NodeSet.Where(j => !j.IsLink).ToArray();
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
    public static Dictionary<Node, HashSet<int>> CollectNodeChars(HashSet<Node> nodes, Dictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
    {
        foreach (var node in nodes)
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
                nodeChars.Add(node, chars);
            }
        }

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