namespace NRegEx;

public record class Path
{
    public readonly List<Node> Nodes = new();
    public readonly HashSet<Node> InternalNodeSet = new();
    public Path(params Node[] nodes) => this.AddNodes(nodes);

    protected Path(List<Node> nodes, HashSet<Node> internalNodeSet)
    {
        Nodes = nodes;
        InternalNodeSet = internalNodeSet;
    }
    public int Length => this.Nodes.Count;
    public bool IsEmpty=>this.Nodes.Count==0;

    public Node? Start => !this.IsEmpty ? this.Nodes[0] : null;
    public Node? End => !this.IsEmpty ? this.Nodes[^1] : null;

    public Path AddNodes(params Node[] nodes) => AddNodes(nodes as IEnumerable<Node>);
    public Path AddNodes(IEnumerable<Node> nodes)
    {
        foreach(Node node in nodes)
        {
            this.Nodes.Add(node);
            this.InternalNodeSet.Add(node);
        }
        return this;
    }
    public bool HasVisited(Node node) => InternalNodeSet.Contains(node);

    public Path Copy() => new Path(
            this.Nodes.ToList(),
            this.InternalNodeSet.ToHashSet());
}
