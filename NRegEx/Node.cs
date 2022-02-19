namespace NRegEx;

/**
 * 状态结点类
 */
public record class Node
{
    public static void ResetID() => ID = 0;
    protected static int ID = 0;

    protected int id = 0;
    public Node() => this.id = ID++;

    public int Id => id;

    public override string ToString() => this.id.ToString();
}