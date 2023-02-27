using System.Text;

namespace NRegEx;

[Flags]
public enum RegExTokenType : int
{
    EOF = -1,
    Literal = 0,
    CharClass = 1,
    FoldCase = 2, //NOT USED
    AnyCharExcludingNewLine = 3,
    AnyCharIncludingNewLine = 4,
    BeginLine = 5,
    EndLine = 6,
    BeginText = 7,
    EndText = 8,
    WordBoundary = 9,
    NotWordBoundary = 10,
    Capture = 11,       //NOT USED
    ZeroPlus = 12,      //*
    OnePlus = 13,       //+
    ZeroOne = 14,       //?
    Repeats = 15,
    Concate = 16,       //& = Sequence
    Sequence = 17,      //.&.&.&...
    Alternate = 18,     // |
    Union = 19,         //..|..|.. = Alternate
    OpenParenthesis = 19,     //(
}
public record class RegExNode(
    RegExTokenType Type = RegExTokenType.EOF,
    string Value = "",
    string Name = "",
    int? Min = null,
    int? Max = null,
    int? CaptureIndex = null,
    bool? Negate = null,
    int[]? Runes = null,
    RegExParserOptions Options = RegExParserOptions.None)
{
    public List<RegExNode> Children = new();
}
public class RegExDomParser
{
    // Unexpected error
    private const string ERR_INTERNAL_ERROR = "regexp/syntax: internal error";

    // Parse errors
    private const string ERR_INVALID_CHAR_CLASS = "invalid character class";
    private const string ERR_INVALID_CHAR_RANGE = "invalid character class range";
    private const string ERR_INVALID_ESCAPE = "invalid escape sequence";
    private const string ERR_INVALID_NAMED_CAPTURE = "invalid named capture";
    private const string ERR_INVALID_PERL_OP = "invalid or unsupported Perl syntax";
    private const string ERR_INVALID_REPEAT_OP = "invalid nested repetition operator";
    private const string ERR_INVALID_REPEAT_SIZE = "invalid repeat count";
    private const string ERR_MISSING_BRACKET = "missing closing ]";
    private const string ERR_MISSING_PAREN = "missing closing )";
    private const string ERR_MISSING_REPEAT_ARGUMENT = "missing argument to repetition operator";
    private const string ERR_TRAILING_BACKSLASH = "trailing backslash at end of expression";
    private const string ERR_DUPLICATE_NAMED_CAPTURE = "duplicate capture group name";
    public readonly string Name;
    public readonly RegExPatternReader Reader;
    public RegExParserOptions Options;
    public string Pattern => this.Reader.Pattern;
    protected readonly Stack<RegExNode> NodeStack = new();
    protected int CaptureIndex = 0;
    public static RegExNode Parse(string name,string pattern, RegExParserOptions options)
        => new RegExDomParser(name, pattern, options).Parse();
    public RegExDomParser(string name,string pattern, RegExParserOptions options)
    {
        this.Name = name;
        this.Reader = new RegExPatternReader(
            pattern ?? throw new ArgumentNullException(nameof(pattern)));
        this.Options = options;
    }
    protected int Peek() => this.Reader.Peek();
    public RegExNode Parse()
    {
        int c;
        while (-1 != (c = this.Peek()))
        {
            switch (c)
            {
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
                        this.Reader.TakeString(), "", 0, -1)
                    { Children = new() { this.NodeStack.Pop() } });
                    break;
                case '+':
                    this.Push(new(RegExTokenType.OnePlus,
                        this.Reader.TakeString(), "", 1, -1)
                    { Children = new() { this.NodeStack.Pop() } });
                    break;
                case '?':
                    this.Push(new(RegExTokenType.ZeroOne,
                        this.Reader.TakeString(), "", 0, 1)
                    { Children = new() { this.NodeStack.Pop() } });
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
                                this.Reader.TakeString(), "", min, max)
                            { Children = new() { this.NodeStack.Pop() } });
                        }
                    }
                    break;

                case '[':
                    this.ParseClass(Reader);
                    break;
                case '(':
                    if (((this.Options & RegExParserOptions.PERL) != RegExParserOptions.None) &&
                        this.Reader.LookingAt("(?"))
                        this.ParsePerlFlags(Reader);
                    else
                        this.Push(
                            new(RegExTokenType.OpenParenthesis, Reader.TakeString(), CaptureIndex: ++CaptureIndex));
                    break;
                case ')':
                    this.ParseCloseParenthesis();
                    break;
                case '\\':
                    this.ParseBackslash(Reader);
                    break;
                default:
                    this.Push(new(RegExTokenType.Literal, this.Reader.TakeString()));
                    break;
            }
            this.Concate();
            this.OverallAlternate();
        }

        if (this.NodeStack.Count != 1)
            throw new RegExSyntaxException(
                RegExParser.ERR_MISSING_PAREN, this.Pattern);
        
        return this.NodeStack.Pop();
    }

    protected void ParseCloseParenthesis()
    {
        this.Concate();
        if (this.SwapAlternate())
            this.Pop();
        this.OverallAlternate();

        if (this.StackDepth < 2)
            throw new RegExSyntaxException(RegExParser.ERR_INTERNAL_ERROR, "Stack Underflow");
        var node = this.Pop();
        var right = this.Pop();
        if (right.Type != RegExTokenType.OpenParenthesis)
            throw new RegExSyntaxException(RegExParser.ERR_MISSING_PAREN, this.Pattern);
        if (right.CaptureIndex == null)
            this.Push(node);
        else
            this.Push(new(
                RegExTokenType.Capture)
            { Children = new() { node } });
    }
    protected void Concate()
    {
        var nodes = new List<RegExNode>();
        while (this.StackDepth > 0
            && this.Top.Type < RegExTokenType.OpenParenthesis)
        {
            var top = this.Pop();
            if (top.Type != RegExTokenType.Concate)
                nodes.Add(top);
        }
        if (nodes.Count > 0)
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
            if (swaps = (node2.Type == RegExTokenType.Alternate))
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
    protected void InprogressAlternate(string alt = "|")
    {
        this.Concate();
        if (!this.SwapAlternate())
            this.Push(new(RegExTokenType.Alternate, alt));
    }
    protected void OverallAlternate()
    {
        var nodes = new List<RegExNode>();
        while (this.StackDepth > 0
            && this.Top.Type < RegExTokenType.OpenParenthesis)
        {
            var top = this.Pop();
            if (top.Type != RegExTokenType.Alternate)
                nodes.Add(top);
        }
        this.Push(
            new(RegExTokenType.Union, Name: this.Peek() == -1 ? this.Name : "") { Children = nodes });
    }
    protected void Push(RegExNode node)
        => this.NodeStack.Push(node);
    protected RegExNode Pop() => this.NodeStack.Pop();
    protected RegExNode Top => this.NodeStack.Peek();
    protected int StackDepth => this.NodeStack.Count;
    private static int ParseInt(RegExPatternReader Reader)
    {
        int start = Reader.Position;
        int c;
        while (Reader.HasMore && (c = Reader.Peek()) >= '0' && c <= '9')
            Reader.Skip(1);
        var n = Reader.From(start);
        if (string.IsNullOrEmpty(n) || (n.Length > 1 && n[0] == '0'))
        { // disallow leading zeros
            return -1; // bad format
        }
        if (n.Length > 8) return -2; // overflow

        return int.TryParse(n, out var r) ? r : -1;
    }

    protected (bool, int?, int?) ParseRepeat(RegExPatternReader Reader)
    {
        if (this.Peek() == -1 || !Reader.LookingAt("{")) goto failed;
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
            goto failed;
        Reader.Skip(1); // '}'
        if (min < 0 || min > 1000 || max == -2 || max > 1000 || (max >= 0 && min > max))
            throw new RegExSyntaxException(RegExParser.ERR_INVALID_REPEAT_SIZE, Reader.From(start));
        return (true, min, max); // success
    failed:
        return (false, null, null);
    }

    // isValidCaptureName reports whether name
    // is a valid capture name: [A-Za-z0-9_]+.
    // PCRE limits names to 32 bytes.
    // Python rejects names starting with digits.
    // We don't enforce either of those.
    private static bool IsValidCaptureName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        for (int i = 0; i < name.Length; ++i)
        {
            var c = name[i];
            if (c != '_' && !Utils.Isalnum(c))
                return false;
        }
        return true;
    }
    protected void ParsePerlFlags(RegExPatternReader Reader)
    {
        int startPos = Reader.Position;

        // Check for named captures, first introduced in Python's regexp library.
        // As usual, there are three slightly different syntaxes:
        //
        //   (?P<name>expr)   the original, introduced by Python
        //   (?<name>expr)    the .NET alteration, adopted by Perl 5.10
        //   (?'name'expr)    another .NET alteration, adopted by Perl 5.10
        //
        // Perl 5.10 gave in and implemented the Python version too,
        // but they claim that the last two are the preferred forms.
        // PCRE and languages based on it (specifically, PHP and Ruby)
        // support all three as well.  EcmaScript 4 uses only the Python form.
        //
        // In both the open source world (via Code Search) and the
        // Google source tree, (?P<expr>name) is the dominant form,
        // so that's the one we implement.  One is enough.
        var s = Reader.Rest;
        if (s.StartsWith("(?P<"))
        {
            // Pull out name.
            var end = s.IndexOf('>');
            if (end < 0)
                throw new PatternSyntaxException(ERR_INVALID_NAMED_CAPTURE, s);
            var name = s.Substring(4, end - 4); // "name"
            Reader.SkipString(name);
            Reader.Skip(5); // "(?P<>"
            if (!IsValidCaptureName(name))
            {
                throw new PatternSyntaxException(
                    ERR_INVALID_NAMED_CAPTURE, s[..end]); // "(?P<name>"
            }
            var re = new RegExNode(RegExTokenType.OpenParenthesis, Value: name, CaptureIndex: ++this.CaptureIndex);
            // Like ordinary capture, but named.
            //if (namedGroup.ContainsKey(name))
            //{
            //    throw new PatternSyntaxException(ERR_DUPLICATE_NAMED_CAPTURE, name);
            //}
            //namedGroups.Add(name, this.CaptureIndex);
            //re.name = name;
            return;
        }

        // Non-capturing group.  Might also twiddle Perl flags.
        Reader.Skip(2); // "(?"
        var flags = this.Options;
        int sign = +1;
        bool sawFlag = false;
        //loop:
        while (Reader.HasMore)
        {
            int c = Reader.Pop();
            switch (c)
            {
                default:
                    goto exit;
                // Flags.
                case 'i':
                    flags |= RegExParserOptions.FOLD_CASE;
                    sawFlag = true;
                    break;
                case 'm':
                    flags &= ~RegExParserOptions.ONE_LINE;
                    sawFlag = true;
                    break;
                case 's':
                    flags |= RegExParserOptions.DOT_NL;
                    sawFlag = true;
                    break;
                case 'U':
                    flags |= RegExParserOptions.NON_GREEDY;
                    sawFlag = true;
                    break;

                // Switch to negation.
                case '-':
                    if (sign < 0) goto exit;
                    sign = -1;
                    // Invert flags so that | above turn into &~ and vice versa.
                    // We'll invert flags again before using it below.
                    flags = ~flags;
                    sawFlag = false;
                    break;

                // End of flags, starting group or not.
                case ':':
                case ')':
                    if (sign < 0)
                    {
                        if (!sawFlag)
                        {
                            goto exit;
                        }
                        flags = ~flags;
                    }
                    if (c == ':')
                    {
                        // Open new group
                        this.Push(new RegExNode(RegExTokenType.OpenParenthesis));
                    }
                    this.Options = flags;
                    return;
            }
        }
    exit:
        throw new PatternSyntaxException(ERR_INVALID_PERL_OP, Reader.From(startPos));
    }
    // parseUnicodeClass() parses a leading Unicode character class like \p{Han}
    // from the beginning of t.  If one is present, it appends the characters to
    // to |cc|, advances |t| and returns true.
    //
    // Returns false if such a pattern is not present or UNICODE_GROUPS
    // flag is not enabled; |t.pos()| is not advanced in this case.
    // Indicates error by throwing PatternSyntaxException.
    private bool ParseUnicodeClass(RegExPatternReader Reader, CharClass cc)
    {
        int startPos = Reader.Position;
        if ((Options & RegExParserOptions.UNICODE_GROUPS) == 0 || (!Reader.LookingAt("\\p") && !Reader.LookingAt("\\P")))
            return false;
        Reader.Skip(1); // '\\'
                        // Committed to parse or throw exception.
        int sign = +1;
        int c = Reader.Pop(); // 'p' or 'P'
        if (c == 'P')
        {
            sign = -1;
        }
        if (!Reader.HasMore)
        {
            Reader.RewindTo(startPos);
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.Rest);
        }
        c = Reader.Pop();
        string name;
        if (c != '{')
            // Single-letter name.
            name = Utils.RuneToString(c);
        else
        {
            // Name is in braces.
            var rest = Reader.Rest;
            int end = rest.IndexOf('}');
            if (end < 0)
            {
                Reader.RewindTo(startPos);
                throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.Rest);
            }
            name = rest.Substring(0, end - 0); // e.g. "Han"
            Reader.SkipString(name);
            Reader.Skip(1); // '}'
                            // Don't use skip(end) because it assumes UTF-16 coding, and
                            // StringIterator doesn't guarantee that.
        }

        // Group can have leading negation too.
        //  \p{^Han} == \P{Han}, \P{^Han} == \p{Han}.
        if (!string.IsNullOrEmpty(name) && name[0] == '^')
        {
            sign = -sign;
            name = name.Substring(1);
        }

        var pair = UnicodeTable(name);
        if (pair == null)
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.From(startPos));
        var tab = pair.first;
        var fold = pair.second; // fold-equivalent table

        // Variation of CharClass.appendGroup() for tables.
        if ((Options & RegExParserOptions.FOLD_CASE) == 0 || fold == null)
            cc.AppendTableWithSign(tab, sign);
        else
        {
            // Merge and clean tab and fold in a temporary buffer.
            // This is necessary for the negative case and just tidy
            // for the positive case.
            cc.AppendClassWithSign(
                new CharClass().AppendTable(tab).AppendTable(fold).CleanClass().ToArray(), sign);
        }
        return true;
    }
    // RangeTables are represented as int[][], a list of triples (start, end,
    // stride).
    private static int[][] ANY_TABLE = new int[][]{
        new int[]{0, Unicode.MAX_RUNE, 1},
    };
    public class Pair<F, S>
    {
        public static Pair<F, S> of(F first, S second) => new(first, second);

        public readonly F first;
        public readonly S second;

        public Pair(F first, S second)
        {
            this.first = first;
            this.second = second;
        }
    }

    // unicodeTable() returns the Unicode RangeTable identified by name
    // and the table of additional fold-equivalent code points.
    // Returns null if |name| does not identify a Unicode character range.
    private static Pair<int[][], int[][]>? UnicodeTable(string name)
    {
        // Special case: "Any" means any.
        if (name.Equals("Any"))
            return Pair<int[][], int[][]>.of(ANY_TABLE, ANY_TABLE);
        if (UnicodeTables.CATEGORIES.TryGetValue(name, out var table))
            if (UnicodeTables.FOLD_CATEGORIES.TryGetValue(name, out var cat))
                return Pair<int[][], int[][]>.of(table, cat);
        if (UnicodeTables.SCRIPTS.TryGetValue(name, out table))
            if (UnicodeTables.FOLD_SCRIPT.TryGetValue(name, out var script))
                return Pair<int[][], int[][]>.of(table, script);
        return null;
    }

    protected void ParseClass(RegExPatternReader Reader)
    {
        int startPos = Reader.Position;
        Reader.Skip(1); // '['

        var cc = new CharClass();

        int sign = +1;
        if (Reader.HasMore && Reader.LookingAt('^'))
        {
            sign = -1;
            Reader.Skip(1); // '^'

            // If character class does not match \n, add it here,
            // so that negation later will do the right thing.
            if ((this.Options & RegExParserOptions.CLASS_NL) == 0)
                cc.AppendRange('\n', '\n');
        }

        bool first = true; // ']' and '-' are okay as first char in class
        while (!Reader.HasMore || Reader.Peek() != ']' || first)
        {
            // POSIX: - is only okay unescaped as first or last in class.
            // Perl: - is okay anywhere.
            if (Reader.HasMore && Reader.LookingAt('-') && (Options & RegExParserOptions.PERL_X) == 0 && !first)
            {
                var s = Reader.Rest;
                if (s.Equals("-") || !s.StartsWith("-]"))
                {
                    Reader.RewindTo(startPos);
                    throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.Rest);
                }
            }
            first = false;

            int beforePos = Reader.Position;

            // Look for POSIX [:alnum:] etc.
            if (Reader.LookingAt("[:"))
            {
                if (ParseNamedClass(Reader, cc))
                    continue;
                Reader.RewindTo(beforePos);
            }

            // Look for Unicode character group like \p{Han}.
            if (ParseUnicodeClass(Reader, cc))
                continue;

            // Look for Perl character class symbols (extension).
            if (ParsePerlClassEscape(Reader, cc))
                continue;
            Reader.RewindTo(beforePos);

            // Single character or simple range.
            int lo = ParseClassChar(Reader, startPos);
            int hi = lo;
            if (Reader.HasMore && Reader.LookingAt('-'))
            {
                Reader.Skip(1); // '-'
                if (Reader.HasMore && Reader.LookingAt(']'))
                {
                    // [a-] means (a|-) so check for final ].
                    Reader.Skip(-1);
                }
                else
                {
                    hi = ParseClassChar(Reader, startPos);
                    if (hi < lo)
                        throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.From(beforePos));
                }
            }
            if ((Options & RegExParserOptions.FOLD_CASE) == 0)
                cc.AppendRange(lo, hi);
            else
                cc.AppendFoldedRange(lo, hi);
        }
        Reader.Skip(1); // ']'

        cc.CleanClass();
        if (sign < 0)
            cc.NegateClass();
        Push(new RegExNode(RegExTokenType.CharClass, Options: this.Options, Runes: cc.ToArray()));
    }
    // parseClassChar parses a character class character and returns it.
    // wholeClassPos is the position of the start of the entire class "[...".
    // Pre: t at class char; Post: t after it.
    private static int ParseClassChar(RegExPatternReader t, int wholeClassPos)
    {
        if (!t.HasMore)
            throw new PatternSyntaxException(ERR_MISSING_BRACKET, t.From(wholeClassPos));

        // Allow regular escape sequences even though
        // many need not be escaped in this context.
        if (t.LookingAt('\\'))
            return ParseEscape(t);

        return t.Pop();
    }
    // parseEscape parses an escape sequence at the beginning of s
    // and returns the rune.
    // Pre: t at '\\'.  Post: after escape.
    private static int ParseEscape(RegExPatternReader Reader)
    {
        int startPos = Reader.Position;
        Reader.Skip(1); // '\\'
        if (!Reader.HasMore)
            throw new PatternSyntaxException(ERR_TRAILING_BACKSLASH);
        int c = Reader.Pop();
        //bigswitch:
        switch (c)
        {
            default:
                if (!Utils.Isalnum(c))
                {
                    // Escaped non-word characters are always themselves.
                    // PCRE is not quite so rigorous: it accepts things like
                    // \q, but we don't.  We once rejected \_, but too many
                    // programs and people insist on using it, so allow \_.
                    return c;
                }
                break;

            // Octal escapes.
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
                // Single non-zero digit is a backreference; not supported
                if (!Reader.HasMore || Reader.Peek() < '0' || Reader.Peek() > '7')
                    break;
                goto for_zero;
            /* fallthrough */
            case '0':
            for_zero:
                {
                    // Consume up to three octal digits; already have one.
                    int r = c - '0';
                    for (int i = 1; i < 3; i++)
                    {
                        if (!Reader.HasMore || Reader.Peek() < '0' || Reader.Peek() > '7')
                        {
                            break;
                        }
                        r = r * 8 + Reader.Peek() - '0';
                        Reader.Skip(1); // digit
                    }
                    return r;
                }

            // Hexadecimal escapes.
            case 'x':
                if (!Reader.HasMore)
                    break;
                c = Reader.Pop();
                if (c == '{')
                {
                    // Any number of digits in braces.
                    // Perl accepts any text at all; it ignores all text
                    // after the first non-hex digit.  We require only hex digits,
                    // and at least one.
                    int nhex = 0;
                    int r = 0;
                    for (; ; )
                    {
                        if (!Reader.HasMore)
                            goto outswitch;
                        c = Reader.Pop();
                        if (c == '}')
                            break;
                        int v = Utils.Unhex(c);
                        if (v < 0)
                            goto outswitch;
                        r = r * 16 + v;
                        if (r > Unicode.MAX_RUNE)
                            goto outswitch;
                        nhex++;
                    }
                    if (nhex == 0)
                        goto outswitch;
                    return r;
                }

                // Easy case: two hex digits.
                int x = Utils.Unhex(c);
                if (!Reader.HasMore)
                    break;
                c = Reader.Pop();
                int y = Utils.Unhex(c);
                if (x < 0 || y < 0)
                    break;
                return x * 16 + y;

            // C escapes.  There is no case 'b', to avoid misparsing
            // the Perl word-boundary \b as the C backspace \b
            // when in POSIX mode.  In Perl, /\b/ means word-boundary
            // but /[\b]/ means backspace.  We don't support that.
            // If you want a backspace, embed a literal backspace
            // character or use \x08.
            case 'a':
                return '\a'; // No \a in Java
            case 'f':
                return '\f';
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case 'v':
                return 11; // No \v in Java
        }
    outswitch:
        throw new PatternSyntaxException(ERR_INVALID_ESCAPE, Reader.From(startPos));
    }

    // parsePerlClassEscape parses a leading Perl character class escape like \d
    // from the beginning of |t|.  If one is present, it appends the characters
    // to cc and returns true.  The iterator is advanced past the escape
    // on success, undefined on failure, in which case false is returned.
    private bool ParsePerlClassEscape(RegExPatternReader Reader, CharClass cc)
    {
        int beforePos = Reader.Position;
        if ((Options & RegExParserOptions.PERL_X) == 0
            || !Reader.HasMore || Reader.Pop() != '\\'
            || // consume '\\'
            !Reader.HasMore)
            return false;
        Reader.Pop(); // e.g. advance past 'd' in "\\d"
        var k = Reader.From(beforePos);
        if (!CharGroup.PERL_GROUPS.TryGetValue(k, out var g))
            return false;
        if (g == null)
            return false;
        cc.AppendGroup(g, (Options & RegExParserOptions.FOLD_CASE) != 0);
        return true;
    }

    // parseNamedClass parses a leading POSIX named character class like
    // [:alnum:] from the beginning of t.  If one is present, it appends the
    // characters to cc, advances the iterator, and returns true.
    // Pre: t at "[:".  Post: t after ":]".
    // On failure (no class of than name), throws PatternSyntaxException.
    // On misparse, returns false; t.pos() is undefined.
    private bool ParseNamedClass(RegExPatternReader Reader, CharClass cc)
    {
        // (Go precondition check deleted.)
        var cls = Reader.Rest;
        int i = cls.IndexOf(":]");
        if (i < 0)
            return false;
        var name = cls.Substring(0, i + 2 - 0); // "[:alnum:]"
        Reader.SkipString(name);
        if (!CharGroup.POSIX_GROUPS.TryGetValue(name, out var g))
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, name);
        cc.AppendGroup(g, (Options & RegExParserOptions.FOLD_CASE) != 0);
        return true;
    }

    protected void ParseBackslash(RegExPatternReader Reader)
    {
        {
            int savedPos = Reader.Position;
            Reader.Skip(1); // '\\'
            if ((this.Options & RegExParserOptions.PERL_X) != 0 && Reader.HasMore)
            {
                int c = Reader.Pop();
                switch ((char)c)
                {
                    case 'A':
                        this.Push(new RegExNode(RegExTokenType.BeginText, Options: Options));
                        goto outswitch;
                    case 'b':
                        this.Push(new RegExNode(RegExTokenType.WordBoundary, Options: Options));
                        goto outswitch;
                    case 'B':
                        this.Push(new RegExNode(RegExTokenType.NotWordBoundary, Options: Options));
                        goto outswitch;
                    case 'C':
                        //NOTICE:use any char instead
                        //Op(Regexp.Op.ANY_CHAR_NOT_NL);
                        //goto outswitch;
                        // any byte; not supported
                        throw new PatternSyntaxException(ERR_INVALID_ESCAPE, "\\C");
                    case 'Q':
                        {
                            // \Q ... \E: the ... is always literals
                            var lit = Reader.Rest;
                            int i = lit.IndexOf("\\E");
                            if (i >= 0)
                                lit = lit[..i];
                            Reader.SkipString(lit);
                            Reader.SkipString("\\E");
                            for (int j = 0; j < lit.Length;)
                            {
                                int codepoint = char.ConvertToUtf32(lit, j);
                                Push(new RegExNode(RegExTokenType.Literal, Options: Options, Runes: new int[] { codepoint }));
                                j += new Rune(codepoint).Utf16SequenceLength;
                            }
                            goto outswitch;
                        }
                    case 'z':
                        this.Push(new RegExNode(RegExTokenType.EndText, Options: Options));
                        goto outswitch;
                    default:
                        Reader.RewindTo(savedPos);
                        break;
                }
            }


            // Look for Unicode character group like \p{Han}
            if (Reader.LookingAt("\\p") || Reader.LookingAt("\\P"))
            {
                var cc2 = new CharClass();
                if (ParseUnicodeClass(Reader, cc2))
                {
                    var re = new RegExNode(RegExTokenType.CharClass, Options: Options, Runes: cc2.ToArray());
                    Push(re);
                    goto outswitch;
                }
            }

            // Perl character class escape.
            var cc = new CharClass();
            if (ParsePerlClassEscape(Reader, cc))
            {
                var re = new RegExNode(RegExTokenType.CharClass, Options: Options, Runes: cc.ToArray());
                Push(re);
                goto outswitch;
            }

            Reader.RewindTo(savedPos);
            //Reuse(re);

            // Ordinary single-character escape.
            this.Push(new RegExNode(RegExTokenType.Literal, Runes: new int[] { ParseEscape(Reader) }));
        }
    outswitch:
        ;
    }
}