using System.Runtime;

namespace NRegEx;
public static class RegExGraphVerifier
{
    /// <summary>
    /// 判断对于NFA（非PNFA）引擎是否可能出现灾难性回溯
    /// 判别标准：
    ///     1. 一个非链接有效输入同时属于两个或者两个以上的环
    ///     2. 一个非链接有效输入和另一个非链接有效输入各自在不同的环中，两者具有通路，且输入具有非空交集。
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool IsCatastrophicBacktrackingPossibe(Regex regex)
        => IsCatastrophicBacktrackingPossible(RebuildModel(regex.Model,true), (regex.Options & Options.DOT_NL) == Options.DOT_NL);

    public static RegExNode RebuildModel(RegExNode model,bool cleaning = true, HashSet<RegExNode>? visited = null)
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

            foreach (var child in model.Children)
            {
                if (visited.Add(child))
                {
                    ret.Children.Add(RebuildModel(child, cleaning, visited));
                }
            }
            return ret;
        }


        return model;
    }
    public static bool IsCatastrophicBacktrackingPossible(RegExNode model, bool withNewLine = true)
        => IsCatastrophicBacktrackingPossible(new RegExGraphBuilder().Build(model,0),withNewLine);

    public static bool IsCatastrophicBacktrackingPossible(Graph graph, bool withNewLine = true)
    {
        //a. 嵌套量词（NQ）模式：具有嵌套量词的正则表达式。例如(\d+)+。
        //b.指数重叠析取（EOD）模式：β = (…(β1 | β2 |…| βk)…){ mβ,nβ} 其中nβ > 1，且满足以下两个条件之一。
        // 1. Bp.first and Bq.first != EMPTY  c                                                       ere 1<=p, q>=k, p!=q
        // 2. Bp.first and Bq.followinglast != EMPTY where 1<=p, q>=k, p!=q
        //c. 指数重叠相邻（EOA）模式：β=(…(β1β2)…){mβ,nβ}，其中nβ> 1,满足以下两个条件之一
        // 1
        // 2
        //d. 多项式重叠相邻（POA）模式：：β=(…(β1β2)…){mβ,nβ}，其中nβ <= 1，且满足条件β1.followlast ∩ β2.first ≠ \not= ​= ∅.。

        //e. 从大量词开始（SLQ）模式：有四种可能的触发条件，其中 n β > n m i n n_β>n_{min} nβ​>nmin​。

        var nodeChars = new Dictionary<Node, HashSet<int>>();
        var circles = GetCircles(graph);

        var affects = new HashSet<Node>();
        foreach (var path in circles)
        {
            affects.UnionWith(path.InternalNodeSet.Where(n => !n.IsLink));
        }

        CollectNodeChars(affects, nodeChars, withNewLine);

        var pairs = new List<(Path pi, Path pj)>();

        var paths = circles.ToArray();
        for (int i = 0; i < paths.Length; i++)
        {
            for (int j = i + 1; j < paths[i].Length; j++)
            {
                var cli = paths[i];
                var clj = paths[j];
                if (cli.HasPathTo(clj) || clj.HasPathTo(cli))
                {
                    pairs.Add((cli, clj));
                }
            }
        }
        foreach (var (pi, pj) in pairs)
        {
            var ri = pi.InternalNodeSet.Where(i => !i.IsLink).ToArray();
            var rj = pj.InternalNodeSet.Where(j => !j.IsLink).ToList();
            foreach (var inode in ri)
            {
                foreach (var jnode in rj)
                {
                    if (nodeChars[inode].Overlaps(nodeChars[jnode]))
                        return true;
                }
            }
        }

        return false;
    }

    public static List<Path> GetCircles(Graph graph)
    {
        var paths = new List<Path>(graph.Head.Outputs.Select(o => new Path(graph.Head, o)));
        var circles = new List<Path>();
        var count = graph.Nodes.Count;
        var step = 0;
        while (step++ < count)
        {
            paths.RemoveAll(path => path.Length < step);
            foreach (var path in paths.ToArray())
            {
                var current = path.End;
                foreach (var outEdge in graph.Edges.Where(e => e.Head == current))
                {
                    var tail = outEdge.Tail;
                    if (!path.Contains(tail))
                        paths.Add(path.Copy().AddNodes(tail));
                    else
                    {
                        circles.Add(path);
                        paths.Remove(path);
                    }
                }
            }
        }
        return circles;
    }
    public static Dictionary<Node, HashSet<int>> CollectNodeChars(HashSet<Node> nodes, Dictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
    {
        foreach (var node in nodes)
        {
            if (!node.IsLink && node.CharsArray != null)
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