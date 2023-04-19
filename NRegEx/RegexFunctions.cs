using System.Text;

namespace NRegEx;

public partial class Regex
{
    public static string[] Split(string input, string pattern, int start = 0, int length = -1, bool reverselySearch = false)
        => new Regex(pattern).Split(input, start, length, reverselySearch);
    public static string ReplaceFirst(string input, string pattern, string replacement, int start = 0)
        => new Regex(pattern).ReplaceFirst(input, replacement, start);

    public static string ReplaceFirst(string input, string pattern, CaptureEvaluator evaluator, int start = 0)
        => new Regex(pattern).ReplaceFirst(input, evaluator, start);

    public static string ReplaceAll(string input, string pattern, string replacement, int start = 0)
        => new Regex(pattern).ReplaceAll(input, replacement, start);

    public static string ReplaceAll(string input, string pattern, CaptureEvaluator evaluator, int start = 0)
        => new Regex(pattern).ReplaceAll(input, evaluator, start);

    public static bool IsMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsMatch(input, start, length);
    public static bool IsFullyMatch(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).IsFullyMatch(input, start, length);
    public static Match Match(string input, string pattern, int start = 0, int length = -1)
        => new Regex(pattern).Match(input, start, length);
    public static List<Match> Matches(string input, string pattern, int start = 0, int length = -1)
         => new Regex(pattern).Matches(input, start, length);
    public string ReplaceFirst(string input, string replacement, int start = 0)
        => this.ReplaceFirst(input, (Capture capture) => replacement, start);
    public string ReplaceFirst(string input, string replacement, Match match, int start = 0)
        => ReplaceFirst(input, (Capture capture) => replacement, match, start);
    public string ReplaceFirst(string input, CaptureEvaluator evaluator, int start = 0)
        => ReplaceFirst(input, evaluator, this.Match(input, start), start);
    public static string ReplaceFirst(string input, CaptureEvaluator evaluator, Match match, int start = 0)
    {
        if (match is not null)
        {
            var builder = new StringBuilder();
            var delta = match.InclusiveStart - start;
            if (delta > 0) builder.Append(input[start..match.InclusiveStart]);
            var replacement = evaluator(match) ?? "";
            builder.Append(replacement);
            start = match.ExclusiveEnd;
            if (start < input.Length) builder.Append(input[start..]);
            return builder.Length > 0 ? builder.ToString() : input;
        }
        return input;
    }

    public string ReplaceAll(string input, string replacement, int start = 0)
        => this.ReplaceAll(input, (Capture capture) => replacement, start);
    public string ReplaceAll(string input, string replacement, List<Match> matches, int start = 0)
        => ReplaceAll(input, (Capture capture) => replacement, matches, start);
    public string ReplaceAll(string input, CaptureEvaluator evaluator, int start = 0)
        => ReplaceAll(input, evaluator, Matches(input, start), start);
    public static string ReplaceAll(string input, CaptureEvaluator evaluator, List<Match> matches, int start = 0)
    {
        var builder = new StringBuilder();
        foreach (var match in matches)
        {
            var delta = match.InclusiveStart - start;
            if (delta > 0) builder.Append(input[start..match.InclusiveStart]);
            var replacement = evaluator(match) ?? "";
            builder.Append(replacement);
            start = match.ExclusiveEnd;
        }
        if (start < input.Length) builder.Append(input[start..]);
        return builder.Length > 0 ? builder.ToString() : input;
    }
    public string[] Split(string input, int first = 0, int length = -1, bool reverselySearch = false)
        => Split(input, this.Matches(input, first, length, reverselySearch));
    public static string[] Split(string input, List<Match> matches)
    {
        var first = 0;
        var result = new List<string>();
        foreach (var match in matches)
        {
            var delta = match.InclusiveStart - first;
            if (delta > 0) result.Add(input[first..match.InclusiveStart]);
            first = match.ExclusiveEnd;
        }
        if (first < input.Length) result.Add(input[first..]);
        return result.ToArray();
    }
}
