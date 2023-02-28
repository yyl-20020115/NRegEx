using System.Collections;
using System.Text;

namespace NRegEx;

public class Node
{
    public const int EOFChar = -1;
    public const int NewLineChar = '\n';
    public const int ReturnChar = '\r';

    public readonly static int[] AllChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).ToArray();
    public readonly static int[] WordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => !char.IsSurrogate((char)i) && char.IsLetter((char)i)).ToArray();
    public readonly static int[] NonWordChars = Enumerable.Range(
                    char.MinValue,
                    char.MaxValue - char.MinValue + 1).Where(i => !char.IsSurrogate((char)i) && !char.IsLetter((char)i)).ToArray();
    //public static bool IsRuneSurrogate(int i)
    //=> i >= char.MinValue && i <= char.MaxValue && char.IsSurrogate((char)i);
    //public static bool IsRuneLetter(int i)
    //    => Rune.GetUnicodeCategory(new Rune(i))
    //        is System.Globalization.UnicodeCategory.LowercaseLetter
    //        or System.Globalization.UnicodeCategory.UppercaseLetter
    //        or System.Globalization.UnicodeCategory.TitlecaseLetter
    //        or System.Globalization.UnicodeCategory.ModifierLetter
    //        or System.Globalization.UnicodeCategory.OtherLetter
    //        ;
    //public readonly static BitSet AllChars = new(Unicode.MAX_RUNE, true);
    //public readonly static BitSet WordChars = new(Enumerable.Range(
    //                0, Unicode.MAX_RUNE).Where(i => !IsRuneSurrogate(i) && IsRuneLetter(i)).ToHashSet());
    //public readonly static BitSet NonWordChars = new(Enumerable.Range(
    //                0, Unicode.MAX_RUNE).Where(i => !IsRuneSurrogate(i) && !IsRuneLetter(i)).ToHashSet());

    protected static int Nid = 0;
    protected int id;
    public bool IsLink => this.CharSet == null || this.CharSet.Count == 0;
    public readonly bool Inverted;
    public readonly string Name ="";
    public bool IsBridge 
        => this.IsLink 
        && this.Inputs.Count == 1 
        && this.Outputs.Count == 1;

    public int Id => id;

    public int SetId(int id) => this.id = id;
    public readonly BitArray? CharSet = null;
    public readonly int[]? CharsArray;
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
            this.Inverted = inverted;
            this.CharsArray = chars;
            this.CharSet = new BitArray(chars.Max(m => m) + 1);
            foreach (var c in chars) this.CharSet[c] = true;
            var ts = chars.Select(c => new Rune(c >= 0 ? c : ' ').ToString());
            var tt = string.Join(",", ts.ToArray());
            var st = Utils.EscapeString(tt, true).PadRight(16)[..16].TrimEnd();
            this.Name = $"'[{(this.Inverted ? '-' : '+')}]" + st + "'";
        }
    }
    public Node FetchNodes(HashSet<Node> outputs)
    {
        FetchNodes(this,outputs);
        return this;
    }
    protected static void FetchNodes(Node node, HashSet<Node> outputs)
    {
        FetchNodes(node.Outputs, outputs);
    }
    protected static void FetchNodes(HashSet<Node> inputs, HashSet<Node> outputs)
    {
        foreach (var node in inputs)
        {
            if (outputs.Contains(node))
            {
                continue;
            }
            else if (!node.IsLink)
            {
                outputs.Add(node);
            }
            else
            {
                FetchNodes(node.Outputs, outputs);
            }
        }
    }

    public Node UnionWith(params int[] runes)
        => this.UnionWith(runes as IEnumerable<int>);
    public Node UnionWith(IEnumerable<int> runes)
    {
        foreach(var i in runes)
        {
            this.CharSet?.Set(i, true);
        }
        return this;
    }
    protected bool TryHitCore(int c)
        => this.CharSet!=null && c >= 0 && c < this.CharSet.Count && this.CharSet.Get(c);

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
        for(int i = 0;i<chars.Count;i++)
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
        => $"[{this.Id}({(this.Inverted?'T':'F')}){(this.CharSet!=null? ":"+FormatCharset(this.CharSet):"")}]";
}
