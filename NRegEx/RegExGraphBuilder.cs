using System.Text;

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
#if true
        var segments = new List<List<Node>>();
        do
        {
            var line = new List<Node>();
            foreach (var node in nodes)
            {
                if (node.IsBridge)
                {
                    line.Add(node);
                    var next = node.Outputs.Single();
                    while (next.IsBridge)
                    {
                        line.Add(next);
                        next = next.Outputs.Single();
                    }
                    break;
                }
            }
            if(line.Count > 0)
            {
                segments.Add(line);
            }
            nodes.RemoveWhere(n => line.Contains(n));
        } while (nodes.Any(n => n.IsBridge));
        
        foreach(var segment in segments)
        {
            if (segment.Count >= 2)
            {
                var head = segment[0];
                var tail = segment[^1];

                var before = head.Inputs.Single();
                var after = tail.Outputs.Single();
                before.Outputs.Clear();
                after.Inputs.Clear();

                segment.ForEach(s => s.Inputs.Clear());
                segment.ForEach(s => s.Outputs.Clear());

                var c = graph.Nodes.RemoveWhere(n => segment.Contains(n));
                if (c > 0)
                {
                    c = graph.Edges.RemoveWhere(e => segment.Contains(e.Tail));
                    c = graph.Edges.RemoveWhere(e => segment.Contains(e.Head));
                }
                graph.Edges.Add(new Edge(before, after));
            }
        }

        graph.Nodes.RemoveWhere(n => !graph.Edges.Any(e => e.Head == n || e.Tail == n));
#endif


        return graph;// FixGraph(graph);
    }
    public static Graph FixGraph(Graph g)
    {
        if (g is not null)
        {
            foreach (var e in g.Edges)
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
        return g;
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
