namespace NRegEx;

public class RegExReplacement
{
    public readonly ReplacementType Type;
    public readonly string Name;
    public readonly int Index;
    public readonly string Value;
    public RegExReplacement(ReplacementType Type, string Value = "", int index = -1, string Name = "")
    {
        this.Type = Type;
        this.Value = Value;
        this.Index = index;
        this.Name = Name;
    }
    //TODO: check replace logic
    public string Replace(string input, Match match) => Type switch
    {
        ReplacementType.Dollar => "$",
        ReplacementType.PlainText => this.Value??string.Empty,
        ReplacementType.GroupIndex => match[this.Index]?.Value??string.Empty,
        ReplacementType.GroupName => match[this.Name]?.Value??string.Empty,
        ReplacementType.WholeMatch => match.Value??string.Empty,
        ReplacementType.PreMatch => input[..match.InclusiveStart],
        ReplacementType.PostMatch => input[match.ExclusiveEnd..],
        ReplacementType.LastGroup => match.Groups.LastOrDefault()?.Value??string.Empty,
        ReplacementType.WholeGroup => match.Value ?? string.Empty,
        _ => "",
    };
}
