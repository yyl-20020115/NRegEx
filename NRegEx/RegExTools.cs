using System.Text;

namespace NRegEx;

public static class RegExTools
{
    public static string DoReplace(string input, string pattern, string replacement)
    {
        var regex = new Regex(pattern);
        var matches = regex.Matches(input);
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
                        repl.Replace(segment.input, segment.match));
            }
            else
            {
                builder.Append(segment.input);
            }
        }

        return builder.ToString();
    }

    public static List<(Match? match,string input)> SegmentInput(string input,List<Match> matches)
    {
        var segments = new List<(Match? match, string input)>();

        if (matches.Count == 0)
        {
            segments.Add((null, input));
        }
        else
        {
            var last = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].InclusiveStart > last)
                {
                    segments.Add((null, input[last..matches[i].InclusiveStart]));
                }

                segments.Add((matches[i], input[matches[i].InclusiveStart..(last = matches[i].ExclusiveEnd)]));
            }
            if (last < input.Length)
            {
                segments.Add((null, input[last..]));
            }
        }
        return segments;
    }
}
