using System.Text;

namespace NRegEx;

public record class Node
{
    public const int EOFChar = -1;
    public const int NewLineChar = '\n';
    public const int ReturnChar = '\r';

    protected static HashSet<int>? _AllChars = null;
    protected static HashSet<int>? _WordChars = null;
    protected static HashSet<int>? _NonWordChars = null;

    public static HashSet<int> AllChars => _AllChars ??= new(Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1));
    public static HashSet<int> WordChars => _WordChars ??= new(Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => char.IsLetter((char)i)));
    public static HashSet<int> NonWordChars => _NonWordChars ??= new(Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => !char.IsLetter((char)i)));

    public static Dictionary<int, HashSet<int>> KnownInvertedSets = new();

    public static int Nid = 0;
    public readonly int Id ;
    public readonly bool IsVirtual;
    public readonly bool Inverted;
    public string Name { get; set; } = String.Empty;
    public readonly HashSet<int> CharSet = new();
    public readonly HashSet<Node> Inputs = new ();
    public readonly HashSet<Node> Outputs = new ();
    public Node(string name = "")
        :this(Node.EOFChar)
    {
        this.Name = name;
        this.IsVirtual = true;
    }
    public Node(params char[] cs)
        : this(cs.Select(c => (int)c).ToArray()) { }
    public Node(params int[] cs)
    {
        this.IsVirtual = false;
        this.Id = ++Nid;
        this.Inverted = false;
        this.CharSet = new HashSet<int>(cs);
    }
    public Node(bool inverted, params char[] cs)
        : this(inverted, cs.Select(c => (int)c).ToArray()) { }

    public Node(bool inverted, params int[] cs)
    {
        this.IsVirtual = false;
        this.Name = "";
        this.Id = ++Nid;
        this.Inverted = inverted;

        if (this.Inverted)
        {
            this.CharSet = new HashSet<int>(AllChars);
            this.CharSet.ExceptWith(cs);
        }
        else
        {
            this.CharSet = new HashSet<int>(cs);
        }
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

    public string FormatNodes(IEnumerable<Node?> nodes)
    {
        var builder = new StringBuilder();
        var ns = new List<Node?>(nodes);
        for(int i = 0; i < ns.Count; i++)
        {
            var node = ns[i];
            builder.Append(node?.Id);
            if (i < ns.Count - 1)
            {
                builder.Append(',');
            }
        }
        if (ns.Count == 0)
        {
            builder.Append(' ');
        }
        return builder.ToString();
    }
    public override string ToString() => "["+this.Id + "(inverted:" 
        + this.Inverted +"):" + string.Join(',',this.CharSet)
        +$" IN:{this.FormatNodes(this.Inputs)}  OUT:{this.FormatNodes(this.Outputs)}"+"]";
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

    protected readonly int id = 0;
    public int Id => id;

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
            this.Head = new(name);
            this.Tail = new(name);
        }
    }
    public Graph Compose(params Node[] nodes)
        => this.Compose(nodes as IEnumerable<Node>);

    public Graph Compose(IEnumerable<Node> nodes)
        => this.Compose(nodes.ToList());

    public Graph Compose(List<Node> nodes)
    {        
        for(int i= 0; i < nodes.Count; i++)
        {
            var _pre = i > 0 ? nodes[i - 1] : this.Head;
            var node = nodes[i];
            var next = (i < nodes.Count - 1) ? nodes[i + 1] : this.Tail;
            _pre.Outputs.Add(node);
            node.Inputs.Add(_pre);
            node.Outputs.Add(next);
            next.Inputs.Add(node);
        }
        return this;
    }
    public Graph Concate(params Graph[] graphs)
        => Concate(graphs as IEnumerable<Graph>);
    public Graph Concate(IEnumerable<Graph> graphs)
        => this.Concate(graphs.ToList());
    public Graph Concate(List<Graph> graphs)
    {
        var head = this;
        for(int i= 0; i < graphs.Count; i++)
        {
            var graph = graphs[i];
            this.Concate(graph, head);
            head = graph;
        }
        return this;
    }
    public Graph ConcateWith(Graph tail)
    {
        return Concate(tail, this);
    }
    public Graph Concate(Graph tail, Graph head)
    {
        this.Head = head.Head;
        this.Tail = tail.Tail;
        this.Edges.UnionWith(head.Edges);
        this.Edges.UnionWith(tail.Edges);
        this.Edges.Add(new(head.Tail, tail.Head));
        this.Nodes.UnionWith(head.Nodes);
        this.Nodes.UnionWith(tail.Nodes);
        this.Description = "(" + head.Description + " & " + tail.Description + ")";
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
    public Graph UnionWith(Graph g0)
    {
        if(this.Head==null) 
            this.Nodes.Add(this.Head = new(this.Name));
        if(this.Tail==null)
            this.Nodes.Add(this.Tail = new(this.Name));
        
        this.Edges.Add(new(this.Head, g0.Head));
        this.Edges.Add(new(g0.Tail, this.Tail));
        
        this.Edges.UnionWith(g0.Edges);
        this.Nodes.UnionWith(g0.Nodes);
        this.Description = "(" + this.Description + " | " + g0.Description + ")";
        return this;
    }
    public Graph Union(Graph g2, Graph g1)
    {
        this.Nodes.Add(this.Head = new (this.Name));
        this.Nodes.Add(this.Tail = new (this.Name));
        this.Edges.Add(new(this.Head, g1.Head));
        this.Edges.Add(new(this.Head, g2.Head));
        this.Edges.Add(new(g1.Tail, this.Tail));
        this.Edges.Add(new(g2.Tail, this.Tail));
        this.Edges.UnionWith(g1.Edges);
        this.Edges.UnionWith(g2.Edges);
        this.Nodes.UnionWith(g1.Nodes);
        this.Nodes.UnionWith(g2.Nodes);
        this.Description = "(" + g1.Description + " | " + g2.Description + ")";
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
        this.Description = "(" + g0.Description + ")?";
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
    public HashSet<Node> Heads => (this.heads??=this.Compact());

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
}
