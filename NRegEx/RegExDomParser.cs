namespace NRegEx;

[Flags]
public enum RegExTokenType: int
{
    EOF = -1,
    Literal = 0,
    CharClass = 1,
    FoldCase = 2,
    AnyCharExcludingNewLine = 3,
    AnyCharIncludingNewLine = 4,
    BeginLine = 5,
    EndLine = 6,
    BeginText = 7,
    EndText = 8,
    WordBoundary = 9,
    NotWordBoundary = 10,
    Capture = 11,
    ZeroPlus = 12,      //*
    OnePlus = 13,       //+
    ZeroOne = 14,       //?
    Repeats = 15,
    Concate = 16,       //&
    Sequence = 17,      //.&.&.&...
    Alternate = 18,     // |
    Union = 19,         //..|..|..
    OpenParenthesis = 19,     //(
}
public record class RegExNode(
    RegExTokenType Type = RegExTokenType.EOF,string Value = "",
    int? Min = null,int? Max = null, int? CaptureIndex = null, bool? Negate=null) 
{
    public List<RegExNode> Children = new ();
}
public class RegExDomParser
{
    public readonly RegExPatternReader Reader;
    public readonly RegExParserOptions Options;
    public string Pattern => this.Reader.Pattern;
    protected readonly Stack<RegExNode> TokenStack = new();
    protected int CaptureIndex = 0;
    public RegExDomParser(string pattern, RegExParserOptions options)
    {
        this.Reader = new RegExPatternReader(
            pattern ?? throw new ArgumentNullException(nameof(pattern)));
        this.Options = options;
    }
    public bool HasMore=>this.Reader.Peek()!=-1;

    protected int Peek() => this.Reader.Peek();
    public RegExNode Parse()
    {
        
        while (this.HasMore)
        {
            switch (this.Reader.Peek())
            {
                default:
                    this.Push(new(RegExTokenType.Literal, this.Reader.TakeString()));
                    break;
                case RegExTextReader.EOF:
                    this.Push(new());
                    break;
                case '&':
                    this.Push(new(RegExTokenType.Concate, this.Reader.TakeString()));
                    break;
                case '|':
                    this.InprogressAlternate();
                    break;
                case '^':
                    this.Push(new(
                        ((this.Options & RegExParserOptions.ONE_LINE) != RegExParserOptions.None)
                        ? RegExTokenType.BeginLine : RegExTokenType.BeginText
                        , this.Reader.TakeString()));
                    break;
                case '$':
                    this.Push(new(
                        ((this.Options & RegExParserOptions.ONE_LINE) != RegExParserOptions.None)
                        ? RegExTokenType.EndLine : RegExTokenType.EndText
                        , this.Reader.TakeString()));
                    break;
                case '.':
                    this.Push(new(
                        ((this.Options & RegExParserOptions.DOT_NL) != RegExParserOptions.None)
                        ? RegExTokenType.AnyCharExcludingNewLine : RegExTokenType.AnyCharExcludingNewLine
                        , this.Reader.TakeString()));
                    break;
                case '*':
                    this.Push(new(RegExTokenType.ZeroPlus, 
                        this.Reader.TakeString(), 0, -1)
                        { Children = new (){ this.TokenStack.Pop() } });
                    break;
                case '+':
                    this.Push(new(RegExTokenType.OnePlus, 
                        this.Reader.TakeString(),1, -1)
                        { Children = new() { this.TokenStack.Pop() } });
                    break;
                case '?':
                    this.Push(new(RegExTokenType.ZeroOne,
                        this.Reader.TakeString(), 0, 1)
                        { Children = new() { this.TokenStack.Pop() } });
                    break;
                case '{':
                    {
                        this.Reader.Enter();
                        var (found, min, max) = this.ParseRepeat(this.Reader);
                        if (!found)
                            {
                            this.Reader.Leave();
                            this.Push(new(RegExTokenType.Literal, this.Reader.TakeString()));
                        }
                        else
                        {
                            this.Reader.Discard();
                            this.Push(new(RegExTokenType.Repeats,
                                this.Reader.TakeString(), 0, 1)
                            { Children = new() { this.TokenStack.Pop() } });
                        }
                    }
                    break;

                case '[':
                    this.ParseClass(Reader);
                    break;
                case '(':
                    if (((this.Options & RegExParserOptions.PERL) != RegExParserOptions.None) &&
                        this.Reader.LookingAt("(?"))
                    {
                        this.ParsePerlFlags(Reader);
                    }
                    else
                    {
                        this.Push(
                            new(RegExTokenType.OpenParenthesis, Reader.TakeString(), CaptureIndex: ++CaptureIndex));
                    }
                    break;
                case ')':
                    this.ParseCloseParenthesis();
                    break;
                case '\\':
                    this.ParseBackslash(Reader);
                    break;
            }
            this.Concate();
            this.OverallAlternate();
        }

        if (this.TokenStack.Count != 1)
            throw new RegExSyntaxException(
                RegExParser.ERR_MISSING_PAREN, this.Pattern);
        return this.TokenStack.Pop();
    }

    protected void ParseCloseParenthesis()
    {
        this.Concate();
        if (this.SwapAlternate())
        {
            this.Pop();
        }
        this.OverallAlternate();

        if (this.StackDepth < 2)
        {
            throw new RegExSyntaxException(RegExParser.ERR_INTERNAL_ERROR, "Stack Underflow");
        }
        var node = this.Pop();
        var right = this.Pop();
        if(right.Type!= RegExTokenType.OpenParenthesis)
        {
            throw new RegExSyntaxException(RegExParser.ERR_MISSING_PAREN, this.Pattern);
        }
        if(right.CaptureIndex == null) 
        {
            this.Push(node);
        }
        else
        {
            this.Push(new(
                RegExTokenType.Capture) { Children = new() { node } });
        }
    }
    protected void Concate()
    {
        var nodes = new List<RegExNode>();
        while(this.StackDepth > 0 
            && this.Top.Type < RegExTokenType.OpenParenthesis)
        {
            var top = this.Pop();
            if(top.Type!= RegExTokenType.Concate)
            {
                nodes.Add(top);
            }
        }
        if(nodes.Count > 0)
        {
            nodes.Reverse();
            this.Push(
                new(RegExTokenType.Sequence) { Children = nodes });
        }
    }
    protected bool SwapAlternate()
    {
        var swaps = false;
        if (this.StackDepth > 1)
        {
            var node1 = this.Pop();
            var node2 = this.Top;
            if (swaps =(node2.Type == RegExTokenType.Alternate))
            {
                //do swap
                this.Push(node1);
                this.Push(node2);
                
            }
            else
            {
                //no swap
                this.Push(node2);
                this.Push(node1);
            }
        }
        return swaps;
    }
    protected void InprogressAlternate(string alt="|")
    {
        this.Concate();
        if(!this.SwapAlternate())
        {
            this.Push(new(RegExTokenType.Alternate, alt));
        }
    }
    protected void OverallAlternate()
    {
        var nodes = new List<RegExNode>();
        while (this.StackDepth > 0
            && this.Top.Type < RegExTokenType.OpenParenthesis)
        {
            var top = this.Pop();
            if (top.Type != RegExTokenType.Alternate)
            {
                nodes.Add(top);
            }
        }
        this.Push(
            new(RegExTokenType.Sequence) { Children = nodes });
    }
    protected void Push(RegExNode node)
    {
        this.TokenStack.Push(node);
    }
    protected RegExNode Pop()
    {
        return this.TokenStack.Pop();
    }
    protected RegExNode Top => this.TokenStack.Peek();
    protected int StackDepth => this.TokenStack.Count;

    private static int ParseInt(RegExPatternReader Reader)
    {
        int start = Reader.Position;
        int c;
        while (Reader.HasMore && (c = Reader.Peek()) >= '0' && c <= '9')
        {
            Reader.Skip(1); // digit
        }
        string n = Reader.From(start);
        if (string.IsNullOrEmpty(n) || (n.Length > 1 && n[0] == '0'))
        { // disallow leading zeros
            return -1; // bad format
        }
        if (n.Length > 8)
        {
            return -2; // overflow
        }

        return int.TryParse(n, out var r) ? r : -1;
    }

    protected (bool,int?,int?) ParseRepeat(RegExPatternReader Reader)
    {
        if (!HasMore||!Reader.LookingAt("{")) goto failed;
        Reader.Skip();
        int start = Reader.Position;
        int min = ParseInt(Reader); // (can be -2)
        if (min == -1) goto failed;
        if (!Reader.HasMore) goto failed;
        int max;
        if (!Reader.LookingAt(','))
            max = min;
        else
        {
            Reader.Skip(); // ','
            if (!Reader.HasMore) goto failed;
            if (Reader.LookingAt('}')) goto failed;
            else if ((max = ParseInt(Reader)) == -1) goto failed;
        }
        if (!Reader.HasMore || !Reader.LookingAt('}'))
        {
            goto failed;
        }
        Reader.Skip(1); // '}'
        if (min < 0 || min > 1000 || max == -2 || max > 1000 || (max >= 0 && min > max))
        {
            throw new RegExSyntaxException(RegExParser.ERR_INVALID_REPEAT_SIZE, Reader.From(start));
        }
        return (true,min,max); // success

    failed:

        return (false, null, null);
    }
    protected void ParsePerlFlags(RegExPatternReader Reader)
    {
        //TODO:
    }
    protected void ParseClass(RegExPatternReader Reader)
    {
        //TODO:
    }
    protected void ParseBackslash(RegExPatternReader Reader)
    {
        //TODO:
    }

}
