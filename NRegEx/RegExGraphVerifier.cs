/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections.Concurrent;

namespace NRegEx;
public static class RegExGraphVerifier
{
    public static bool IsCatastrophicBacktrackingPossible(string regex)
        => IsCatastrophicBacktrackingPossible(new Regex(regex));

    /// <summary>
    /// 判断对于NFA（非PNFA）引擎是否可能出现灾难性回溯
    /// 判别标准：
    ///     1. 一个非链接有效输入同时属于两个或者两个以上的环
    ///     2. 一个非链接有效输入和另一个非链接有效输入各自在不同的环中，两者具有通路，且输入具有非空交集。
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool IsCatastrophicBacktrackingPossible(Regex regex)
        => IsCatastrophicBacktrackingPossible(RebuildModel(regex.Model, true), (regex.Options & Options.DOT_NL) == Options.DOT_NL);

    public static RegExNode RebuildModel(RegExNode model, bool cleaning = true, HashSet<RegExNode>? visited = null)
    {
        //1. clean +?
        //2. remove ^ and $
        //3. remove \b \B 
        //4. remove lookarounds
        //5. remove backreference
        //6. remove noncaptives
        visited ??= new();

        if (visited.Add(model))
        {
            var isRemoved = model.IsRemoved;

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
            if (!cleaning) isRemoved = model.IsRemoved;

            var ret = model with { IsRemoved = isRemoved };

            foreach (var child in model.Children.ToArray())
            {
                ret.Children.Add(RebuildModel(child, cleaning, visited));
            }
            return ret;
        }


        return model;
    }
    public static bool IsCatastrophicBacktrackingPossible(RegExNode model, bool withNewLine = true)
        => IsCatastrophicBacktrackingPossible(new RegExGraphBuilder().Build(model, 0,false), withNewLine);

    public static bool IsCatastrophicBacktrackingPossible(Graph graph, bool withNewLine = true)
    {
        /*
         */
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
 
        return GetCircles(graph,withNewLine);
    }

    public static bool GetCircles(Graph graph, bool withNewLine)
    {
        var done = false;
        var nodeChars = new Dictionary<Node, HashSet<int>>();
        var affects = new HashSet<Node>();

        var circles = new ConcurrentBag<Path>();

        var paths = new ConcurrentBag<Path>(graph.Head.Outputs.Select(o => new Path(graph.Head, o)));
        var count = graph.Nodes.Count;
        var step = 0;
        var heads = new ConcurrentDictionary<Node, HashSet<Edge>>();

        //foreach (var node in graph.Nodes)
        Parallel.ForEach(graph.Nodes, node =>
        {
            heads[node] = graph.Edges.Where(e => e.Head == node).ToHashSet();
        });
        while (step++ < count)
        {
            paths = new ConcurrentBag<Path>(paths.Where(path=>path.Length>=step));
            //paths.RemoveAll(path => path.Length < step);
            paths.AsParallel().ForAll(path =>
            //foreach(var path in paths.ToArray())
            {
                var current = path.End;
                if (current != null)
                {
                    foreach (var outEdge in heads[current])// graph.Edges.Where(e => e.Head == current)
                    {
                        if (outEdge.Repeats > 1)
                        {

                        }
                        //else
                        {
                            var tail = outEdge.Tail;
                            if (!path.Contains(tail))
                                paths.Add(path.CopyWith(tail));
                            else
                            {
                                circles.Add(path);
                            }
                        }
                    }
                }
            });

            if (circles.Count >= 2)
            {
                foreach (var circle in circles)
                {
                    affects.UnionWith(circle.InternalNodeSet.Where(n => !n.IsLink));
                }

                CollectNodeChars(affects, nodeChars, withNewLine);

                var pairs = new List<(Path pi, Path pj)>();

                var circle_path = circles.ToArray();
                for (int i = 0; i < circle_path.Length; i++)
                {
                    for (int j = i + 1; j < circle_path.Length; j++)
                    {
                        var cli = circle_path[i];
                        var clj = circle_path[j];
                        if (cli.HasPathTo(clj) || clj.HasPathTo(cli))
                        {
                            pairs.Add((cli, clj));
                        }
                    }
                }
                foreach (var n in pairs)
                //Parallel.ForEach(pairs,(n,s) =>
                {
                    var ri = n.pi.InternalNodeSet.Where(i => !i.IsLink).ToArray();
                    var rj = n.pj.InternalNodeSet.Where(j => !j.IsLink).ToList();
                    foreach (var inode in ri)
                    {
                        foreach (var jnode in rj)
                        {
                            if (nodeChars[inode].Overlaps(nodeChars[jnode]))
                            {
                                done = true;
                                //s.Break();
                                return done;
                            }
                        }
                    }
                }//);
            }
        }
        return done;
    }
    public static Dictionary<Node, HashSet<int>> CollectNodeChars(HashSet<Node> nodes, Dictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
    {
        foreach (var node in nodes)
        {
            if (!node.IsLink && node.CharsArray != null && !nodeChars.ContainsKey(node))
            {
                var chars = new HashSet<int>(node.CharsArray.Where(c => Unicode.IsValidUTF32(c)));
                if (!node.Inverted)
                {
                    nodeChars[node] = chars;
                }
                else
                {
                    var set = new HashSet<int>(withNewLine ? Node.AllChars : Node.AllCharsWithoutNewLine);
                    set.ExceptWith(chars);
                    nodeChars[node] = set;

                }

            }
        }

        return nodeChars;
    }

}