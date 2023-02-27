namespace NRegEx;

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
