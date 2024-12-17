/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Match(Regex Source, string Input, bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null) : Group(Success, Name, InclusiveStart, ExclusiveEnd, Value)
{
    public readonly string Input = Input;
    public readonly Regex Source = Source;
    public readonly List<Group> Groups = [];
    public new virtual Group this[string name]
        => this.Groups.FirstOrDefault(g => g.Name == name) ?? Empty;
    public new virtual Group this[int index] 
        => this.Groups[index];
    public new virtual int Count
        => this.Groups.Count;

    public override string ToString()
        => this.Value ?? base.ToString();
}