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
        => IsCatastrophicBacktrackingPossible(regex.Graph,(regex.Options& Options.DOT_NL)== Options.DOT_NL);
    public static bool IsCatastrophicBacktrackingPossible(Graph graph, bool withNewLine = true)
    {
        var nodeChars = new Dictionary<Node, HashSet<int>>();
        var circles = GetCircles(graph);

        var affects = new HashSet<Node>();
        foreach(var path in circles)
        {
            affects.UnionWith(path.InternalNodeSet.Where(n => !n.IsLink));
        }
        
        CollectNodeChars(affects, nodeChars, withNewLine);

        var pairs = new List<(Path pi, Path pj)>();

        var paths = circles.ToArray();
        for(int i = 0;i< paths.Length; i++)
        {
            for(int j = i+1;j< paths[i].Length; j++)
            {
                var cli = paths[i];
                var clj = paths[j];
                if (cli.HasPathTo(clj) || clj.HasPathTo(cli)){
                    pairs.Add((cli, clj));
                }
            }
        }
        foreach(var (pi, pj) in pairs)
        {
            var ri = pi.InternalNodeSet.Where(i => !i.IsLink).ToArray();
            var rj = pj.InternalNodeSet.Where(j => !j.IsLink).ToList();
            foreach (var inode in ri)
            {
                foreach(var jnode in rj)
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
                    if (!path.HasVisited(tail))
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
    public static Dictionary<Node,HashSet<int>> CollectNodeChars(HashSet<Node> nodes, Dictionary<Node, HashSet<int>> nodeChars, bool withNewLine)
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