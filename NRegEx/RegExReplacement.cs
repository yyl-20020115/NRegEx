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
    public string Replace(Match match,string input,string pre,string post) => Type switch
    {
        ReplacementType.Dollar => "$",
        ReplacementType.PlainText => this.Value??string.Empty,
        ReplacementType.GroupIndex => match[this.Index]?.Value??string.Empty,
        ReplacementType.GroupName => match[this.Name]?.Value??string.Empty,
        ReplacementType.WholeMatch => match.Value??string.Empty,
        ReplacementType.PreMatch => pre,
        ReplacementType.PostMatch => post,
        ReplacementType.LastGroup => match.Groups.LastOrDefault()?.Value??string.Empty,
        ReplacementType.Input => input,
        _ => "",
    };
}
