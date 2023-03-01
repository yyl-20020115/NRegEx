﻿namespace NRegEx;

public class Capture
{
    public readonly string Name;
    public readonly int InclusiveStart;
    public readonly int ExclusiveEnd;
    public readonly string? Value;
    public Capture(string Name = "",int InclusiveStart = 0,int ExclusiveEnd = -1, string? Value=null)
    {
        this.Name = Name;
        this.InclusiveStart = InclusiveStart;
        this.ExclusiveEnd = ExclusiveEnd;   
        this.Value = Value;
    }
    public override string ToString() => Value ?? string.Empty;
}