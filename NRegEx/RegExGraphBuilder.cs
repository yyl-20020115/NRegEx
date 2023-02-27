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
            case RegExTokenType.EOF:
                {
                    graph.ComposeLiteral(new Node());
                }
                break;
            case RegExTokenType.OnePlus:
                {
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.OnePlus) + nameof(node));
                    graph.OnePlus(Build(node.Children[0]));
                }
                break;
            case RegExTokenType.ZeroPlus:
                {
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.ZeroPlus) + nameof(node));
                    graph.ZeroPlus(Build(node.Children[0]));
                }
                break;
            case RegExTokenType.ZeroOne:
                {
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.ZeroOne) + nameof(node));
                    graph.ZeroOne(Build(node.Children[0]));
                }
                break;
            case RegExTokenType.Literal:
                {
                    graph.ComposeLiteral(node.Value.Select(c => new Node(c)));
                }
                break;
            case RegExTokenType.CharClass:
                {
                    graph.ComposeLiteral(new Node().UnionWith(node.Runes ?? Array.Empty<int>()));
                }
                break;
            case RegExTokenType.AnyCharIncludingNewLine:
                {
                    graph.ComposeLiteral(new Node().UnionWith(Node.AllChars));
                }
                break;
            case RegExTokenType.AnyCharExcludingNewLine:
                {
                    graph.UnionWith(new Node(true, Node.ReturnChar), new Node(true, Node.NewLineChar));
                }
                break;
            case RegExTokenType.Sequence:
                {
                    //if (node.Children.Count == 0) throw new PatternSyntaxException(
                    //    nameof(RegExTokenType.Sequence) + nameof(node));
                    if (node.Children.Count > 0)
                    {
                        graph.Concate(node.Children.Select(c => Build(c)));
                    }
                }
                break;

            case RegExTokenType.Union:
                {
                    //if (node.Children.Count == 0) throw new PatternSyntaxException(
                    //    nameof(RegExTokenType.Union) + nameof(node));
                    if(node.Children.Count > 0)
                    {
                        graph.UnionWith(node.Children.Select(c => Build(c)));
                    }
                }
                break;
            case RegExTokenType.Repeats:
                {
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Union) + nameof(node));
                    graph.ComposeRepeats(Build(node.Children[0]),
                        node.Min.GetValueOrDefault(),
                        node.Max.GetValueOrDefault());
                }
                break;
            case RegExTokenType.BeginLine:
                {
                    graph.Head.UnionWith(Node.NewLineChar);
                    graph.Tail.UnionWith(Node.AllChars);
                }
                break;
            case RegExTokenType.EndLine:
                {
                    graph.Head.UnionWith(Node.AllChars);
                    graph.Tail.UnionWith(Node.NewLineChar);
                }
                break;
            case RegExTokenType.BeginText:
                {
                    graph.Head.UnionWith(Node.EOFChar);
                    graph.Tail.UnionWith(Node.AllChars);
                }
                break;
            case RegExTokenType.EndText:
                {
                    graph.Head.UnionWith(Node.AllChars);
                    graph.Tail.UnionWith(Node.EOFChar);
                }
                break;
            case RegExTokenType.WordBoundary:
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
            case RegExTokenType.NotWordBoundary:
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
        if (graph.Edges.Count == 0)
        {
            graph.Edges.Add(new (graph.Head, graph.Tail));
        }
        return RecomposeIds(graph);
    }
}
