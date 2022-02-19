namespace NRegEx;
/**
 * 连接两状态结点的边
 * 标签label 即 驱动字符 其中 epsilon 即 ε
 */
public record class Edge
{
    public Node Head { get; }
    public Node Tail { get; }
    public string Label { get; }

    public Edge(Node Head, Node Tail, string Label = "")
    {
        this.Head = Head;
        this.Tail = Tail;   
        this.Label = Label;
        this.Head.OutEdges.Add(this);
        this.Tail.InEdges.Add(this);

    }
    public bool Hit(char c) 
        => this.Label == Graph.EPSILON || c.ToString() == this.Label;
    public override string ToString()
        => "Edge [Head=" + Head + ", Tail=" + Tail + ", Label=" + Label + "]";
}