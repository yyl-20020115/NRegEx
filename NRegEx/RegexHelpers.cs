/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
public static class RegexHelpers
{
    public static int FixDirection(int direction) => direction >= 0 ? 1 : -1;
    public static int Abs(int value) => value >= 0 ? value : -value;
}