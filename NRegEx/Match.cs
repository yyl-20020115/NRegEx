/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Match : Group
{
    public readonly string Input;
    public readonly Regex Source;
    public readonly List<Group> Groups = new();
    public new virtual Group this[string name]
        => this.Groups.FirstOrDefault(g => g.Name == name) ?? Empty;
    public new virtual Group this[int index] 
        => this.Groups[index];
    public new virtual int Count
        => this.Groups.Count;
    public Match(Regex Source, string Input, bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null)
        : base(Success, Name, InclusiveStart, ExclusiveEnd, Value)
    {
        this.Source = Source;
        this.Input = Input;
    }
    public override string ToString()
        => this.Value ?? base.ToString();
}