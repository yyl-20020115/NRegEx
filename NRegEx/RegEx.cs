using System.Text;

namespace NRegEx;

public record class Capture(int Index = 0,int Length = -1, string? Value = null);

public delegate string CaptureEvaluator(Capture capture);
public class Regex
{

    public const string MetaChars = "|()[]{}^$*+?\\ #";
    private static bool IsMetachar(char ch) => MetaChars.IndexOf(ch)>=0;

    public static string Escape(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            if (IsMetachar(input[i]))
            {
                return EscapeImpl(input, i);
            }
        }

        return input;
    }

    private static string EscapeImpl(string input, int i)
    {
        var vsb = new StringBuilder(input.Length*3);

        char ch = input[i];
        vsb.Append(input.AsSpan(0, i));

        do
        {
            vsb.Append('\\');
            switch (ch)
            {
                case '\n':
                    ch = 'n';
                    break;
                case '\r':
                    ch = 'r';
                    break;
                case '\t':
                    ch = 't';
                    break;
                case '\f':
                    ch = 'f';
                    break;
            }

            vsb.Append(ch);
            i++;
            int lastpos = i;

            while (i < input.Length)
            {
                ch = input[i];
                if (IsMetachar(ch))
                {
                    break;
                }

                i++;
            }

            vsb.Append(input.AsSpan(lastpos, i - lastpos));
        } while (i < input.Length);

        return vsb.ToString();
    }

    public static string Unescape(string input)
    {
        int i = input.IndexOf('\\');
        return i >= 0 ?
            UnescapeImpl(input, i) :
            input;
    }

    private static string UnescapeImpl(string input, int i)
    {
        var vsb = new StringBuilder(input.Length * 3);

        vsb.Append(input.AsSpan(0, i));
        do
        {
            i++;
            if (i == input.Length - 1)
            {
                vsb.Append(input[i]);
            }
            else //i<input.Length -1
            {
                char ch = input[i];
                if (ch == '\\')
                {
                    i++;
                    ch = input[i];
                    switch (ch)
                    {
                        case 'n':
                            ch = '\n';
                            break;
                        case 'r':
                            ch = '\r';
                            break;
                        case 't':
                            ch = '\t';
                            break;
                        case 'f':
                            ch = '\f';
                            break;
                        default:
                            if (IsMetachar(ch))
                            {
                                //ch is ok
                            }
                            else
                            {
                                i--;
                                //ch not changed
                            }
                            break;
                    }


                    vsb.Append(ch);
                } 
            }
 

            int lastpos = i;
            while (i < input.Length && input[i] != '\\')
            {
                i++;
            }

            vsb.Append(input.AsSpan(lastpos, i - lastpos));
        } while (i < input.Length);

        return vsb.ToString();
    }

    public static string[] Split(string input, string pattern, int count = 0, int start =0)
        => new Regex(pattern).Split(input, count, start);

    public static string Replace(string input, string pattern,string replacement, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, replacement, count, start);

    public static string Replace(string input, string pattern, CaptureEvaluator evaluator, int count = 0, int start = 0)
        => new Regex(pattern).Replace(input, evaluator, count, start);

    public static bool IsMatch(string input, string pattern, int start = 0, int length = -1) 
        => new Regex(pattern).IsMatch(input, start,length);
    public static Capture Match(string input, string pattern, int start = 0, int length = -1) 
        => new Regex(pattern).Match(input, start, length);
    public static List<Capture> Matches(string input, string pattern, int start = 0, int length = -1) 
        => new Regex(pattern).Matches(input, start, length);
    /// <summary>
    /// We should check easy back tracing regex first
    /// or we'll have to accept the early (not lazy or greedy) result for sure.
    /// 
    /// </summary>
    /// <param name="regex"></param>
    /// <returns></returns>
    public static bool IsBacktracingFriendly(string regex)
        => !string.IsNullOrEmpty(regex) 
        && new Regex(regex).Graph.IsBacktracingFriendly();
    
    public const char NullChar = '\0';
    public static string Operators = "*&|()" + NullChar;
    protected static int[][] Priorities = new int[][]{
            new []{ 1, 1, 1, -1, 1, 1 }, // *&|()#
            new []{ -1, 1, 1, -1, 1, 1 },
            new []{ -1, -1, 1, -1, 1, 1 },
            new []{ -1, -1, -1, -1, 0, 2 },
            new []{ 1, 1, 1, 1, 1, 1 },
            new []{ -1, -1, -1, -1, -1, -1 } };
    public static bool HigherPriority(char c1, char c2)
        => Operators.IndexOf(c1) <= Operators.IndexOf(c2);
    protected string regex = "";
    protected string name = "";
    public Graph Graph { get; protected set; } = new Graph();
    public string RegexText => regex;
    public string Name => name;
    public Regex() { }
    public Regex(string regex, string? name =null)
    {
        this.SetRegexText(regex,name);
    }

    /**
     * 核心转换代码
     * stack 记录 NFA 片段
     * 依次读取表达式的每个字符 ch
     * 如果 ch 是运算符，从 stack 出栈所需数目的 NFA 片段，构建新的 NFA 片段后入栈 stack
     * 如果 ch 是普通字符，创建新的状态，并构建只包含此状态的 NFA 片段入栈 stack
     * 返回 stack 栈顶的 NFA 片段，即最终结果
     */
    protected Graph Build(string input_regex, string name)
    {
        if (input_regex.Length == 0)
            return new();
        else
        {
            int i = 0;
            var operatorStack = new Stack<char>();
            var operandStack = new Stack<Graph>();
            operatorStack.Push(NullChar);
            var _regex = (input_regex + NullChar);
            while (_regex[i] != NullChar
                    || operatorStack.Peek() != NullChar)
            {
                char c = _regex[i];

                if (IsNotOperator(c))
                {
                    operandStack.Push(new(name,c));
                    i++;
                }
                else
                {
                    int value = GetPriority(operatorStack.Peek(), c);
                    switch (value)
                    {
                        case 1:
                            char character = operatorStack.Pop();
                            switch (character)
                            {
                                case '*':
                                    operandStack.Push(new Graph(name).ZeroPlus(operandStack.Pop()));
                                    break;
                                case '&':
                                    operandStack.Push(new Graph(name).Concate(operandStack.Pop(), operandStack.Pop()));
                                    break;
                                case '|':
                                    operandStack.Push(new Graph(name).Union(operandStack.Pop(), operandStack.Pop()));
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 0:
                            operatorStack.Pop();
                            i++;
                            break;
                        case -1:
                            operatorStack.Push(c);
                            i++;
                            break;
                        default:
                            break;
                    }
                }
            }
            return operandStack.Pop();
        }
    }

    public void SetRegexText(string regex, string? name = null)
    { 
        this.Graph = this.Build(
            this.regex = this.Prepare(regex),
            this.name = name ?? regex);
    }
    protected bool IsNotOperator(char c) 
        => Operators.IndexOf(c)<0;

    protected int GetPriority(char c1, char c2) 
        => Priorities[Operators.IndexOf(c1)][Operators.IndexOf(c2)];


    protected string Invert(string input)
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
    /**
     * 在构建 NFA 之前，需要对正则表达式进行处理，以 (a|b)*abb 为例，在正则表达式里是没有连接符号的，这时就需要添加连接符
     * 对当前字符类型进行判断，并对前一个字符进行判断，最终得到添加连接符之后的字符串
     * 如 (a|b)*abb 添加完则为 (a|b)*&a&b&b
     */
    protected string Prepare(string input_regex)
    {
        var builder = new StringBuilder();
        input_regex = input_regex.Replace(" ", "");
        for (int i = 0; i < input_regex.Length; i++)
            if (i == 0)
                builder.Append(input_regex[i]);
            else
            {
                if (input_regex[i] == '|' || input_regex[i] == '*' || input_regex[i] == ')')
                    builder.Append(input_regex[i]);
                else
                    if (i >= 1 && input_regex[i - 1] == '(' || input_regex[i - 1] == '|')
                    builder.Append(input_regex[i]);
                else
                    builder.Append("&" + input_regex[i]);
            }
        return builder.ToString();
    }

    public bool IsMatch(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start)+","+nameof(length));

        var heads = this.Graph.Heads;
        
        var nodes = heads.ToHashSet();

        var i = start;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            if (copies.All(copy => copy.IsVirtual))
            {
                nodes.UnionWith(copies.SelectMany(n => n.Outputs));
            }
            else
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    if (node.Hit(c))
                    {
                        hit = true;
                        //needs all hits
                        nodes.UnionWith(node.Outputs);
                    }
                }
                if (hit) i++;
            }
        }

        return i == input.Length && (nodes.Count == 0 || nodes.Any(n => n.Outputs.Count == 0));
    }
    public Capture Match(string input,int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "," + nameof(length));
        var s = start;
        var heads = this.Graph.Heads;
    repeat:

        var nodes = heads?.ToHashSet() ?? new ();
        var i = start;
        var m = 0;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            if (copies.All(copy => copy.IsVirtual))
            {
                nodes.UnionWith(copies.SelectMany(n => n.Outputs));
            }
            else
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    if (node.Hit(c))
                    {
                        hit = true;
                        //needs all hits
                        nodes.UnionWith(node.Outputs);
                    }
                }
                if (hit)
                {
                    m++;
                    i++;
                }
                else
                {
                    start+=m;
                    goto repeat;
                }
            }
        }

        return start > s && nodes.Count==0
            ? new (start, m, input[start..(start+m)])
            : new (start,-1);
    }
    public List<Capture> Matches(string input, int start = 0,int length = -1)
    {
        var captures = new List<Capture>();
        while(true)
        {
            var capture = this.Match(input, start, length);
            if (null == capture)
            {
                break;
            }
            else
            {
                if (capture.Index<0 || capture.Length == 0) 
                    break;
                captures.Add(capture);
            }
            if (capture.Index + capture.Length >= length) break;
        }
        return captures;
    }
    public string Replace(string input, string replacement, int count = 0, int start = 0)
        => this.Replace(input, (Capture capture) => replacement, count, start);
    public string Replace(string input, CaptureEvaluator evaluator, int count = 0, int start = 0)
    {
        var matchs = this.Matches(input, start);

        var result = new List<string>();

        start = 0;
        var c = 0;
        foreach (var match in matchs)
        {
            var delta = match.Index - start;
            if (delta > 0) result.Add(input[start..match.Index]);
            var replacement = evaluator(match) ?? "";
            result.Add(replacement);
            start = match.Index + match.Length;
            if (++c >= count) break;
        }
        if (start < input.Length) result.Add(input[start..]);

        return result.Aggregate((a,b)=>a+b);
    }
    public string[] Split(string input, int count = 0, int start = 0)
    {
        var matchs = this.Matches(input, start);
        var result = new List<string>();

        start = 0;
        var c = 0;
        foreach (var match in matchs)
        {
            var delta = match.Index - start;
            if (delta>0) result.Add(input[start..match.Index]);
            start =match.Index + match.Length;
            if (++c >= count) break;
        }
        if (start < input.Length) result.Add(input[start..]);

        return result.ToArray();
    }
}

