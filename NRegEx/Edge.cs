namespace NRegEx;
/**
 * 连接两状态结点的边
 * 标签label 即 驱动字符 其中 epsilon 即 ε
 */
public record class Edge(Node Head, Node Tail, string Label = "")
{
    public override string ToString()
        => "Edge [Head=" + Head + ", Tail=" + Tail + ", Label=" + Label + "]";
}