using System.ComponentModel;
using System.Text;

namespace NRegEx;

public record class Node
{
    public const int EOFChar = -1;
    public const int NewLineChar = '\n';
    public const int ReturnChar = '\r';

    public readonly static HashSet<int> AllChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).ToHashSet();
    public readonly static HashSet<int> WordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => char.IsLetter((char)i)).ToHashSet();
    public readonly static HashSet<int> NonWordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => !char.IsLetter((char)i)).ToHashSet();

    public readonly static Dictionary<int, HashSet<int>> KnownInvertedSets = new();

    protected static int Nid = 0;
    protected int id;
    public readonly bool IsVirtual;
    public readonly bool Inverted;
    public string Name { get; set; } = string.Empty;

    public int Id => id;

    public int SetId(int id) => this.id = id;
    public readonly HashSet<int> CharSet = new();
    public readonly HashSet<Node> Inputs = new ();
    public readonly HashSet<Node> Outputs = new ();
    public Node(string name = "")
        :this(EOFChar)
    {
        this.Name = name;
        this.IsVirtual = true;
    }
    public Node(params char[] cs)
        : this(cs.Select(c => (int)c).ToArray()) { }
    public Node(params int[] cs)
        : this(false, cs) { }
    public Node(bool inverted, params char[] cs)
        : this(inverted, cs.Select(c => (int)c).ToArray()) { }

    public Node(bool inverted, params int[] cs)
    {
        this.IsVirtual = false;
        this.Name = "";
        this.id = ++Nid;
        this.Inverted = inverted;

        if (this.Inverted)
        {
            this.CharSet = new (AllChars);
            this.CharSet.ExceptWith(cs);
        }
        else
        {
            this.CharSet = new (cs);
        }
        this.Name = "'" + string.Join(",", cs.Select(c => new Rune(c>=0?c:' ').ToString()).ToArray()) + "'";
    }

    public Node UnionWith(params int[] runes)
        => this.UnionWith(runes as IEnumerable<int>);
    public Node UnionWith(IEnumerable<int> runes)
    {
        this.CharSet.UnionWith(runes);
        return this;
    }
    public bool Hit(int c)
    {
        var cx = this.CharSet.Contains(c);
        return Inverted ? !cx : cx;
    }

    public static string FormatNodes(IEnumerable<Node?> nodes)
    {
        var builder = new StringBuilder();
        var ns = new List<Node?>(nodes);
        for(int i = 0; i < ns.Count; i++)
        {
            var node = ns[i];
            builder.Append(node?.Id);
            if (i < ns.Count - 1)
                builder.Append(',');
        }
        return builder.ToString();
    }
    public override string ToString() 
        => $"[({this.Id},{this.Inverted}):{string.Join(',',this.CharSet)} IN:{FormatNodes(this.Inputs)}  OUT:{FormatNodes(this.Outputs)}]";
}
public record class Edge
{
    public readonly Node Head;
    public readonly Node Tail;
    public Edge(Node Head,Node Tail)
    {
        this.Head = Head;
        this.Tail = Tail;
        this.Head.Outputs.Add(Tail);
        this.Tail.Inputs.Add(Head);
    }

}
public record class Graph
{
    protected static int Gid = 0;

    protected int id = 0;
    public int Id => id;
    public int SetId(int id) => this.id = id;
    public string? Description { get; protected set; }
    public readonly string Name;

    public readonly HashSet<Node> Nodes = new();
    public readonly HashSet<Edge> Edges = new();
    public Node Head;
    public Node Tail;
    public Graph(string name = "",params char[] cs )
    {
        this.Name = name;
        this.id = ++Gid;
        if (cs.Length>0)
        {
            this.Nodes.Add(this.Head = this.Tail = new Node(cs) with { Name = name });
            this.Description = this.Head.ToString();
        }
        else
        {
            this.Nodes.Add(this.Head = new(name));
            this.Nodes.Add(this.Tail = new(name));
        }
    }
    public Graph ComposeLiteral(params Node[] sequence)
        => this.ComposeLiteral(sequence as IEnumerable<Node>);

    public Graph ComposeLiteral(IEnumerable<Node> sequence)
        => this.ComposeLiteral(sequence.ToList());

    public Graph ComposeLiteral(List<Node> sequence)
    {        
        for(int i= 0; i < sequence.Count; i++)
        {
            var before = i <= 0 ? this.Head : sequence[i - 1];
            var current = sequence[i];
            var after = i >= sequence.Count - 1 ? this.Tail : sequence[i + 1];
            this.Edges.Add(new (before, current));
            this.Edges.Add(new(current, after));
            this.Nodes.Add(current);
        }
        return this;
    }
    public Graph Concate(params Graph[] graphs)
        => Concate(graphs as IEnumerable<Graph>);
    public Graph Concate(IEnumerable<Graph> graphs)
        => this.Concate(graphs.ToList());
    public Graph Concate(List<Graph> graphs)
    {
        if(graphs.Count == 0)
        {
            return this;
        }
        else if(graphs.Count == 1)
        {
            this.Edges.Clear();
            this.Nodes.Clear();
            this.Edges.UnionWith(graphs[0].Edges);
            this.Nodes.UnionWith(graphs[0].Nodes);
            this.Head = graphs[0].Head;
            this.Tail = graphs[0].Tail;
            return this;
        }
        else if (graphs.Count == 2)
        {
            this.Nodes.Clear();
            this.Edges.Clear();

            this.Head = graphs[0].Head;
            this.Tail = graphs[^1].Tail;
            this.Nodes.UnionWith(graphs[0].Nodes);
            this.Nodes.UnionWith(graphs[^1].Nodes);
            this.Edges.UnionWith(graphs[0].Edges);
            this.Edges.UnionWith(graphs[^1].Edges);
            this.Edges.Add(new Edge(graphs[0].Tail, graphs[^1].Head));
        }
        else
        {
            this.Nodes.Clear();
            this.Edges.Clear();

            this.Head = graphs[0].Head;
            this.Tail = graphs[^1].Tail;
            this.Nodes.UnionWith(graphs[0].Nodes);
            this.Nodes.UnionWith(graphs[^1].Nodes);
            this.Edges.UnionWith(graphs[0].Edges);
            this.Edges.UnionWith(graphs[^1].Edges);

            for (int i = 1; i < graphs.Count - 1; i++)
            {
                var before = graphs[i - 1];
                var current = graphs[i + 0];
                var after = graphs[i + 1];
                this.Nodes.UnionWith(current.Nodes);
                this.Edges.UnionWith(current.Edges);
                this.Edges.Add(new Edge(before.Tail, current.Head));
                this.Edges.Add(new Edge(current.Tail, after.Head));
            }
        }
        return this;
    }

    public Graph UnionWith(params Graph[] gs)
        => this.UnionWith(gs as IEnumerable<Graph>);
    public Graph UnionWith(IEnumerable<Graph> gs)
    {
        foreach(var g in gs) this.UnionWith(g);
        return this;
    }
    public Graph UnionWith(params Node[] nodes)
        => this.UnionWith(nodes as IEnumerable<Node>);
    public Graph UnionWith(IEnumerable<Node> nodes)
        => this.UnionWith(nodes.ToList());
    public Graph UnionWith(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            this.Head.Outputs.Add(n);
            n.Inputs.Add(this.Head);
            n.Outputs.Add(this.Tail);
            this.Tail.Inputs.Add(n);
        }
        return this;
    }
    public Graph UnionWith(Graph g)
    {
        this.Edges.Add(new(this.Head, g.Head));
        this.Edges.Add(new(g.Tail, this.Tail));
        
        this.Edges.UnionWith(g.Edges);
        this.Nodes.UnionWith(g.Nodes);
        this.Description = !string.IsNullOrEmpty(Description) 
            ? $"({this.Description} | {g.Description})"
            : $"({g.Description})";
        return this;
    }
    public Graph ZeroPlus(Graph g0)
    {
        this.Edges.Add(new(g0.Head, g0.Tail)); //direct pass
        this.Edges.Add(new(g0.Tail, g0.Head)); //back link
        this.Description = "(" + g0.Description + ")*";
        return this.EmbedOne(g0);
    }
    public Graph OnePlus(Graph g0)
    {
        this.Edges.Add(new(g0.Tail, g0.Head)); //back link
        this.Description = "(" + g0.Description + ")+";
        return this.EmbedOne(g0);
    }
    public Graph ZeroOne(Graph g0)
    {
        this.Edges.Add(new(g0.Head, g0.Tail)); //direct pass
        this.Description = $"({g0.Description})?";
        return this.EmbedOne(g0);
    }
    public Graph EmbedOne(Graph g0)
    {
        this.Nodes.Add(this.Head = new (this.Name));
        this.Nodes.Add(this.Tail = new (this.Name));
        this.Edges.Add(new(this.Head, g0.Head));
        this.Edges.Add(new(g0.Tail, this.Tail));
        this.Edges.UnionWith(g0.Edges);
        this.Nodes.UnionWith(g0.Nodes);
        return this;
    }

    public Graph ComposeRepeats(Graph graph, int min, int max)
    {
        min = min <= 0 ? 0 : min;
        max = max <= 0 ? 0 : max;

        var fols = new List<Graph>();
        for (int i = 0; i < max; i++)
        {
            var ng = graph with { };
            fols.Add(ng);
            if (i >= min && i < max - 1)
            {
                ng.Tail.Outputs.Add(this.Tail);
                this.Tail.Inputs.Add(ng.Tail);
            }
        }

        this.Concate(fols);

        return this;
    }

    public override string ToString() => $"H:{this.Head},T:{this.Tail}";


    protected HashSet<Node>? heads = null;
    public HashSet<Node> Heads => (this.heads ??= this.Compact());

    protected HashSet<Node> Compact()
    {
        var inits = this.Nodes.Where(n => n.Inputs.Count == 0).ToHashSet();
        var nodes = inits.ToHashSet();
        var visited = nodes.ToHashSet();

        var heads = new HashSet<Node>();
        do
        {
            heads.UnionWith(nodes.Where(n => !n.IsVirtual));
            var virtuals = nodes.Where(n => n.IsVirtual).ToHashSet();
            nodes = virtuals.SelectMany(n => n.Outputs).ToHashSet();
            virtuals.ToList().ForEach(v =>
                v.Outputs.ToList().ForEach(o => o.Inputs.Remove(v)));
            this.Nodes.ExceptWith(virtuals);
            nodes.ExceptWith(visited);
            visited.UnionWith(nodes);
        } while (nodes.Count > 0);

        nodes = heads.ToHashSet();
        while (nodes.Count > 0)
        {
            nodes = nodes.SelectMany(n => n.Outputs).ToHashSet();
            var virtuals= nodes.Where(n => n.IsVirtual).ToHashSet();
            foreach (var node in virtuals)
            {
                foreach (var i in node.Inputs)
                {
                    i.Outputs.Remove(node);
                    i.Outputs.UnionWith(node.Outputs);
                }
                foreach (var o in node.Outputs)
                {
                    o.Inputs.Remove(node);
                    o.Inputs.UnionWith(node.Inputs);
                }
            }
            this.Nodes.ExceptWith(virtuals);
        }
        return heads;
    }


    public bool IsBacktracingFriendly()
    {
        //对于支持回溯的RegEx引擎，
        //  1.如果相继&的两个node之间具有交集，则有可能出现回溯灾难。
        //  2.如果前一个是{0,n}后一个和前一个无交集，则有可能出现回溯灾难。
        //本引擎不处理回溯灾难
        var heads = this.Nodes.Where(n => n.Inputs.Count == 0).ToHashSet();
        var copies = heads.ToHashSet();
        do
        {
            foreach (var head in heads)
                if (head.IsVirtual)
                    head.CharSet.UnionWith(
                        head.Inputs.SelectMany(i => i.CharSet));

            heads = heads.SelectMany(h => h.Outputs).ToHashSet();
        } while (heads.Count > 0);

        heads = copies;
        do
        {
            foreach (var head in heads)
                //a&b: a and b's charset shares elements
                if (head.Inputs.Count == 1
                    && head.CharSet.Overlaps(head.Inputs.Single().CharSet))
                    return false;

            heads = heads.SelectMany(h => h.Outputs).ToHashSet();
        } while (heads.Count > 0);
        return true;
    }

    public static StringBuilder ExportGraph(Graph graph, StringBuilder builder = null)
    {
        builder ??= new StringBuilder();
        builder.AppendLine($"digraph {(!string.IsNullOrEmpty(graph.Name) ? graph.Name:"g")}{{");
        foreach(var node in graph.Nodes)
        {
            var label = (!string.IsNullOrEmpty(node.Name) ? $"[label=\"{node.Id}({node.Name})\"]" : "");
            builder.AppendLine($"\t{node.Id} {label};");
        }
        foreach(var edge in graph.Edges)
        {
            builder.AppendLine($"\t{edge.Head.Id}->{edge.Tail.Id};");
        }
        builder.AppendLine("}");
        return builder;
    }

    public static HashSet<Node> GetFollowings(Graph graph, Node current, HashSet<Node> visited) 
        => graph.Edges.Where(e => e.Head == current && visited.Add(e.Tail)).Select(e => e.Tail).ToHashSet();
    public static Graph RebuldIds(Graph graph, int id = 0)
    {
        var visited = new HashSet<Node>();
        var current = graph.Head;

        var followings = GetFollowings(graph, current, visited);
        var list = new List<HashSet<Node>>
        {
            new() { current },
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
            list.Add(collectings);
            collectings = new();
        }while(followings.Count > 0);
        foreach(var line in list)
        {
            foreach(var node in line)
            {
                node.SetId(id++);
            }
        }
        return graph;
    }
}
