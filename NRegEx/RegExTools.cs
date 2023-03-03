using System.Text;

namespace NRegEx;

public static class RegExTools
{
    public static string Result(this Match match, string replacement)
        => DoReplace(match.Input, new Match[] { match }, replacement);
    public static string Result(this IEnumerable<Match> matches, string replacement)
        => DoReplace(matches.First().Input, matches.ToArray(), replacement);
    public static string DoReplace(string input, string pattern, string replacement) 
        => DoReplace(input, new Regex(pattern).Matches(input).ToArray(), replacement);
    public static string DoReplace(string input, Match[] matches, string replacement)
    {
        var builder = new StringBuilder();
        var parser = new RegExReplacementParser(replacement);
        var list = parser.Parse();
        var segments = SegmentInput(input, matches);

        foreach (var segment in segments)
        {
            if (segment.match != null)
            {
                foreach (var repl in list)
                    builder.Append(
                        repl.Replace(segment.match, segment.input, segment.pre, segment.post));
            }
            else
            {
                builder.Append(segment.input);
            }
        }

        return builder.ToString();
    }

    public static List<(Match? match, string input, string pre, string post)> SegmentInput(string input,params Match[] matches)
    {
        var segments = new List<(Match? match, string input, string pre, string post)>();

        if (matches.Length == 0)
        {
            segments.Add((null, input,"",""));
        }
        else
        {
            var last = 0;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].InclusiveStart > last)
                {
                    segments.Add((null, input[last..matches[i].InclusiveStart],"",""));
                }

                segments.Add((matches[i], input[matches[i].InclusiveStart..(last = matches[i].ExclusiveEnd)],"",""));
            }
            if (last < input.Length)
            {
                segments.Add((null, input[last..],"",""));
            }
        }
        var rebuild = new List<(Match? match, string input, string pre, string post)>();
        for(int i= 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (i > 0)
            {
                segment.pre = segments[i - 1].input;
            }
            if (i < segments.Count - 1)
            {
                segment.post = segments[i + 1].input;
            }
            rebuild.Add(segment);
        }
        return rebuild;
    }
}
