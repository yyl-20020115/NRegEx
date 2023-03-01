namespace NRegEx;

public class Group : Capture
{
    public readonly bool Success;
    public readonly List<Capture> Captures =new();
    public Capture? this[string name]=>this.Captures.FirstOrDefault(c=>c.Name==name);
    public Capture this[int index] => this.Captures[index];

    public int Count => this.Captures.Count;
    public Group(bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = -1, string? Value = null)
        :base(Name,InclusiveStart,ExclusiveEnd,Value)
    {
        this.Success = Success;
    }
    public override string ToString() => string.Join<Capture>(' ', Captures.ToArray());
}
