namespace NRegEx;

public record class RegExNode(
    TokenTypes Type = TokenTypes.EOF,
    string Value = "",
    string Name = "",
    int? Min = null,
    int? Max = null,
    int? CaptureIndex = null,
    bool? Negate = null,
    int[]? Runes = null,
    Options Options = Options.None,
    BehaviourOptions BehaviourOptions = BehaviourOptions.Greedy,
    bool Inverted = false,
    int Position = -1,
    int Length = -1,
    string? PatternName=null)
{
    public List<RegExNode> Children = new();
}
