﻿using System.Text;

namespace NRegEx;
public static class RegExGraphBuilder
{
    public static StringBuilder ExportAsDot(Graph graph, StringBuilder? builder = null)
    {
        builder ??= new StringBuilder();
        builder.AppendLine("digraph g {");
        foreach (var node in graph.Nodes.OrderBy(n=>n.Id))
        {
            var label = (!string.IsNullOrEmpty(node.Name) ? $"[label=\"{node.Id}({node.Name})\"]" : "");
            builder.AppendLine($"\t{node.Id} {label};");
        }
        foreach (var edge in graph.Edges.OrderBy(e=>e.Head.Id).OrderBy(e=>e.Tail.Id))
        {
            builder.AppendLine($"\t{edge.Head.Id}->{edge.Tail.Id};");
        }
        builder.AppendLine("}");
        return builder;
    }

    public static HashSet<Node> GetFollowings(Graph graph, Node current, HashSet<Node> visited)
        => graph.Edges.Where(e => e.Head == current && visited.Add(e.Tail)).Select(e => e.Tail).ToHashSet();
    public static Graph Recompose(Graph graph, int id = 0)
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
        var nodes = new HashSet<Node>();
        foreach (var line in list)
        {
            foreach (var node in line)
            {
                node.SetId(id++);
                nodes.Add(node);
            }
        }
        return CompactDoubles(graph);
    }
    public static Graph CompactDoubles(Graph graph, int minlength =2)
    {
        var pairs = new List<(List<Node> nodes, List<Edge> edges)>();
        var bridges = new HashSet<Node>();
        foreach (var node in graph.Nodes)
        {
            if (bridges.Contains(node))
                continue;
            if (node.IsBridge)
            {
                var dots = new List<Node>();
                var stream = new List<Edge>();
                var next = node;
                var last = node;
                var edge = graph.Edges.Single(e => e.Tail == next);
                do
                {
                    dots.Add(next);
                    stream.Add(edge);
                    edge = graph.Edges.Single(e => e.Head == next);
                    last = next;
                    next = edge.Tail;
                } while (next != null && next.IsBridge);
                if (next != null && last != null && last != next)
                {
                    edge = graph.Edges.FirstOrDefault(
                        e => e.Head == last && e.Tail == next);
                    if (edge != null)
                        stream.Add(edge);
                }
                bridges.UnionWith(dots);
                pairs.Add((dots, stream));
            }
        }
        foreach (var (nodes, edges) in pairs)
        {
            if (nodes.Count >= minlength)
            {
                var first = edges[0];
                var last = edges[^1];
                graph.Edges.Add(new (first.Head, last.Tail));
                graph.RemoveNodes(nodes.ToHashSet());
            }
        }
        return graph;
    }
    public static Graph FixGraph(Graph graph)
    {
        if (graph is not null)
        {
            foreach (var e in graph.Edges)
            {
                if (!e.Head.Outputs.Contains(e.Tail))
                {
                    e.Head.Outputs.Add(e.Tail);
                }
                if (e.Tail.Inputs.Contains(e.Head))
                {
                    e.Tail.Outputs.Add(e.Head);
                }
            }
        }
        return graph;
    }

    public static bool HasPassThrough(Graph graph)
        => HasPassThrough(graph.Tail,graph.Head);
    public static bool HasPassThrough(Graph graph, Node[] nodes)
        => HasPassThrough(graph.Tail, nodes);
    public static bool HasPassThrough(Node tail,params Node[] nodes)
        => HasPassThrough(tail,nodes.ToHashSet());
    /// <summary>
    /// Check if there is a way to pass through the whole graph
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool HasPassThrough(Node tail, HashSet<Node> nodes)
    {
        var visited = new HashSet<Node>();
        do
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            foreach(var node in copies)
            {
                if (visited.Add(node))
                {
                    if (node == tail)
                        return true;
                    else if (node.IsLink)
                        nodes.UnionWith(node.Outputs);
                }
            }
        } while (nodes.Count > 0);

        return false;
    }

    public static Graph Build(RegExNode node,int id = 0,bool caseInsensitive = false) 
        => Recompose(BuildInternal(node, caseInsensitive),id);
    private static Graph BuildInternal(RegExNode node, bool caseInsensitive = false)
    {
        var graph = new Graph(node.Name) { SourceNode = node };
        switch (node.Type)
        {
            case TokenTypes.EOF:
                {
                    graph.ComposeLiteral(new Node() { Parent = graph});
                }
                break;
            case TokenTypes.OnePlus:
                {
                    if (node.Children.Count >= 1)
                        graph.OnePlus(BuildInternal(node.Children[0]));
                }
                break;
            case TokenTypes.ZeroPlus:
                {
                    if (node.Children.Count >= 1)
                        graph.ZeroPlus(BuildInternal(node.Children[0]));
                }
                break;
            case TokenTypes.ZeroOne:
                {
                    if (node.Children.Count >= 1)
                        graph.ZeroOne(BuildInternal(node.Children[0]));
                }
                break;
            case TokenTypes.Literal:
                {
                    graph.ComposeLiteral(node.Value.EnumerateRunes().Select(c => 
                        caseInsensitive 
                        ? new Node(Characters.ToUpperCase(c.Value),
                                   Characters.ToLowerCase(c.Value)) { Parent = graph } 
                        : new Node(c.Value) { Parent = graph }));
                }
                break;
            case TokenTypes.RuneClass:
                {
                    graph.ComposeLiteral(new Node(node.Inverted, 
                        node.Runes ?? Array.Empty<int>()) { Parent = graph });
                }
                break;
            case TokenTypes.AnyCharIncludingNewLine:
                {
                    //inverted empty means everything
                    graph.ComposeLiteral(new Node(true, 
                        Array.Empty<int>()) { Parent = graph });
                }
                break;
            case TokenTypes.AnyCharExcludingNewLine:
                {
                    //\n excluded
                    graph.UnionWith(new Node(true,
                        Node.NewLineChar) { Parent = graph });
                }
                break;
            case TokenTypes.Sequence:
                {
                    if (node.Children.Count > 0)
                        graph.Concate(node.Children.Select(c => BuildInternal(c)));
                }
                break;

            case TokenTypes.Union:
                if(node.Children.Count > 0)
                    graph.UnionWith(node.Children.Select(c => BuildInternal(c)));
                break;
            case TokenTypes.Repeats:
                if (node.Children.Count > 0)
                    graph.ComposeRepeats(BuildInternal(node.Children[0]),
                        node.Min.GetValueOrDefault(),
                        node.Max.GetValueOrDefault());
                break;
            case TokenTypes.BeginLine:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.BEGIN_LINE) { Parent = graph });
                }
                break;
            case TokenTypes.EndLine:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.END_LINE) { Parent = graph });
                }
                break;
            case TokenTypes.BeginText:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.BEGIN_TEXT) { Parent = graph });
                }
                break;
            case TokenTypes.EndText:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.END_TEXT) { Parent = graph });
                }
                break;
            case TokenTypes.WordBoundary:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.WORD_BOUNDARY) { Parent = graph });
                }
                break;
            case TokenTypes.NotWordBoundary:
                {
                    graph.UnionWith(new Node(true,RegExTextReader.NOT_WORD_BOUNDARY) { Parent = graph });
                }
                break;
        }
        return graph.TryComplete();
    }
}
