using System.Text;

namespace NRegEx;

public record class Node
{
    public const int EOFChar = -1;
    public const int NewLineChar = '\n';
    public const int ReturnChar = '\r';

    public readonly static HashSet<int> AllChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).ToHashSet();
    public readonly static HashSet<int> WordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => char.IsLetter((char)i)).ToHashSet();
    public readonly static HashSet<int> NonWordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => !char.IsLetter((char)i)).ToHashSet();

    protected static int Nid = 0;
    protected int id;
    public bool IsVirtual => this.CharSet==null || this.CharSet.Count == 0;

    public readonly bool Inverted;
    public readonly string Name;

    public int Id => id;

    public int SetId(int id) => this.id = id;
    public readonly HashSet<int>? CharSet = null;
    public readonly HashSet<Node> Inputs = new ();
    public readonly HashSet<Node> Outputs = new ();
    public Node(string name = "") => Name = name;
    public Node(params Rune[] runes)
        : this(runes.Select(r=>r.Value).ToArray()) { }
    public Node(params char[] chars)
        : this(chars.Select(c => (int)c).ToArray()) { }
    public Node(params int[] chars)
        : this(false, chars) { }
    public Node(bool inverted, params char[] chars)
        : this(inverted, chars.Select(c => (int)c).ToArray()) { }

    public Node(bool inverted, params int[] chars)
    {
        this.id = ++Nid;
        if (chars!=null && chars.Length > 0)
        {
            this.CharSet = (this.Inverted = inverted) ? new(AllChars.Except(chars)) : new(chars);
            this.Name = "'" + string.Join(",", chars.Select(c => new Rune(c >= 0 ? c : ' ').ToString()).ToArray()) + "'";
        }
        else 
        {
            this.Name = string.Empty;
        }
    }

    public Node UnionWith(params int[] runes)
        => this.UnionWith(runes as IEnumerable<int>);
    public Node UnionWith(IEnumerable<int> runes)
    {
        this.CharSet?.UnionWith(runes);
        return this;
    }
    public bool? Hit(int c) => CharSet?.Contains(c);

    public static string FormatNodes(IEnumerable<Node> nodes) 
        => string.Join(',', nodes.Select(n => n.id).ToArray());
    public override string ToString() 
        => $"[({this.Id},{(this.Inverted?'T':'F')}):{string.Join(',',this.CharSet??new())} IN:{FormatNodes(this.Inputs)}  OUT:{FormatNodes(this.Outputs)}]";
}
