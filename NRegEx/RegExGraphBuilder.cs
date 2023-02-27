using System.Text;

namespace NRegEx;
public static class RegExGraphBuilder
{
    public static StringBuilder ExportAsDot(Graph graph, StringBuilder? builder = null)
    {
        builder ??= new StringBuilder();
        builder.AppendLine("digraph g {");
        foreach (var node in graph.Nodes)
        {
            var label = (!string.IsNullOrEmpty(node.Name) ? $"[label=\"{node.Id}({node.Name})\"]" : "");
            builder.AppendLine($"\t{node.Id} {label};");
        }
        foreach (var edge in graph.Edges)
        {
            builder.AppendLine($"\t{edge.Head.Id}->{edge.Tail.Id};");
        }
        builder.AppendLine("}");
        return builder;
    }

    public static HashSet<Node> GetFollowings(Graph graph, Node current, HashSet<Node> visited)
        => graph.Edges.Where(e => e.Head == current && visited.Add(e.Tail)).Select(e => e.Tail).ToHashSet();
    public static Graph RecomposeIds(Graph graph, int id = 0)
    {
        var visited = new HashSet<Node>();
        var followings = GetFollowings(graph, graph.Head, visited);
        var list = new List<HashSet<Node>>
        {
            new() { graph.Head },
            followings
        };

        var collectings = new HashSet<Node>();
        do
        {
            foreach (var node in followings)
            {
                collectings.UnionWith(GetFollowings(graph, node, visited));
            }
            followings = collectings.ToHashSet();
            if (collectings.Count > 0)
            {
                list.Add(collectings);
                collectings = new();
            }
        } while (followings.Count > 0);
        var hits = new HashSet<Node>();
        foreach (var line in list)
        {
            foreach (var node in line)
            {
                if (hits.Add(node))
                {
                    node.SetId(id++);
                }
            }
        }
        return graph;
    }
    /// <summary>
    /// Check if there is a way to pass through the whole graph
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool HasPassThrough(Graph graph)
    {
        var nodes = new HashSet<Node> { graph.Head };
        var visited = nodes.ToHashSet();
        do
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            foreach(var node in copies)
            {
                if (visited.Add(node))
                {
                    if (node == graph.Tail)
                        return true;
                    else if (node.IsVirtual)
                        nodes.UnionWith(node.Outputs);
                }
            }
        } while (nodes.Count > 0);

        return false;
    }

    public static Graph Build(RegExNode node)
    {
        var graph = new Graph(node.Name);
        switch (node.Type)
        {
            case TokenTypes.EOF:
                {
                    graph.ComposeLiteral(new Node());
                }
                break;
            case TokenTypes.OnePlus:
                {
                    if (node.Children.Count >= 1)
                        graph.OnePlus(Build(node.Children[0]));
                }
                break;
            case TokenTypes.ZeroPlus:
                {
                    if (node.Children.Count >= 1)
                        graph.ZeroPlus(Build(node.Children[0]));
                }
                break;
            case TokenTypes.ZeroOne:
                {
                    if (node.Children.Count >= 1)
                        graph.ZeroOne(Build(node.Children[0]));
                }
                break;
            case TokenTypes.Literal:
                {
                    graph.ComposeLiteral(node.Value.Select(c => new Node(c)));
                }
                break;
            case TokenTypes.CharClass:
                {
                    graph.ComposeLiteral(new Node(node.Runes ?? Array.Empty<int>()));
                }
                break;
            case TokenTypes.AnyCharIncludingNewLine:
                {
                    graph.ComposeLiteral(new Node(Node.AllChars.ToArray()));
                }
                break;
            case TokenTypes.AnyCharExcludingNewLine:
                {
                    graph.UnionWith(new Node(true, Node.ReturnChar, Node.NewLineChar));
                }
                break;
            case TokenTypes.Sequence:
                {
                    if (node.Children.Count > 0)
                        graph.Concate(node.Children.Select(c => Build(c)));
                }
                break;

            case TokenTypes.Union:
                if(node.Children.Count > 0)
                    graph.UnionWith(node.Children.Select(c => Build(c)));
                break;
            case TokenTypes.Repeats:
                if (node.Children.Count > 0)
                    graph.ComposeRepeats(Build(node.Children[0]),
                        node.Min.GetValueOrDefault(),
                        node.Max.GetValueOrDefault());
                break;
            case TokenTypes.BeginLine:
                {
                    graph.Head.UnionWith(Node.NewLineChar);
                    graph.Tail.UnionWith(Node.AllChars);
                }
                break;
            case TokenTypes.EndLine:
                {
                    graph.Head.UnionWith(Node.AllChars);
                    graph.Tail.UnionWith(Node.NewLineChar);
                }
                break;
            case TokenTypes.BeginText:
                {
                    graph.Head.UnionWith(Node.EOFChar);
                    graph.Tail.UnionWith(Node.AllChars);
                }
                break;
            case TokenTypes.EndText:
                {
                    graph.Head.UnionWith(Node.AllChars);
                    graph.Tail.UnionWith(Node.EOFChar);
                }
                break;
            case TokenTypes.WordBoundary:
                {
                    var g1 = new Graph();
                    var g2 = new Graph();
                    g1.Head.UnionWith(Node.WordChars);
                    g1.Tail.UnionWith(Node.NonWordChars);

                    g2.Head.UnionWith(Node.NonWordChars);
                    g2.Tail.UnionWith(Node.WordChars);

                    graph.UnionWith(g1, g2);
                }
                break;
            case TokenTypes.NotWordBoundary:
                {
                    var g1 = new Graph();
                    var g2 = new Graph();
                    g1.Head.UnionWith(Node.WordChars);
                    g1.Tail.UnionWith(Node.WordChars);

                    g2.Head.UnionWith(Node.NonWordChars);
                    g2.Tail.UnionWith(Node.NonWordChars);

                    graph.UnionWith(g1, g2);
                }
                break;
        }
        return RecomposeIds(graph.TryComplete());
    }
}
