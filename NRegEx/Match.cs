namespace NRegEx;

public class Match : Group
{
    public readonly List<Group> Groups =new();
    public new Group? this[string name] => this.Groups.FirstOrDefault(g => g.Name == name);
    public new Group? this[int index] => this.Groups[index];
    public new int Count=> this.Groups.Count;
    public Match(bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = -1, string? Value = null)
        : base(Success,Name,InclusiveStart,ExclusiveEnd,Value) 
    {
    }
    public override string ToString() => string.Join<Group>(' ', this.Groups.ToArray());
}