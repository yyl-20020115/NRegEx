using System.Text;
using System.Xml.Linq;

namespace NRegEx;

public class RegExDomParser
{
    public const int MatchGroupIndex = 0;

    // Unexpected error
    private const string ERR_INTERNAL_ERROR = "regexp/syntax: internal error";
    // Parse errors
    private const string ERR_INVALID_CHAR_CLASS = "invalid character class";
    private const string ERR_INVALID_CHAR_RANGE = "invalid character class range";
    private const string ERR_INVALID_ESCAPE = "invalid escape sequence";
    private const string ERR_INVALID_NAMED_CAPTURE = "invalid named capture";
    private const string ERR_INVALID_PERL_OP = "invalid or unsupported syntax";
    private const string ERR_INVALID_REPEAT_OP = "invalid nested repetition operator";
    private const string ERR_INVALID_REPEAT_SIZE = "invalid repeat count";
    private const string ERR_MISSING_BRACKET = "missing closing ]";
    private const string ERR_MISSING_PAREN = "missing closing )";
    private const string ERR_MISSING_REPEAT_ARGUMENT = "missing argument to repetition operator";
    private const string ERR_TRAILING_BACKSLASH = "trailing backslash at end of expression";
    private const string ERR_DUPLICATE_NAMED_CAPTURE = "duplicate capture group name";
    public readonly string Name;
    public readonly RegExPatternReader Reader;
    public Options Options;
    public readonly Dictionary<string, int> NamedGroups = new();
    public string Pattern => this.Reader.Pattern;
    protected readonly Stack<RegExNode> NodeStack = new();
    protected readonly Stack<RegExNode> NestStack = new ();

    protected int CaptureIndex = MatchGroupIndex;
    public RegExDomParser(string name, string pattern, Options options)
    {
        this.Name = name;
        this.Reader = new RegExPatternReader(
            pattern ?? throw new ArgumentNullException(nameof(pattern)));
        this.Options = options;
    }
    /// <summary>
    /// Lazy/Possessive/Normal support
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static BehaviourOptions ParseTokenOptions(int c) => c switch
    {
        '?' => BehaviourOptions.Lazy,
        '+' => BehaviourOptions.Possessive,
        _ => BehaviourOptions.Greedy,
    };
    public RegExNode Parse()
    {
        int c;
        char d;
        while (-1 != (c = this.Reader.Peek()))
        {
            d = ((char)c);
            int Position = this.Reader.Position;
            switch (c)
            {
                default:
                    {
                        var taken = this.Reader.Take();
                        this.Push(new(
                            TokenTypes.Literal, taken, Position: Position, 
                            Length: taken.Length, PatternName:this.Name));
                    }
                    continue;
                case RegExTextReader.EOF:
                    this.Push(new(PatternName: this.Name));
                    continue;
                case '\\':
                    this.ParseBackslash(Reader);
                    continue;
                case '&':
                    this.Push(new(
                        TokenTypes.Concate, this.Reader.Take(), Position: Position, PatternName: this.Name));
                    continue;
                case '|':
                    this.Push(new(TokenTypes.Alternate, this.Reader.Take(), Position: Position, PatternName: this.Name));
                    continue;
                case '^':
                    this.Push(new(
                        ((this.Options & Options.ONE_LINE) != Options.None)
                        ? TokenTypes.BeginLine
                        : TokenTypes.BeginText
                        , this.Reader.Take(), Position: Position, PatternName: this.Name));
                    break;
                case '$':
                    this.Push(new(
                        ((this.Options & Options.ONE_LINE) != Options.None)
                        ? TokenTypes.EndLine
                        : TokenTypes.EndText
                        , this.Reader.Take(), Position: Position, PatternName: this.Name));
                    continue;
                case '.':
                    this.Push(new(
                        ((this.Options & Options.DOT_NL) != Options.None)
                        ? TokenTypes.AnyCharExcludingNewLine
                        : TokenTypes.AnyCharExcludingNewLine
                        , this.Reader.Take(), Position: Position, PatternName: this.Name));
                    continue;
                case '+':
                    {
                        var text = this.Reader.Take();
                        var tops = ParseTokenOptions(this.Reader.Peek());
                        if (tops != BehaviourOptions.Greedy) this.Reader.Skip();
                        this.Push(new(TokenTypes.OnePlus,
                            text, "", 1, -1, BehaviourOptions: tops, Position: Position, PatternName: this.Name)
                        { Children = new() { this.NodeStack.Pop() } });
                    }
                    continue;
                case '*':
                    {
                        var text = this.Reader.Take();
                        var tops = ParseTokenOptions(this.Reader.Peek());
                        if (tops != BehaviourOptions.Greedy) this.Reader.Skip();
                        this.Push(new(TokenTypes.ZeroPlus,
                            text, "", 0, -1, BehaviourOptions: tops, Position: Position, PatternName: this.Name)
                        { Children = new() { this.NodeStack.Pop() } });
                    }
                    continue;
                case '?':
                    {
                        var text = this.Reader.Take();
                        var tops = ParseTokenOptions(this.Reader.Peek());
                        if (tops != BehaviourOptions.Greedy) this.Reader.Skip();
                        this.Push(new(TokenTypes.ZeroOne,
                            text, "", 0, 1, BehaviourOptions: tops, Position: Position, PatternName: this.Name)
                        { Children = new() { this.NodeStack.Pop() } });
                    }
                    continue;
                case '(':
                    {
                        if (((this.Options & Options.PERL) != Options.None) &&
                            (this.Reader.LookingAt("(?"))
                            ||this.NestPeek()?.GroupType == GroupType.BackReferenceConditionGroup)
                            this.ParsePerlFlags(Reader);
                        else
                        {
                            this.Push(new(TokenTypes.OpenParenthesis, Reader.Take(),
                                CaptureIndex: ++CaptureIndex, GroupType: GroupType.NormalGroup, Position: Position, PatternName: this.Name));
                        }
                    }
                    continue;
                case ')':
                    {
                        this.ProcessCloseParenthesis();
                    }
                    continue;
                case '{':
                    {
                        this.Reader.Enter();
                        var (found, min, max, text) = this.ParseRepeat(this.Reader);
                        if (!found)
                        {
                            this.Reader.Leave();
                            var taken = this.Reader.Take();
                            this.Push(new(TokenTypes.Literal, taken, Position: Position,Length:taken.Length, PatternName: this.Name));
                        }
                        else
                        {
                            this.Reader.Discard();
                            this.Push(new(TokenTypes.Repeats,
                                text ?? "", "", min, max, Position: Position, PatternName: this.Name)
                            { Children = new() { this.NodeStack.Pop() } });
                        }
                    }
                    continue;
                case '[':
                    this.ParseClass(Reader);
                    continue;
            }
        }

        if (this.NestCount != 0)
            throw new RegExSyntaxException(
                ERR_INTERNAL_ERROR, this.Pattern);

        this.ProcessStack();

        if (this.NestCount != 0 || this.NodeStack.Count != 1)
            throw new RegExSyntaxException(
                ERR_MISSING_PAREN, this.Pattern);

        return this.NodeStack.Pop();
    }
    protected void ProcessCloseParenthesis()
    {
        this.Reader.Skip();
        this.ProcessStack();
        var result = this.Pop();
        var open = this.Pop();
        if (open.Type != TokenTypes.OpenParenthesis)
            throw new RegExSyntaxException(
                ERR_MISSING_PAREN, this.Pattern);

        if ((this.Options & Options.NO_CAPTURE) == 0)
        {
            result = new RegExNode(TokenTypes.Capture, GroupType: open.GroupType, PatternName: this.Name, CaptureIndex: open.CaptureIndex)
            {
                Children = new() { result }
            };
        }

        this.Push(result);
    }
    protected void ProcessStack()
    {
        var nodes = new List<RegExNode>();
        var nlist = new List<List<RegExNode>>
        {
            nodes
        };
        while (this.StackDepth > 0
            && this.Peek().Type < TokenTypes.OpenParenthesis)
        {
            var top = this.Pop();
            switch (top.Type)
            {
                case TokenTypes.Concate:
                    continue;
                case TokenTypes.Alternate:
                    nlist.Add(nodes = new());
                    continue;
                default:
                    nodes.Add(top);
                    continue;
            }
        }
        var alts = new RegExNode(TokenTypes.Union, PatternName: this.Name);
        foreach (var ds in nlist)
        {
            ds.Reverse();
            alts.Children.Add(new(
                TokenTypes.Sequence, PatternName: this.Name) { Children = ds });
        }
        this.Push(alts);
    }

    protected int NestCount 
        => this.NestStack.Count;
    protected RegExNode Push(RegExNode node)
    {
        var nest = node.Type == TokenTypes.OpenParenthesis;
        if (nest)
        {
            NestStack.Push(node);
        }
        this.NodeStack.Push(node);
        return node;
    }

    protected RegExNode Pop()
    { 
        var node = this.NodeStack.Pop();
        if (this.NestStack.Count>0 && node == this.NestStack.Peek())
        {
            this.NestStack.Pop();
        }
        return node;
    }
    protected RegExNode NestPop()
    {
        var node = this.NestStack.Pop();
        return node;
    }
    protected RegExNode? NestPeek() 
        => NestCount > 0 ? this.NestStack.Peek() : null;
    protected RegExNode Peek()
        => this.NodeStack.Peek();

    protected int StackDepth
        => this.NodeStack.Count;
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
        if (n.Length > 8)
        {
            return -2; // overflow
        }
        return int.TryParse(n, out var r) ? r : -1;
    }

    protected (bool, int?, int?, string?) ParseRepeat(RegExPatternReader Reader)
    {
        if (this.Reader.Peek() == -1 || !Reader.LookingAt("{")) goto failed;
        var p = Reader.Position;
        Reader.Skip();
        var s = Reader.Pattern;
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
            if (Reader.LookingAt('}')) { max = -1; goto exit; }
            else if ((max = ParseInt(Reader)) == -1) goto failed;
        }
        if (!Reader.HasMore || !Reader.LookingAt('}'))
            goto failed;
        exit:
        Reader.Skip(1); // '}'
        if (min < 0 || min > 1000 || max == -2 || max > 1000 || (max >= 0 && min > max))
            throw new RegExSyntaxException(ERR_INVALID_REPEAT_SIZE, Reader.From(start));
        return (true, min, max, s[p..Reader.Position]); // success
    failed:
        return (false, null, null, null);
    }

    // isValidCaptureName reports whether name
    // is a valid capture name: [A-Za-z0-9_]+.
    // PCRE limits names to 32 bytes.
    // Python rejects names starting with digits.
    // We don't enforce either of those.
    public static bool IsValidCaptureNameChar(int c) => (c == '_' || Utils.Isalnum(c));
    public static bool IsValidCaptureName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        for (int i = 0; i < name.Length; ++i)
        {
            if (!IsValidCaptureNameChar(name[i]))
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
        if (s.StartsWith("(?P<") || (s.StartsWith("(?<")&&!s.StartsWith("(?<="))&&!s.StartsWith("(?<!") || s.StartsWith("(?'"))
        {
            var delta = (s.StartsWith("(?<") || s.StartsWith("(?'")) ? -1 : 0;
            // Pull out name.
            var end = s.IndexOf('>');
            if (end < 0)
                throw new PatternSyntaxException(ERR_INVALID_NAMED_CAPTURE, s);
            var name = s[(4 + delta)..end]; // "name"
            Reader.SkipString(name);
            Reader.Skip(5+delta); // "(?P<>"
            if (!IsValidCaptureName(name))
            {
                throw new PatternSyntaxException(
                    ERR_INVALID_NAMED_CAPTURE, s[..end]); // "(?P<name>"
            }
            var node = new RegExNode(TokenTypes.OpenParenthesis,
                Value: name, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.NormalGroup, Position: startPos, PatternName: this.Name);
            // Like ordinary capture, but named.
            if (NamedGroups.ContainsKey(name))
            {
                throw new PatternSyntaxException(ERR_DUPLICATE_NAMED_CAPTURE, name);
            }
            NamedGroups.Add(name, this.CaptureIndex);

            this.Push(node);

            return;
        }

        //if parent is condition
        var n = this.NestPeek();
        if (n is not null)
        {
            int c = 0;
            switch (n.GroupType)
            {
                case GroupType.BackReferenceConditionGroup:
                    {
                        Reader.Skip(1);
                        //this should be a number or a name
                        var lb = new StringBuilder();
                        while (this.Reader.HasMore)
                        {
                            c = this.Reader.Pop();
                            if (c == ')')
                                break;
                            else if (IsValidCaptureNameChar(c))
                            {
                                lb.Append((char)c);
                            }
                            else
                            {
                                throw new Exception("invalid group index");
                            }
                        }
                        var text = lb.ToString();
                        if (int.TryParse(text, out int index))
                        {
                            //index
                            this.Push(new(TokenTypes.BackReference,
                                Position: startPos, GroupType: GroupType.BackReferenceCondition, PatternName: this.Name));
                            return;
                        }
                        else if (IsValidCaptureName(text))
                        {
                            if(!this.NamedGroups.TryGetValue(text, out index))
                            {
                                index = -1;
                            }
                            //name
                            this.Push(new(TokenTypes.BackReference, CaptureIndex: index, Position: startPos, GroupType: GroupType.BackReferenceCondition, PatternName: this.Name));
                            return;
                        }
                        throw new Exception($"invalid group name:{lb}");
                    }
                case GroupType.LookAroundConditionGroup:
                    //fallthrough
                    break;
                default:
                    break;
            }
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
                    goto throws_exception;
                // Flags.
                case 'i':
                    flags |= Options.CASE_INSENSITIVE;
                    sawFlag = true;
                    break;
                case 'm':
                    flags &= ~Options.ONE_LINE;
                    sawFlag = true;
                    break;
                case 'n':
                    flags |= Options.NO_CAPTURE;
                    sawFlag = true;
                    break;
                case 's':
                    flags |= Options.DOT_NL;
                    sawFlag = true;
                    break;
                case 'U':
                    flags |= Options.NON_GREEDY;
                    sawFlag = true;
                    break;
                case 'x':
                    flags |= Options.SHARP_LINE_COMMENT;
                    sawFlag = true;
                    break;
                case '#': //#comments
                    { //skip comments
                        while (this.Reader.HasMore)
                        {
                            c = this.Reader.Pop();
                            if ((flags & Options.SHARP_LINE_COMMENT) == Options.SHARP_LINE_COMMENT)
                            {
                                if (c is '\n' or ')') return;
                            }
                            else
                            {
                                if (c == ')') return;
                            }
                        }
                    }
                    break;
                // Switch to negation.
                case '-':
                    if (sign < 0) goto throws_exception;
                    sign = -1;
                    // Invert flags so that | above turn into &~ and vice versa.
                    // We'll invert flags again before using it below.
                    flags = ~flags;
                    sawFlag = false;
                    break;

                // End of flags, starting group or not.
                //case ':':
                case ')':
                    if (sign < 0)
                    {
                        if (!sawFlag)
                        {
                            goto throws_exception;
                        }
                        flags = ~flags;
                    }
                    if (c == ':')
                    {
                        // Open new group
                        this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.NormalGroup, PatternName: this.Name));
                    }
                    this.Options = flags;
                    return;
                //NOTICE:conditions 
                //(?(number)
                //(?(name)

                //(?(?=
                //(?(?!
                //(?(?<=
                //(?(?<!
                //  ^
                case '(':
                    if (this.Reader.Peek() == '?')
                    {
                        //started a condition group
                        this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, GroupType: GroupType.LookAroundConditionGroup, PatternName: this.Name));
                        //retry this condition
                        this.Reader.Skip(-1);
                        return;
                    }
                    else if (Unicode.IsRuneLetterOrDigit(this.Reader.Peek()))
                    {
                        //started a condition group
                        this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, GroupType: GroupType.BackReferenceConditionGroup, PatternName: this.Name));
                        //retry this condition
                        this.Reader.Skip(-1);
                        return;
                    }
                    goto throws_exception;
                //atomic group
                case '>':
                    this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.AtomicGroup, PatternName: this.Name));
                    return; 
                //non captive
                case ':':
                    //NOT CAPTIVE
                    this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.NotCaptiveGroup, PatternName: this.Name));
                    return;
                //forward inspection
                //Windows(?=95|98|NT|2000) - 可以匹配 Windows2000 中的 Windows , 但是不能匹配 Windows10 中的 Windows
                case '=':
                    this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.ForwardPositiveGroup, PatternName: this.Name));
                    return;
                //Window(?!95|98|NT|2000) - 可以匹配 Windows10 中的 Windows , 但是不能匹配 Windows2000 中的 Windows
                case '!':
                    this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.ForwardNegativeGroup, PatternName: this.Name));
                    return;
                //backward inspection
                case '<':
                    {
                        if (Reader.HasMore)
                        {
                            switch(this.Reader.Peek())
                            {
                                //(?<=95|98|NT|2000)Windows - 可以匹配 2000Windows 中的 Windows , 但是不能匹配 10Windows 中的 Windows
                                case '=':
                                    this.Reader.Pop();
                                    this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.BackwardPositiveGroup, PatternName: this.Name));
                                    return;
                                //(?<!95|98|NT|2000)Windows - 可以匹配 10Windows中的 Windows , 但是不能匹配 2000Windows 中的 Windows
                                case '!':
                                    this.Reader.Pop();
                                    this.Push(new(TokenTypes.OpenParenthesis, Position: startPos, CaptureIndex: ++this.CaptureIndex, GroupType: GroupType.BackwardPositiveGroup, PatternName: this.Name));
                                    return;
                            }
                        }
                    }
                    return;
            }
        }
    throws_exception:
        throw new PatternSyntaxException(ERR_INVALID_PERL_OP, Reader.From(startPos));
    }
    // parseUnicodeClass() parses a leading Unicode character class like \p{Han}
    // from the beginning of t.  If one is present, it appends the characters to
    // to |cc|, advances |t| and returns true.
    //
    // Returns false if such a pattern is not present or UNICODE_GROUPS
    // flag is not enabled; |t.pos()| is not advanced in this case.
    // Indicates error by throwing PatternSyntaxException.
    private bool ParseUnicodeClass(RegExPatternReader Reader, List<RuneClass> list)
    {
        int startPos = Reader.Position;
        if ((Options & Options.UNICODE_GROUPS) == 0
            || (!Reader.LookingAt("\\p") && !Reader.LookingAt("\\P")))
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
            name = rest[..end]; // e.g. "Han"
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
            name = name[1..];
        }

        var pair = UnicodeTable(name)
            ?? throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.From(startPos));
        var tab = pair.first;
        var fold = pair.second; // fold-equivalent table

        RuneClass.FromTable(tab, sign < 0, list, startPos);
        if ((Options & Options.CASE_INSENSITIVE) == Options.CASE_INSENSITIVE)
        {
            RuneClass.FromTable(fold, sign < 0, list, startPos);
        }
        return true;
    }
    // RangeTables are represented as int[][], a list of triples (start, end,
    // stride).
    private static readonly int[][] ANY_TABLE = new int[][]{
        new int[]{0, Unicode.MAX_RUNE, 1},
    };
    public class Pair<F, S>
    {
        public static Pair<F, S> Of(F first, S second) => new(first, second);

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
            return Pair<int[][], int[][]>.Of(ANY_TABLE, ANY_TABLE);
        if (UnicodeTables.CATEGORIES.TryGetValue(name, out var table))
            if (UnicodeTables.FOLD_CATEGORIES.TryGetValue(name, out var cat))
                return Pair<int[][], int[][]>.Of(table, cat);
        if (UnicodeTables.SCRIPTS.TryGetValue(name, out table))
            if (UnicodeTables.FOLD_SCRIPT.TryGetValue(name, out var script))
                return Pair<int[][], int[][]>.Of(table, script);
        return null;
    }

    protected void ParseClass(RegExPatternReader Reader)
    {
        int startPos = Reader.Position;
        Reader.Skip(1); // '['

        var list = new List<RuneClass>();

        int sign = +1;
        if (Reader.HasMore && Reader.LookingAt('^'))
        {
            sign = -1;
            Reader.Skip(1); // '^'

            // If character class does not match \n, add it here,
            // so that negation later will do the right thing.
            if ((this.Options & Options.CLASS_NL) == 0)
                list.Add(new(sign < 0, '\n') { Position = startPos });
        }

        bool first = true; // ']' and '-' are okay as first char in class
        while (!Reader.HasMore || Reader.Peek() != ']' || first)
        {
            // POSIX: - is only okay unescaped as first or last in class.
            // Perl: - is okay anywhere.
            if (Reader.HasMore && Reader.LookingAt('-') && (Options & Options.PERL_X) == 0 && !first)
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
                if (ParseNamedClass(Reader, list))
                    continue;
                Reader.RewindTo(beforePos);
            }

            // Look for Unicode character group like \p{Han}.
            if (ParseUnicodeClass(Reader, list))
                continue;

            // Look for Perl character class symbols (extension).
            if (ParsePerlClassEscape(Reader, list))
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
                    if (hi < lo) (hi, lo) = (lo, hi);
                    //    throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, Reader.From(beforePos));
                }
            }
            list.Add(new(lo, hi, sign < 0) { Position = beforePos });
        }
        Reader.Skip(1); // ']'
        this.PushRuneClassList(list, startPos);
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
                // Single non-zero digit is a backreference; not supported
                //if (!Reader.HasMore || Reader.Peek() < '0' || Reader.Peek() > '7')
                //    break;
                //goto for_zero;
            /* fallthrough */
            case '0':
            //for_zero:
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
                {
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
                        while(true)
                        {
                            if (!Reader.HasMore)
                                goto outswitch;
                            c = Reader.Pop();
                            if (c == '}')
                                break;
                            int v = Utils.Unhex(c);
                            if (v < 0)
                                goto outswitch;
                            r = r << 4 | v;
                            if (r > Unicode.MAX_RUNE)
                                goto outswitch;
                            nhex++;
                        }
                        if (nhex == 0)
                            goto outswitch;
                        return r;
                    }

                    // Easy case: two hex digits.
                    int h1 = Utils.Unhex(c);
                    if (!Reader.HasMore)
                        break;
                    c = Reader.Pop();
                    int h0 = Utils.Unhex(c);
                    if (h1 < 0 || h0 < 0)
                        break;
                    return h1 << 4 | h0;
                }
            case 'u':
                {
                    if (!Reader.HasMore)
                        break;
                    int h3 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h2 = Utils.Unhex(Reader.Pop());

                    if (!Reader.HasMore)
                        break;
                    int h1 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h0 = Utils.Unhex(Reader.Pop());
                    if (h3 < 0 || h2 < 0 || h1 < 0 || h0 < 0)
                        break;
                    return h3 << 12 | h2 << 8 | h1 << 4 | h0;
                }
            case 'U':
                {
                    if (!Reader.HasMore)
                        break;
                    int h7 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h6 = Utils.Unhex(Reader.Pop());

                    if (!Reader.HasMore)
                        break;
                    int h5 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h4 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h3 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h2 = Utils.Unhex(Reader.Pop());

                    if (!Reader.HasMore)
                        break;
                    int h1 = Utils.Unhex(Reader.Pop());
                    if (!Reader.HasMore)
                        break;
                    int h0 = Utils.Unhex(Reader.Pop());
                    if (h7<0 | h6<0 | h5<0 | h4<0 |h3 < 0 || h2 < 0 || h1 < 0 || h0 < 0)
                        break;
                    return h7<<28 | h6 <<24 | h5<<20 | h4<<16 | h3 << 12 | h2 << 8 | h1 << 4 | h0;
                }
            // C escapes.  There is no case 'b', to avoid misparsing
            // the Perl word-boundary \b as the C backspace \b
            // when in POSIX mode.  In Perl, /\b/ means word-boundary
            // but /[\b]/ means backspace.  We don't support that.
            // If you want a backspace, embed a literal backspace
            // character or use \x08.
            case 'a':
                return '\a'; // No \a in Java
            case 'b':
                return '\b';
            case 't':
                return '\t';
            case 'r':
                return '\r';
            case 'v':
                return '\u000b'; // No \v in Java
            case 'f':
                return '\f';
            case 'n':
                return '\n';
            case 'e':
                return '\u001b';
        }
    outswitch:
        throw new PatternSyntaxException(ERR_INVALID_ESCAPE, Reader.From(startPos));
    }

    // parsePerlClassEscape parses a leading Perl character class escape like \d
    // from the beginning of |t|.  If one is present, it appends the characters
    // to cc and returns true.  The iterator is advanced past the escape
    // on success, undefined on failure, in which case false is returned.
    private bool ParsePerlClassEscape(RegExPatternReader Reader, List<RuneClass> list)
    {
        int beforePos = Reader.Position;
        if ((Options & Options.PERL_X) == 0
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

        list.Add(new(g.Sign < 0, TranslateCharClass(g.Class)) { Position = beforePos });

        return true;
    }
    protected static int[] TranslateCharClass(int[] ranges)
    {
        if (ranges.Length % 2 == 0)
        {
            var set = new HashSet<int>();
            for(int i = 0;i < ranges.Length; i += 2)
            {
                int lo = ranges[i + 0];
                int hi = ranges[i + 1];
                set.UnionWith(Enumerable.Range(lo, hi - lo + 1));
            }
            return set.ToArray();
        }
        return Array.Empty<int>();
    }

    // parseNamedClass parses a leading POSIX named character class like
    // [:alnum:] from the beginning of t.  If one is present, it appends the
    // characters to cc, advances the iterator, and returns true.
    // Pre: t at "[:".  Post: t after ":]".
    // On failure (no class of than name), throws PatternSyntaxException.
    // On misparse, returns false; t.pos() is undefined.
    private bool ParseNamedClass(RegExPatternReader Reader, List<RuneClass> list)
    {
        // (Go precondition check deleted.)
        var savedPos = Reader.Position;
        var cls = Reader.Rest;
        int i = cls.IndexOf(":]");
        if (i < 0)
            return false;
        var name = cls[..(i + 2)]; // "[:alnum:]"
        Reader.SkipString(name);
        if (!CharGroup.POSIX_GROUPS.TryGetValue(name, out var g))
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, name);
        list.Add(new(g.Sign < 0,TranslateCharClass(g.Class)) { Position = savedPos });
        return true;
    }

    protected void ParseBackslash(RegExPatternReader Reader)
    {
        int savedPos = Reader.Position;
        Reader.Skip(1); // '\\'
        if ((this.Options & Options.PERL_X) != 0 && Reader.HasMore)
        {
            int c = Reader.Pop();
            switch ((char)c)
            {
                //BackReference
                case 'k':
                    {
                        var s = Reader.Rest;
                        if (s.StartsWith("<"))
                        {
                            // Pull out name.
                            var end = s.IndexOf('>');
                            if (end < 0)
                                throw new PatternSyntaxException(ERR_INVALID_NAMED_CAPTURE, s);
                            var name = s[1..end]; // "name"
                            Reader.SkipString(name);
                            Reader.Skip(2); // "<>"
                            if (!IsValidCaptureName(name))
                            {
                                throw new PatternSyntaxException(
                                    ERR_INVALID_NAMED_CAPTURE, s[..end]); // "k<name>"
                            }
                            // Like ordinary capture, but named.
                            if (!this.NamedGroups.TryGetValue(name, out var index))
                                throw new RegExSyntaxException($"Unable to find capture index for name:{name}");

                            this.Push(new(TokenTypes.BackReference,
                                "\\" + (char)c, Options: Options, Position: savedPos, PatternName: this.Name,
                                CaptureIndex: index));
                        }
                    }
                    goto outswitch;
                //BackReference
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    {
                        int index = c - '0';

                        this.Push(new(TokenTypes.BackReference, 
                            "\\"+(char)c, Options: Options, Position: savedPos, PatternName: this.Name,
                            CaptureIndex: index));
                    }
                    goto outswitch;
                case 'A':
                    this.Push(new(TokenTypes.BeginText, "\\A", Options: Options, Position: savedPos, PatternName: this.Name));
                    goto outswitch;
                case 'b':
                    this.Push(new(TokenTypes.WordBoundary, "\\b", Options: Options, Position: savedPos, PatternName: this.Name));
                    goto outswitch;
                case 'B':
                    this.Push(new(TokenTypes.NotWordBoundary, "\\B", Options: Options, Position: savedPos, PatternName: this.Name));
                    goto outswitch;
                case 'C':
                    //NOTICE:use any char instead
                    //Op(Regexp.Op.ANY_CHAR_NOT_NL);
                    //goto outswitch;
                    // any byte; not supported
                    throw new PatternSyntaxException(ERR_INVALID_ESCAPE, "\\C");
                case 'c':
                    if(!Reader.HasMore) 
                        goto outswitch;
                    else
                    {
                        int r = this.Reader.Peek();
                        if (r is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
                        {
                            this.Reader.Pop();
                            int n = (char.ToLower((char)r) - 'a') + 1;
                            var t = char.ConvertFromUtf32(n);
                            this.Push(new(TokenTypes.Literal,t , Options: Options, Position: savedPos,Length:t.Length, PatternName: this.Name));
                        }
                        else
                        {
                            this.Push(new(TokenTypes.Literal, "\\c", Options: Options, Position: savedPos, Length:1, PatternName: this.Name));
                        }
                    }
                    goto outswitch;
                case 'Q':
                    {
                        // \Q ... \E: the ... is always literals
                        var lit = Reader.Rest;
                        int i = lit.IndexOf("\\E");
                        if (i >= 0)
                            lit = lit[..i];
                        Reader.SkipString(lit);
                        Reader.SkipString("\\E");
                        var cps = new List<int>();
                        for (int j = 0; j < lit.Length;)
                        {
                            int codepoint = char.ConvertToUtf32(lit, j);
                            cps.Add(codepoint);
                            j += new Rune(codepoint).Utf16SequenceLength;
                        }
                        var taken = Utils.RunesToString(cps);
                        this.Push(new(TokenTypes.Literal,taken, Options: Options,Position:savedPos,Length:taken.Length, PatternName: this.Name));
                        goto outswitch;
                    }
                case 'Z':
                    this.Push(new(TokenTypes.EndLine, "\\" + (char)c, Options: Options, Position: savedPos, PatternName: this.Name));
                    goto outswitch;
                case 'z':
                    this.Push(new(TokenTypes.EndText, "\\"+(char)c, Options: Options, Position: savedPos, PatternName: this.Name));
                    goto outswitch;
                default:
                    Reader.RewindTo(savedPos);
                    break;
            }

            // Look for Unicode character group like \p{Han}
            if (Reader.LookingAt("\\p") || Reader.LookingAt("\\P"))
            {
                var list = new List<RuneClass>();
                if (ParseUnicodeClass(Reader, list))
                {
                    this.PushRuneClassList(list, savedPos);
                    goto outswitch;
                }
            }
            {
                // Perl character class escape.
                var list = new List<RuneClass>();
                if (ParsePerlClassEscape(Reader, list))
                {
                    this.PushRuneClassList(list, savedPos);
                    goto outswitch;
                }
            }
            Reader.RewindTo(savedPos);
            //Reuse(re);

            // Ordinary single-character escape.
            {
                var escaped = ParseEscape(Reader);
                var taken = char.ConvertFromUtf32(escaped);
                this.Push(new(TokenTypes.Literal, taken, Runes: new int[] { escaped }, Position: savedPos, Length: taken.Length, PatternName: this.Name));
            }
        }
    outswitch:
        ;
    }

    protected void PushRuneClassList(List<RuneClass> list, int savedPos)
    {
        var alt = new RegExNode(TokenTypes.Union, Position: savedPos, PatternName: this.Name);
        this.AddNodeChildren(alt.Children, list.Where(cc => cc.Inverted).ToList(), true, savedPos);
        this.AddNodeChildren(alt.Children, list.Where(cc => !cc.Inverted).ToList(), false, savedPos);

        this.Push(alt);
    }
    protected void AddNodeChildren(List<RegExNode> children, List<RuneClass> list, bool inverted, int position)
    {
        var runes = new HashSet<int>();
        foreach (var cc in list)
        {
            runes.UnionWith(cc.Runes);
        }
        if (runes.Count > 0)
        {
            children.Add(new(TokenTypes.Sequence, PatternName: this.Name)
            {
                Children = new() {new(TokenTypes.RuneClass, Options: Options,
                    Runes: runes.ToArray(), Inverted: inverted, Position: position, PatternName:this.Name)}
            });
        }
    }
}