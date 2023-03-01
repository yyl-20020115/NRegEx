namespace NRegEx;

public static class RegExTools
{
    //CHECK
    public static string Result(this Match match, string replacement) 
        => match.Source.ReplaceAll(match.Input, replacement, new List<Match> { match });
}
