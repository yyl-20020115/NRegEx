using System.Text;

namespace NRegEx;

public static class RegExTools
{
    public static string Result(this Match match, string replacement)
        => DoReplace(match.Input, match, replacement);
    public static string DoReplace(string input, string pattern, string replacement)
    {
        var regex = new Regex(pattern);
        var match = regex.Match(input);
        return DoReplace(input, match, replacement);
    }
    public static string DoReplace(string input, Match match, string replacement)
    {
        var builder = new StringBuilder();
        var parser = new RegExReplacementParser(replacement);
        var list = parser.Parse();
        foreach (var repl in list)
        {
            builder.Append(repl.Replace(input, match));
        }
        return builder.ToString();
    }
}
