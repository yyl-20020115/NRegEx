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
    TokenOptions TokenOptions = TokenOptions.Normal,
    bool Inverted = false,
    int Position = -1,
    int Length = -1)
{
    public List<RegExNode> Children = new();
}
