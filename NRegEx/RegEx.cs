using System.Text;

namespace NRegEx;

public class Regex
{
    public const char NullChar = '\0';
    public static string Operators = "*&|()" + NullChar;
    protected string regex = "";
    protected string inverted = "";
    protected static int[][] priorities = new int[][]{
            new []{ 1, 1, 1, -1, 1, 1 }, // *&|()#
            new []{ -1, 1, 1, -1, 1, 1 },
            new []{ -1, -1, 1, -1, 1, 1 },
            new []{ -1, -1, -1, -1, 0, 2 },
            new []{ 1, 1, 1, 1, 1, 1 },
            new []{ -1, -1, -1, -1, -1, -1 } };
    public static bool HigherPriority(char c1, char c2)
        => Operators.IndexOf(c1) <= Operators.IndexOf(c2);
    public Regex(string regex = "")
    {
        this.regex = "";
        this.Prepare(regex);
        this.Graph = this.Build();
    }
    public Graph Graph { get; protected set; }

    protected Graph Concate(Graph g2, Graph g1) => new Graph().Concate(g2, g1);
    protected Graph Union(Graph g2, Graph g1) => new Graph().Union(g2, g1);
    protected Graph Star(Graph g) => new Graph().Star(g);
    /**
     * 核心转换代码
     * stack 记录 NFA 片段
     * 依次读取表达式的每个字符 ch
     * 如果 ch 是运算符，从 stack 出栈所需数目的 NFA 片段，构建新的 NFA 片段后入栈 stack
     * 如果 ch 是普通字符，创建新的状态，并构建只包含此状态的 NFA 片段入栈 stack
     * 返回 stack 栈顶的 NFA 片段，即最终结果
     */
    protected Graph Build()
    {
        if (regex.Length == 0)
            return new();
        else
        {
            int i = 0;
            var operatorStack = new Stack<char>();
            var operandStack = new Stack<Graph>();
            operatorStack.Push(NullChar);
            var _regex = (regex + NullChar).ToArray();
            while (_regex[i] != NullChar
                    || operatorStack.Peek() != NullChar)
            {
                char c = _regex[i];

                if (IsNotOperator(c))
                {
                    operandStack.Push(new(c));
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
                                    operandStack.Push(new Graph().Star(operandStack.Pop()));
                                    break;
                                case '&':
                                    operandStack.Push(new Graph().Concate(operandStack.Pop(), operandStack.Pop()));
                                    break;
                                case '|':
                                    operandStack.Push(new Graph().Union(operandStack.Pop(), operandStack.Pop()));
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

    public string RegexText { get => regex; set {  
            Prepare(value);
            this.Graph = this.Build();
        } }

    protected bool IsNotOperator(char c) 
        => Operators.IndexOf(c)<0;

    protected int GetPriority(char c1, char c2)
    {
        return priorities[Operators.IndexOf(c1)]
            [Operators.IndexOf(c2)];
    }

    /**
     * 在构建 NFA 之前，需要对正则表达式进行处理，以 (a|b)*abb 为例，在正则表达式里是没有连接符号的，这时就需要添加连接符
     * 对当前字符类型进行判断，并对前一个字符进行判断，最终得到添加连接符之后的字符串
     * 如 (a|b)*abb 添加完则为 (a|b)*&a&b&b
     */
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
    protected string Prepare(string _regex)
    {
        var regexs = _regex.Replace(" ", "").ToArray();
        for (int i = 0; i < regexs.Length; i++)
        {
            if (i == 0)
                regex += regexs[i];
            else
            {
                if (regexs[i] == '|' || regexs[i] == '*' || regexs[i] == ')')
                {
                    regex += regexs[i];
                }
                else
                {
                    if (i>=1 && regexs[i - 1] == '(' || regexs[i - 1] == '|')
                        regex += regexs[i];
                    else
                        regex += ("&" + regexs[i]);
                }
            }
        }
        return this.inverted = this.Invert(regex);
    }

    protected HashSet<Node> MoveForward(IEnumerable<Node> nodes)
    {
        var ret = new HashSet<Node>(nodes);

        var any = true;
        while (any)
        {
            var rta = ret.ToArray();
            ret.Clear();
            any= false;
            foreach (var n in rta)
            {
                if (n.IsVirtual)
                {
                    any |= true;
                    ret.UnionWith(n.Outputs);
                }
            }
            if (!any)
            {
                ret.UnionWith(rta);
            }
        }

        return ret;
    }
    public bool Match(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            var nodes = this.Graph.Nodes
                    .Where(n=>n.Inputs.Count==0).ToHashSet();

            var i = 0;
            while (nodes.Count>0 && i<text.Length)
            {
                var copies = nodes.ToArray();
                nodes.Clear();
                if (copies.All(copy => copy.IsVirtual))
                {
                    nodes.UnionWith(copies.SelectMany(n => n.Outputs));
                }
                else
                {
                    var c = text[i];
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

            return i == text.Length;
        }

        return false;
    }

}

