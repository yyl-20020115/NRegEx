/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public enum ReplacementType : uint
{
    PlainText = 0,
    GroupIndex = 1,//$number
    GroupName = 2,//$name
    Dollar = 3, //$$
    WholeMatch = 4,//$&
    PreMatch = 5, //$`
    PostMatch = 6, //$'
    LastGroup = 7, //$+
    Input = 8, //$_

}
