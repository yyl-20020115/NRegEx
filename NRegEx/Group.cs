namespace NRegEx;

public class Group : Capture
{
    public readonly static Capture EmptyCapture = new ();
    public readonly bool Success;
    public readonly List<Capture> Captures = new();
    public virtual Capture this[string name] => this.Captures.FirstOrDefault(c => c.Name == name) ?? EmptyCapture;
    public virtual Capture this[int index] => this.Captures[index];

    public virtual int Count => this.Captures.Count;
    public Group(bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null)
        : base(Name, InclusiveStart, ExclusiveEnd, Value)
    {
        this.Success = Success;
    }
    public override string ToString()
        => this.Count > 0 ? string.Join(' ', this.Captures) : this.Value ?? string.Empty;
}
