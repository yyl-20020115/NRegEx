using System.Text;

namespace NRegEx;

public class RegExParser
{

    public const char NullChar = '\0';
    public static string Operators = "*&|()" + NullChar;
    public static readonly int[][] Priorities = new int[][]{
            new []{ 1, 1, 1, -1, 1, 1 }, // *&|()#
            new []{ -1, 1, 1, -1, 1, 1 },
            new []{ -1, -1, 1, -1, 1, 1 },
            new []{ -1, -1, -1, -1, 0, 2 },
            new []{ 1, 1, 1, 1, 1, 1 },
            new []{ -1, -1, -1, -1, -1, -1 } };
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


    public readonly string Name;
    public RegExParser(string name = "")
    {
        this.Name = name;
    }
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
    public Graph Parse(string regex)
    {
        if (!string.IsNullOrEmpty(regex))
        {
            //TODO: need full algorithm (see RE2CS)
        }
        return new();
    }
    public Graph SimpleParse(string regex)
    {
        regex = this.Prepare(regex);
        if (regex.Length == 0)
            return new();
        else
        {
            int i = 0;
            var operatorStack = new Stack<char>();
            var operandStack = new Stack<Graph>();
            operatorStack.Push(NullChar);
            var _regex = (regex + NullChar);
            while (_regex[i] != NullChar
                    || operatorStack.Peek() != NullChar)
            {
                char c = _regex[i];

                if (IsNotOperator(c))
                {
                    operandStack.Push(new(this.Name, c));
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
                                    operandStack.Push(new Graph(this.Name).ZeroPlus(operandStack.Pop()));
                                    break;
                                case '&':
                                    operandStack.Push(new Graph(this.Name).Concate(operandStack.Pop(), operandStack.Pop()));
                                    break;
                                case '|':
                                    operandStack.Push(new Graph(this.Name).Union(operandStack.Pop(), operandStack.Pop()));
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
}
