using System.Text;

namespace NRegEx;

// Parser flags.
[Flags]
public enum ParserOptions : uint
{
    None = 0,
    // Fold case during matching (case-insensitive).
    FOLD_CASE = 0x01,
    // Treat pattern as a literal string instead of a regexp.
    LITERAL = 0x02,
    // Allow character classes like [^a-z] and [[:space:]] to match newline.
    CLASS_NL = 0x04,
    // Allow '.' to match newline.
    DOT_NL = 0x08,
    // Treat ^ and $ as only matching at beginning and end of text, not
    // around embedded newlines.  (Perl's default).
    ONE_LINE = 0x10,
    // Make repetition operators default to non-greedy.
    NON_GREEDY = 0x20,
    // allow Perl extensions:
    //   non-capturing parens - (?: )
    //   non-greedy operators - *? +? ?? {}?
    //   flag edits - (?i) (?-i) (?i: )
    //     i - FoldCase
    //     m - !OneLine
    //     s - DotNL
    //     U - NonGreedy
    //   line ends: \A \z
    //   \Q and \E to disable/enable metacharacters
    //   (?P<name>expr) for named captures
    // \C (any byte) is not supported.
    PERL_X = 0x40,
    // Allow \p{Han}, \P{Han} for Unicode group and negation.
    UNICODE_GROUPS = 0x80,
    // Regexp END_TEXT was $, not \z.  Internal use only.
    WAS_DOLLAR = 0x100,
    MATCH_NL = CLASS_NL | DOT_NL,
    // As close to Perl as possible.
    PERL = CLASS_NL | ONE_LINE | PERL_X | UNICODE_GROUPS,
    // POSIX syntax.
    POSIX = 0,
}

public static class RegExParser
{

    public const char NullChar = char.MinValue;
    public static readonly string Operators = "*&|()" + NullChar;
    public static readonly int[][] Priorities = new int[][]{
            new []{ 1, 1, 1, -1, 1, 1 }, // *&|()#
            new []{ -1, 1, 1, -1, 1, 1 },
            new []{ -1, -1, 1, -1, 1, 1 },
            new []{ -1, -1, -1, -1, 0, 2 },
            new []{ 1, 1, 1, 1, 1, 1 },
            new []{ -1, -1, -1, -1, -1, -1 } };
    public static Graph Parse(string name, string regex, ParserOptions options = ParserOptions.None)
        => regex!=null ? RegExGraphBuilder.Build(RegExDomParser.Parse(name, regex, options)) : new();

    public static string Invert(string input)
    {
        var builder = new StringBuilder();
        var inputStack = new Stack<char>();
        foreach (var c in input)
        {
            if (c == '(')
            {
                inputStack.Push(c);
            }
            else if (c == ')')
            {
                while (inputStack.Count > 0 && inputStack.Peek() != '(')
                {
                    builder.Append(inputStack.Pop());
                }
                inputStack.Pop();

            }
            else if (IsNotOperator(c))
            {
                builder.Append(c);
            }
            else //operator
            {
                while (inputStack.Count > 0 && HigherPriority(inputStack.Peek(), c))
                {
                    builder.Append(inputStack.Pop());
                }
                inputStack.Push(c);
            }
        }
        return builder.ToString();
    }
    public static bool HigherPriority(char c1, char c2)
        => Operators.IndexOf(c1) <= Operators.IndexOf(c2);
    public static bool IsNotOperator(char c)
        => Operators.IndexOf(c) < 0;

    public static int GetPriority(char c1, char c2)
        => Priorities[Operators.IndexOf(c1)][Operators.IndexOf(c2)];

    public static string Prepare(string input_regex)
    {
        var builder = new StringBuilder();
        input_regex = input_regex.Replace(" ", "");
        for (int i = 0; i < input_regex.Length; i++)
            if (i == 0)
                builder.Append(input_regex[i]);
            else
                if (input_regex[i] == '|' || input_regex[i] == '*' || input_regex[i] == ')')
                builder.Append(input_regex[i]);
            else
                    if (i >= 1 && input_regex[i - 1] == '(' || input_regex[i - 1] == '|')
                builder.Append(input_regex[i]);
            else
                builder.Append("&" + input_regex[i]);
        return builder.ToString();
    }
}
