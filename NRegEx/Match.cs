namespace NRegEx;

public class Match : Group
{
    public readonly string Input;
    public readonly Regex Source;
    public readonly List<Group> Groups = new();
    public new Group? this[string name] => this.Groups.FirstOrDefault(g => g.Name == name);
    public new Group? this[int index] => this.Groups[index];
    public new int Count => this.Groups.Count;
    public Match(Regex Source, string Input, bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null)
        : base(Success, Name, InclusiveStart, ExclusiveEnd, Value)
    {
        this.Source = Source;
        this.Input = Input;
    }
}