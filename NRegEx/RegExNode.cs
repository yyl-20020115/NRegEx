namespace NRegEx;

public record class RegExNode(
    TokenTypes Type = TokenTypes.EOF,
    string Value = "",
    string Name = "",
    int? Min = null,
    int? Max = null,
    int? CaptureIndex = null,
    GroupType GroupType = GroupType.NotGroup,
    int[]? Runes = null,
    Options Options = Options.None,
    BehaviourOptions BehaviourOptions = BehaviourOptions.Greedy,
    bool Inverted = false,
    int Position = -1,
    int Length = -1,
    string? PatternName=null,
    bool IsRemoved = false)
{
    public List<RegExNode> Children = new();
}
