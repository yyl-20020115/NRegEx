using System.Collections;
using System.Text;
using System.Xml.Linq;

namespace NRegEx;

public class Node
{
    public const int EOFChar = -1;
    public const int NewLineChar = '\n';
    public const int ReturnChar = '\r';
    public readonly static int[] AllChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i=>Unicode.IsValidUTF32(i)).ToArray();
    
    public readonly static int[] AllCharsWithoutNewLine = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i=>Unicode.IsValidUTF32(i) && i!='\n').ToArray();

    public readonly static int[] WordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => Unicode.IsValidUTF32(i) && Unicode.IsRuneLetter(i)).ToArray();
    public readonly static int[] NonWordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => Unicode.IsValidUTF32(i) && !Unicode.IsRuneLetter(i)).ToArray();

    protected static int Nid = 0;
    protected int id = Nid++;
    public bool IsLink => this.CharSet == null || this.CharSet.Count == 0;
    protected bool inverted;
    protected string name = "";
    protected Graph? parent = null;
    public bool HasInput => this.Inputs.Count > 0;
    public bool HasOutput =>this.Outputs.Count > 0;
    public bool HasSingleInput => this.Inputs.Count == 1;
    public bool HasSingleOutput=>this.Outputs.Count == 1;
    public bool IsBridge
        => this.IsLink
        && this.HasSingleInput
        && this.HasSingleOutput;

    public bool IsBroken
        => this.IsLink
        && !this.HasInput
        && !this.HasOutput;

    public int Id => id;
    public bool Inverted { get => inverted; protected set => inverted = value; }
    public string Name { get => name; protected set => name = value; }

    public BitArray? CharSet => charSet;
    public int[]? CharsArray => charsArray;

    public Graph? Parent { get => parent; set => parent = value; }

    public Node Copy(Graph? parent = null) => new()
    {
        name = name,
        inverted = inverted,
        charsArray = charsArray,
        charSet = charSet,
        id = id,
        parent = parent ?? this.parent
    };

    public int SetId(int id) => this.id = id;
    protected BitArray? charSet = null;
    protected int[]? charsArray;
    public readonly HashSet<Node> Inputs = new();
    public readonly HashSet<Node> Outputs = new();
    public readonly List<int> Captures = new();
    public Node(string name = "") => Name = name;
    public Node(params int[] chars)
        : this(false, chars) { }
    public Node(bool inverted, params char[] chars)
        : this(inverted, chars.Select(c => (int)c).ToArray()) { }

    public Node(bool inverted, params int[] chars)
    {
        if (chars != null && chars.Length > 0)
        {
            this.Inverted = inverted;
            this.charsArray = chars;
            this.charSet = new BitArray(chars.Max(m => m) + 1);
            foreach (var c in chars) this.charSet[c] = true;
            this.Name = $"'[{(this.Inverted ? '-' : '+')}]" +
                Utils.EscapeString(
                    Utils.RunesToString(chars.Where(c=>Unicode.IsValidUTF32(c)), ","), true).PadRight(16)[..16].TrimEnd()
                + "'";
        }
    }
    public void FetchNodes(HashSet<Node> nodes, bool deep = true, int direction = +1)
    {
        var subs = new HashSet<Node>(direction>=0 ? this.Outputs : this.Inputs);
        if (!deep)
        {
            nodes.UnionWith(subs);
            nodes.Remove(this);
        }
        else 
        {
            //always use loop instead of recursion to prevent stack overflow.
            var visited = new HashSet<Node>();
            do
            {
                var copy = subs.ToArray();
                subs.Clear();
                foreach (var sub in copy)
                {
                    if (sub == this) continue;
                    else if (!sub.IsLink)
                    {
                        nodes.Add(sub);
                    }
                    else if(visited.Add(sub))
                    {
                        subs.UnionWith(
                            direction >= 0 ? sub.Outputs : sub.Inputs);
                    }
                }
            } while (subs.Count>0);
        }
    }

    public Node UnionWith(params int[] runes)
        => this.UnionWith(runes as IEnumerable<int>);
    public Node UnionWith(IEnumerable<int> runes)
    {
        foreach (var i in runes)
        {
            this.charSet?.Set(i, true);
        }
        return this;
    }
    protected bool TryHitCore(int c)
        => this.charSet != null && c >= 0 && c < this.charSet.Count && this.charSet.Get(c);

    protected bool TryHitCoreWithInverted(int c)
    {
        var hit = this.TryHitCore(c);
        return this.Inverted ? (!hit) : hit;
    }

    public bool? TryHit(int c)
        => this.CharSet == null
        ? null
        : this.TryHitCoreWithInverted(c);

    public static string FormatNodes(IEnumerable<Node> nodes)
        => string.Join(',', nodes.Select(n => n.id).ToArray());
    public static string FormatCharset(BitArray chars)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < chars.Count; i++)
        {
            if (chars[i])
            {
                builder.Append('\'');
                builder.Append(char.ConvertFromUtf32(i));
                builder.Append('\'');
                if (i < chars.Count - 1)
                {
                    builder.Append(',');
                }
            }
        }
        return builder.ToString();
    }
    public override string ToString()
        => $"[{this.Id}({(this.Inverted ? 'T' : 'F')}){(this.charSet != null ? ":" + FormatCharset(this.charSet) : "")}]";
}
