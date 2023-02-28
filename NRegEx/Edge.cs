namespace NRegEx;

public class Edge
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
    public override int GetHashCode() => Head.GetHashCode() ^ Tail.GetHashCode();
    public override bool Equals(object? obj)
        => obj is Edge e ? this.Head == e.Head && this.Tail == e.Tail : base.Equals(obj);
}
