/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class RegExReplacement(ReplacementType Type, string Value = "", int index = -1, string Name = "")
{
    public readonly ReplacementType Type = Type;
    public readonly string Name = Name;
    public readonly int Index = index;
    public readonly string Value = Value;

    public string Replace(Match match, string input, string pre, string post) => Type switch
    {
        ReplacementType.Dollar => "$",
        ReplacementType.PlainText => this.Value ?? string.Empty,
        ReplacementType.GroupIndex => match[this.Index]?.Value ?? string.Empty,
        ReplacementType.GroupName => match[this.Name]?.Value ?? string.Empty,
        ReplacementType.WholeMatch => match.Value ?? string.Empty,
        ReplacementType.PreMatch => pre,
        ReplacementType.PostMatch => post,
        ReplacementType.LastGroup => match.Groups.LastOrDefault()?.Value ?? string.Empty,
        ReplacementType.Input => input,
        _ => "",
    };
}
