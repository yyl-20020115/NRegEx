/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public record class RegExNode(
    TokenTypes Type = TokenTypes.EOF,
    string Value = "",
    string Name = "",
    int? Min = null,
    int? Max = null,
    int? CaptureIndex = null,
    GroupType GroupType = GroupType.NotGroup,
    int[]? Runes = null,
    Options Options = Options.None,
    BehaviourOptions BehaviourOptions = BehaviourOptions.Greedy,
    bool Inverted = false,
    int Position = -1,
    int Length = -1,
    string? PatternName = null,
    bool IsRemoved = false)
{
    public bool IsRemoved { get; set; } = IsRemoved;
    public List<RegExNode> Children = new();
}
