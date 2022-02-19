using System.Text;

namespace NRegEx;

public record class Graph
{
    public const string EPSILON = "EPSILON";

    protected List<Edge> edges = new();
    protected Node head = new();
    protected Node tail = new();

    public Graph() { }

    public List<Edge> Edges => edges;

    public Node Head { get => head; set => this.head = value; }

    public Node Tail { get => tail; set => this.tail = value; }

    public void ResetID()
    {
        Node.ResetID();
    }

    /**
     * 根据操作符和操作对象来进行连接、联合、闭包处理
     * 由参数来判断调用构建NFA的函数
     * 处理a*时会调用处理单字符闭包的函数，对应的也有处理NFA闭包的函数
     */
    public Graph Star(object o)
    {
        if (o is Graph g)
        {
            AddStar(g);
        }
        else if (o is char c)
        {
            AddStar(c);
        }
        return this;
    }

    public Graph Union(object o1, object o2)
    {
        if (o1 is char c1)
        {
            if (o2 is Graph g2)
            {
                AddUnion(c1, g2);
            }
            else if (o2 is char c2)
            {
                AddUnion(c1, c2);
            }
        }
        if (o1 is Graph g1)
        {
            if (o2 is Graph g2)
            {
                AddUnion(g1, g2);
            }
            else if (o2 is char c2)
            {
                AddUnion(g1, c2);
            }
        }
        return this;
    }

    public Graph Concat(object o1, object o2)
    {
        if (o1 is char c1)
        {
            if (o2 is Graph g2)
            {
                AddConcat(c1, g2);
            }
            else if (o2 is char c2)
            {
                AddConcat(c1, c2);
            }
        }
        else if (o1 is Graph g1)
        {
            if (o2 is Graph g2)
            {
                AddConcat(g1, g2);
            }
            if (o2 is char c2)
            {
                AddConcat(g1, c2);
            }
        }
        return this;
    }

    /**
     * 处理NFA闭包
     */
    public void AddStar(Graph g)
    {
        Node n1 = new();
        Node n2 = new();
        Edge e1 = new(n1, n2, EPSILON);
        Edge e2 = new(n1, g.Head, EPSILON);
        Edge e3 = new(g.Tail, n2, EPSILON);
        /**
         * 改动的地方
         */
        Edge e4 = new(g.Tail, g.Head, EPSILON);
        for (int i = 0; i < g.Edges.Count; i++)
        {
            this.edges.Add(g.Edges[i]);
        }
        this.edges.Add(e1);
        this.edges.Add(e2);
        this.edges.Add(e3);
        /**
         * 改动的地方
         */
        this.edges.Add(e4);
        this.head = n1;
        this.tail = n2;
    }

    /**
     * 处理单字符闭包
     */
    public void AddStar(char c)
    {
        Node n0 = new();
        Node n1 = new();
        Node n2 = new();
        Edge e0 = new(n2, n2, c.ToString());
        Edge e1 = new(n0, n2, EPSILON);
        Edge e2 = new(n2, n1, EPSILON);
        this.edges.Add(e0);
        this.edges.Add(e1);
        this.edges.Add(e2);
        this.head = n0;
        this.tail = n1;
    }

    public void AddUnion(char c, Graph g)
    {
        Node n1 = new();
        Node n2 = new();
        Edge e1 = new(n1, g.Head, EPSILON);
        Edge e2 = new(g.Tail, n2, EPSILON);
        Edge e3 = new(n1, n2, c.ToString());
        for (int i = 0; i < g.Edges.Count; i++)
        {
            this.edges.Add(g.Edges[i]);
        }
        this.edges.Add(e1);
        this.edges.Add(e2);
        this.edges.Add(e3);
        this.head = n1;
        this.tail = n2;
    }

    public void AddUnion(Graph g, char c)
    {
        Node n1 = new();
        Node n2 = new();
        Edge e1 = new(n1, g.Head, EPSILON);
        Edge e2 = new(g.Tail, n2, EPSILON);
        Edge e3 = new(n1, n2, c.ToString());
        for (int i = 0; i < g.Edges.Count; i++)
        {
            this.edges.Add(g.Edges[i]);
        }
        this.edges.Add(e1);
        this.edges.Add(e2);
        this.edges.Add(e3);
        this.head = n1;
        this.tail = n2;
    }

    public void AddUnion(Graph g1, Graph g2)
    {
        Node n1 = new();
        Node n2 = new();
        Edge e1 = new(n1, g1.Head, EPSILON);
        Edge e2 = new(n1, g2.Head, EPSILON);
        Edge e3 = new(g1.Tail, n2, EPSILON);
        Edge e4 = new(g2.Tail, n2, EPSILON);
        this.head = n1;
        this.tail = n2;
        for (int i = 0; i < g1.Edges.Count; i++)
        {
            this.edges.Add(g1.Edges[i]);
        }
        for (int i = 0; i < g2.Edges.Count; i++)
        {
            this.edges.Add(g2.Edges[i]);
        }
        this.edges.Add(e1);
        this.edges.Add(e2);
        this.edges.Add(e3);
        this.edges.Add(e4);
    }

    public void AddUnion(char c1, char c2)
    {
        Node n1 = new();
        Node n2 = new();
        Edge e1 = new(n1, n2, c1.ToString());
        Edge e2 = new(n1, n2, c2.ToString());
        edges.Add(e1);
        edges.Add(e2);
        head = n1;
        tail = n2;
    }

    public void AddConcat(char c, Graph g)
    {
        Node n = new();
        Edge e = new(n, g.Head, c.ToString());
        for (int i = 0; i < g.Edges.Count; i++)
        {
            this.edges.Add(g.Edges[i]);
        }
        this.edges.Add(e);
        this.head = n;
        this.tail = g.Tail;
    }

    public void AddConcat(Graph g, char c)
    {
        Node n = new();
        Edge e = new(g.Tail, n, c.ToString());
        for (int i = 0; i < g.Edges.Count; i++)
        {
            this.edges.Add(g.Edges[i]);
        }
        this.edges.Add(e);
        this.head = g.Head;
        this.tail = n;
    }

    public void AddConcat(Graph g1, Graph g2)
    {
        Edge edge = new(g1.Tail, g2.Head, EPSILON);
        this.head = g1.Head;
        this.tail = g2.Tail;
        for (int i = 0; i < g1.Edges.Count; i++)
        {
            this.edges.Add(g1.Edges[i]);
        }
        for (int i = 0; i < g2.Edges.Count; i++)
        {
            this.edges.Add(g2.Edges[i]);
        }
        this.edges.Add(edge);
    }

    public void AddConcat(char c1, char c2)
    {
        Node n0 = new();
        Node n1 = new();
        Node n2 = new();
        Edge e1 = new(n0, n1, c1.ToString());
        Edge e2 = new(n1, n2, c2.ToString());
        this.head = n0;
        this.tail = n2;
        this.edges.Add(e1);
        this.edges.Add(e2);
    }

    public override string ToString()
    {
        StringBuilder printString = new();
        printString.AppendLine("Start=" + this.head + "  End=" + this.tail);

        for (int i = 0; i < edges.Count; i++)
        {
            printString.AppendLine("" + edges[i]);
        }
        return printString.ToString();
    }
}
