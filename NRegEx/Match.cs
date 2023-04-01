namespace NRegEx;

public class Match : Group
{
    public readonly static Group EmptyGroup = new (false);
    public readonly string Input;
    public readonly Regex Source;
    public readonly List<Group> Groups = new();
    public new virtual Group this[string name] => this.Groups.FirstOrDefault(g => g.Name == name) ?? EmptyGroup;
    public new virtual Group this[int index] => this.Groups[index];
    public new virtual int Count => this.Groups.Count;
    public Match(Regex Source, string Input, bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null)
        : base(Success, Name, InclusiveStart, ExclusiveEnd, Value)
    {
        this.Source = Source;
        this.Input = Input;
    }
    public override string ToString()
        => this.Value ?? base.ToString();
}