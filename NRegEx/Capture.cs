/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Capture(string Name = "", int InclusiveStart = 0, int ExclusiveEnd = int.MaxValue, string? Value = null)
{
    public readonly static Capture Empty = new();
    public readonly string Name = Name;
    public readonly int InclusiveStart = InclusiveStart;
    public readonly int ExclusiveEnd = ExclusiveEnd;
    public readonly string? Value = Value;

    public override string ToString()  => Value ?? string.Empty;
}
