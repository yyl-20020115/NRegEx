﻿/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/all_test.go
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace NRegex.Test;


/** Tests of RE2 API. */
[TestClass]
public class RE2Test
{
    [TestMethod]
    public void TestFullMatch()
    {
        AssertTrue(new RE2("ab+c").Match("abbbbbc", 0, 7, RE2.ANCHOR_BOTH, null, 0));
        AssertFalse(new RE2("ab+c").Match("xabbbbbc", 0, 8, RE2.ANCHOR_BOTH, null, 0));

        AssertTrue(new RE2("ab+c").Match(MatcherInput.Utf8("abbbbbc"), 0, 7, RE2.ANCHOR_BOTH, null, 0));
        AssertFalse(
            new RE2("ab+c").Match(MatcherInput.Utf8("xabbbbbc"), 0, 8, RE2.ANCHOR_BOTH, null, 0));
    }

    [TestMethod]
    public void TestFindEnd()
    {
        RE2 r = new RE2("abc.*def");
        string s = "yyyabcxxxdefzzz";
        foreach (MatcherInput input in new List<MatcherInput>()
            { MatcherInput.Utf8(s), MatcherInput.Utf16(s) })
        {
            AssertTrue(r.Match(input, 0, 15, RE2.UNANCHORED, null, 0));
            AssertTrue(r.Match(input, 0, 12, RE2.UNANCHORED, null, 0));
            AssertTrue(r.Match(input, 3, 15, RE2.UNANCHORED, null, 0));
            AssertTrue(r.Match(input, 3, 12, RE2.UNANCHORED, null, 0));
            AssertFalse(r.Match(input, 4, 12, RE2.UNANCHORED, null, 0));
            AssertFalse(r.Match(input, 3, 11, RE2.UNANCHORED, null, 0));
        }
    }
    public static void AssertTrue(bool v)
    {
        Assert.IsTrue(v);
    }
    public static void AssertFalse(bool v)
    {
        Assert.IsFalse(v);
    }
    public static void AssertTrue(string s, bool v)
    {
        Assert.IsTrue(v, s);
    }
    public static void AssertFalse(string s, bool v)
    {
        Assert.IsFalse(v, s);
    }
}
