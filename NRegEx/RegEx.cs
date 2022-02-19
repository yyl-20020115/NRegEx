namespace NRegEx;

public class Regex
{
    public const char NullChar = '\0';
    public const string Operators = "*&|()#";
    private string regex = "";
    private Stack<object> operatorStack = new Stack<object>();
    private Stack<object> operandStack = new Stack<object>();
    private int[][] priority = new int[][]{
            new []{ 1, 1, 1, -1, 1, 1 }, // *&|()#
            new []{ -1, 1, 1, -1, 1, 1 },
            new []{ -1, -1, 1, -1, 1, 1 },
            new []{ -1, -1, -1, -1, 0, 2 },
            new []{ 1, 1, 1, 1, 1, 1 },
            new []{ -1, -1, -1, -1, -1, -1 } };

    public Regex(string regex = "")
    {
        this.regex = "";
        this.Prepare(regex);
        this.NFA = this.BuildNFA();
    }
    public Graph NFA { get; protected set; }
    /**
     * 核心转换代码
     * stack 记录 NFA 片段
     * 依次读取表达式的每个字符 ch
     * 如果 ch 是运算符，从 stack 出栈所需数目的 NFA 片段，构建新的 NFA 片段后入栈 stack
     * 如果 ch 是普通字符，创建新的状态，并构建只包含此状态的 NFA 片段入栈 stack
     * 返回 stack 栈顶的 NFA 片段，即最终结果
     */
    protected Graph BuildNFA()
    {
        if (regex.Length == 0)
            return new Graph();
        else
        {
            int i = 0;
            operatorStack.Push(NullChar);
            char[] _regex = (regex + NullChar).ToArray();
            while (_regex[i] != NullChar
                    || (char)(operatorStack.Peek()) != NullChar)
            {
                if (!IsOperator(_regex[i]))
                {
                    operandStack.Push(_regex[i]);
                    i++;
                }
                else
                {
                    var value = GetPriority((char)(operatorStack.Peek()), _regex[i]);
                    switch (value)
                    {
                        case 1:
                            char character = (char)operatorStack.Pop();
                            switch (character)
                            {
                                case '*':
                                    operandStack.Push(
                                        new Graph().Star(operandStack.Pop()));
                                    break;
                                case '&':
                                    var obj2 = operandStack.Pop();
                                    var obj1 = operandStack.Pop();
                                    operandStack.Push(new Graph().Concat(obj1, obj2));
                                    break;
                                case '|':
                                    var obj4 = operandStack.Pop();
                                    var obj3 = operandStack.Pop();
                                    operandStack.Push(new Graph().Union(obj3,obj4));
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
                            operatorStack.Push(_regex[i]);
                            i++;
                            break;
                        default:
                            break;
                    }
                }
            }
            return (Graph)operandStack.Pop();
        }
    }

    public void Reset()
    {
        Node.ResetID();
        this.operandStack.Clear();
        this.operatorStack.Clear();
    }

    public string RegexText { get => regex; set {  
            Prepare(value);
            this.operandStack.Clear();
            this.operatorStack.Clear();
            this.NFA = this.BuildNFA();
        } }

    protected bool IsOperator(char c) 
        => Operators.Contains(c.ToString());

    protected int GetPriority(char c1, char c2)
    {
        var priorityString = "*&|()#";
        return this.priority[priorityString.IndexOf(c1.ToString())]
            [priorityString.IndexOf(c2.ToString())];
    }

    /**
     * 在构建 NFA 之前，需要对正则表达式进行处理，以 (a|b)*abb 为例，在正则表达式里是没有连接符号的，这时就需要添加连接符
     * 对当前字符类型进行判断，并对前一个字符进行判断，最终得到添加连接符之后的字符串
     * 如 (a|b)*abb 添加完则为 (a|b)*&a&b&b
     */
    protected void Prepare(string _regex)
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
    }


    public bool Match(string text)
    {

        return false;
    }

}

