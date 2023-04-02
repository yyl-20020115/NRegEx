/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Capture
{
    public readonly static Capture Empty = new();
    public readonly string Name;
    public readonly int InclusiveStart;
    public readonly int ExclusiveEnd;
    public readonly string? Value;
    public Capture(string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null)
    {
        this.Name = Name;
        this.InclusiveStart = InclusiveStart;
        this.ExclusiveEnd = ExclusiveEnd;
        this.Value = Value;
    }
    public override string ToString() => Value ?? string.Empty;
}
