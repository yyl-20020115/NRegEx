﻿/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public enum GroupType : int
{
    NotGroup = -1,
    NormalGroup = 0,
    NotCaptiveGroup = 1,
    NotCaptiveDefinitionGroup = 2,
    DefinitionReferenceGroup = 3,
    AtomicGroup = 4,
    ForwardPositiveGroup = 5,
    ForwardNegativeGroup = 6,
    BackwardPositiveGroup = 7,
    BackwardNegativeGroup = 8,

    BackReferenceCondition = 9,
    LookAroundConditionGroup = 10,
    BackReferenceConditionGroup = 11,
}
