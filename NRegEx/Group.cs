/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Group(bool Success, string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null) : Capture(Name, InclusiveStart, ExclusiveEnd, Value)
{
    public readonly new static Group Empty = new(false);

    public readonly bool Success = Success;
    public readonly List<Capture> Captures = [];
    public virtual Capture this[string name] => this.Captures.FirstOrDefault(c => c.Name == name) ?? Capture.Empty;
    public virtual Capture this[int index] => this.Captures[index];

    public virtual int Count => this.Captures.Count;

    public override string ToString()
        => this.Count > 0 ? string.Join(' ', this.Captures) : this.Value ?? string.Empty;
}
