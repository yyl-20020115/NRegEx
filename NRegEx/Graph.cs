namespace NRegEx;
public class Graph
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
            this.Nodes.Add(this.Head = this.Tail = new Node(cs));
            this.Description = this.Head.ToString();
        }
        else
        {
            this.Nodes.Add(this.Head = new(name));
            this.Nodes.Add(this.Tail = new(name));
        }
    }
    public Graph Copy()
    {
        var copy = new Graph(this.Name) { id = id };
        foreach (var node in this.Nodes)
        {
            copy.Nodes.Add(node.Copy());
        }
        copy.Head = copy.Nodes.First(n => n.Id == this.Head.Id);
        copy.Tail = copy.Nodes.First(n => n.Id == this.Tail.Id);
        foreach(var edge in this.Edges)
        {
            copy.Edges.Add(new (
                copy.Nodes.First(n => n.Id == edge.Head.Id),
                copy.Nodes.First(n => n.Id == edge.Tail.Id)
                ));
        }

        return copy;
    }
    public Graph TryComplete()
    {
        if (this.Edges.Count == 0)
        {
            this.Edges.Add(new(this.Head, this.Tail));
        }
        return this;
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
    public Graph Concate(IEnumerable<Graph> graphs, bool plus = false)
        => this.Concate(graphs.ToList());
    public Graph Concate(List<Graph> graphs, bool plus = false)
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
            this.Edges.Add(new (graphs[0].Tail, graphs[^1].Head));
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
                this.Edges.Add(new (before.Tail, current.Head));
                this.Edges.Add(new (current.Tail, after.Head));
            }
        }
        if (plus)
        {
            //back to self
            this.Edges.Add(new(graphs[^1].Tail, graphs[^1].Head));
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
            var node = nodes[i];
            this.Nodes.Add(node);
            this.Edges.Add(new Edge(this.Head, node));
            this.Edges.Add(new Edge(node, this.Tail));
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
    public Graph ZeroPlus(Graph g, HashSet<Node>? loopset = null)
    {
        this.Edges.Add(new(this.Head, this.Tail)); //direct pass
        this.Edges.Add(new(g.Tail, g.Head)); //back link
        this.Description = $"({g.Description})*";
        loopset?.UnionWith(this.Nodes);
        loopset?.UnionWith(g.Nodes);
        return this.EmbedOne(g);
    }
    public Graph OnePlus(Graph g, HashSet<Node>? loopset = null)
    {
        this.Edges.Add(new(g.Tail, g.Head)); //back link
        this.Description = $"({g.Description})+";
        loopset?.UnionWith(this.Nodes);
        loopset?.UnionWith(g.Nodes);
        return this.EmbedOne(g);
    }
    public Graph ZeroOne(Graph g)
    {
        this.Edges.Add(new(this.Head, this.Tail)); //direct pass
        this.Description = $"({g.Description})?";
        return this.EmbedOne(g);
    }
    public Graph EmbedOne(Graph g)
    {
        this.Edges.Add(new(this.Head, g.Head));
        this.Edges.Add(new(g.Tail, this.Tail));
        this.Edges.UnionWith(g.Edges);
        this.Nodes.UnionWith(g.Nodes);
        return this;
    }

    public Graph ComposeRepeats(Graph graph, int min, int max)
    {
        var plus = max < 0;

        min = min <= 0 ? 0 : min;
        max = max <= 0 ? min : max;

        var followings = new List<Graph>();
        for (int i = 0; i < max; i++)
        {
            followings.Add(graph.Copy());
        }

        this.Concate(followings, plus);

        return this;
    }

    public override string ToString() => $"H:{this.Head},T:{this.Tail}";
}
