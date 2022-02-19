using System.Text;

namespace NRegEx;

public record class Node
{
    public static int Nid = 0;
    public readonly char C;
    public readonly int Id ;

    public bool IsVirtual => this.C == '\0';
    public HashSet<Node> Inputs { get; }= new HashSet<Node>();
    public HashSet<Node> Outputs { get; }= new HashSet<Node>();
    public Node(char c = '\0')
    {
        this.Id = ++Nid;
        this.C = c;
    }
    public bool Hit(char c) => this.C == c;
    public string FormatNodes(IEnumerable<Node> nodes)
    {
        var builder = new StringBuilder();
        var ns = new List<Node>(nodes);
        for(int i = 0; i < ns.Count; i++)
        {
            var node = ns[i];
            builder.Append(node.Id);
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
    public override string ToString() => "["+this.Id + ":" 
        + (C!='\0'? C:' ')
        +$"  IN:{this.FormatNodes(this.Inputs)}  OUT:{this.FormatNodes(this.Outputs)}"+"]";
}
public record class Edge
{
    public Node Head { get; }
    public Node Tail { get; }
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

    public string Description { get; protected set; }

    public HashSet<Node> Nodes { get; } = new ();
    public HashSet<Edge> Edges { get; } = new ();   

    public Graph(char c = '\0')
    {
        this.id = ++Gid;
        if (c != '\0')
        {
            this.Nodes.Add(this.Head = this.Tail = new(c));
            this.Description = this.Head.ToString();
        }
    }
    public Node Head { get; protected set; }
    public Node Tail { get; protected set; }   

    public Graph Concate(Graph g2,Graph g1)
    {
        this.Head = g1.Head;
        this.Tail = g2.Tail;
        this.Edges.UnionWith(g1.Edges);
        this.Edges.UnionWith(g2.Edges);
        this.Edges.Add(new (g1.Tail, g2.Head));
        this.Nodes.UnionWith(g1.Nodes);
        this.Nodes.UnionWith(g2.Nodes);
        this.Description = "(" + g1.Description + " & " + g2.Description + ")";
        return this;
    }
    public Graph Union(Graph g2,Graph g1)
    {
        this.Nodes.Add(this.Head = new Node());
        this.Nodes.Add(this.Tail = new Node());
        this.Edges.Add(new (this.Head, g1.Head));
        this.Edges.Add(new (this.Head, g2.Head));
        this.Edges.Add(new (g1.Tail, this.Tail));
        this.Edges.Add(new (g2.Tail, this.Tail));
        this.Edges.UnionWith(g1.Edges);
        this.Edges.UnionWith(g2.Edges);
        this.Nodes.UnionWith(g1.Nodes);
        this.Nodes.UnionWith(g2.Nodes);
        this.Description = "("+g1.Description+" | "+g2.Description+")";
        return this;
    }
    public Graph Star(Graph g0)
    {
        this.Nodes.Add(this.Head = new Node());
        this.Nodes.Add(this.Tail = new Node());
        this.Edges.Add(new (this.Head, g0.Head));
        this.Edges.Add(new (g0.Tail, this.Tail));
        this.Edges.Add(new(g0.Tail, g0.Head));
        this.Edges.UnionWith(g0.Edges);
        this.Nodes.UnionWith(g0.Nodes);
        
        this.Description = "(" + g0.Description + ")*";
        return this;
    }
    public override string ToString() => $"H:{this.Head},T:{this.Tail}";
}
