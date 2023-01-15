namespace NRegEx;

public class RegExPatternReader
{
    public readonly string Pattern;
    public readonly Stack<int> PositionStack = new();
    protected int position = 0;
    public int Position 
        => this.position;
    public bool HasMore 
        => this.position < this.Pattern.Length;
    public string Rest 
        => this.Pattern[this.position..];
    public RegExPatternReader(string pattern) =>
        this.Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    public void RewindTo(int pos) 
        => this.position = pos;
    public int Peek() 
        => char.ConvertToUtf32(Pattern, this.position);
    public void Skip(int n = 1) 
        => this.position += n;
    public void SkipString(string s) 
        => this.position += s.Length;
    public int Pop()
    {
        this.position += (char.ConvertToUtf32(this.Pattern, this.position) is int r) && (r > char.MaxValue) ? 2 : 1;
        return r;
    }
    public string TakeString() 
        => char.ConvertFromUtf32(this.Pop());
    public bool LookingAt(char c) 
        => Pattern[this.position] == c;
    public bool LookingAt(string s) 
        => Rest.StartsWith(s);
    public string From(int previous)
        => Pattern[previous..position];
    public override string ToString() 
        => Rest;
    public int Enter()
    {
        this.PositionStack.Push(this.position);
        return this.position;
    }
    public bool Leave()
    {
        if (this.PositionStack.Count > 0)
        {
            this.position = this.PositionStack.Pop();
            return true;
        }
        return false;
    }
    public bool Discard()
    {
        if (this.PositionStack.Count > 0)
        {
            this.PositionStack.Pop();
            return true;
        }
        return false;
    }
}
