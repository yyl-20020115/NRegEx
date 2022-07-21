﻿/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NRegex.Test;

[TestClass]
public class RE2ReplaceTest
{
    private static readonly string[][] REPLACE_TESTS = {
    // Test empty input and/or replacement,
    // with pattern that matches the empty string.
    new string[]{"", "", "", "", "false"},
    new string[]{"", "x", "", "x", "false"},
    new string[]{"", "", "abc", "abc", "false"},
    new string[]{"", "x", "abc", "xaxbxcx", "false"},

    // Test empty input and/or replacement,
    // with pattern that does not match the empty string.
    new string[]{"b", "", "", "", "false"},
    new string[]{"b", "x", "", "", "false"},
    new string[]{"b", "", "abc", "ac", "false"},
    new string[]{"b", "x", "abc", "axc", "false"},
    new string[]{"y", "", "", "", "false"},
    new string[]{"y", "x", "", "", "false"},
    new string[]{"y", "", "abc", "abc", "false"},
    new string[]{"y", "x", "abc", "abc", "false"},

    // Multibyte characters -- verify that we don't try to match in the middle
    // of a character.
    new string[]{"[a-c]*", "x", "\u65e5", "x\u65e5x", "false"},
    new string[]{"[^\u65e5]", "x", "abc\u65e5def", "xxx\u65e5xxx", "false"},

    // Start and end of a string.
    new string[]{"^[a-c]*", "x", "abcdabc", "xdabc", "false"},
    new string[]{"[a-c]*$", "x", "abcdabc", "abcdx", "false"},
    new string[]{"^[a-c]*$", "x", "abcdabc", "abcdabc", "false"},
    new string[]{"^[a-c]*", "x", "abc", "x", "false"},
    new string[]{"[a-c]*$", "x", "abc", "x", "false"},
    new string[]{"^[a-c]*$", "x", "abc", "x", "false"},
    new string[]{"^[a-c]*", "x", "dabce", "xdabce", "false"},
    new string[]{"[a-c]*$", "x", "dabce", "dabcex", "false"},
    new string[]{"^[a-c]*$", "x", "dabce", "dabce", "false"},
    new string[]{"^[a-c]*", "x", "", "x", "false"},
    new string[]{"[a-c]*$", "x", "", "x", "false"},
    new string[]{"^[a-c]*$", "x", "", "x", "false"},
    new string[]{"^[a-c]+", "x", "abcdabc", "xdabc", "false"},
    new string[]{"[a-c]+$", "x", "abcdabc", "abcdx", "false"},
    new string[]{"^[a-c]+$", "x", "abcdabc", "abcdabc", "false"},
    new string[]{"^[a-c]+", "x", "abc", "x", "false"},
    new string[]{"[a-c]+$", "x", "abc", "x", "false"},
    new string[]{"^[a-c]+$", "x", "abc", "x", "false"},
    new string[]{"^[a-c]+", "x", "dabce", "dabce", "false"},
    new string[]{"[a-c]+$", "x", "dabce", "dabce", "false"},
    new string[]{"^[a-c]+$", "x", "dabce", "dabce", "false"},
    new string[]{"^[a-c]+", "x", "", "", "false"},
    new string[]{"[a-c]+$", "x", "", "", "false"},
    new string[]{"^[a-c]+$", "x", "", "", "false"},

    // Other cases.
    new string[]{"abc", "def", "abcdefg", "defdefg", "false"},
    new string[]{"bc", "BC", "abcbcdcdedef", "aBCBCdcdedef", "false"},
    new string[]{"abc", "", "abcdabc", "d", "false"},
    new string[]{"x", "xXx", "xxxXxxx", "xXxxXxxXxXxXxxXxxXx", "false"},
    new string[]{"abc", "d", "", "", "false"},
    new string[]{"abc", "d", "abc", "d", "false"},
    new string[]{".+", "x", "abc", "x", "false"},
    new string[]{"[a-c]*", "x", "def", "xdxexfx", "false"},
    new string[]{"[a-c]+", "x", "abcbcdcdedef", "xdxdedef", "false"},
    new string[]{"[a-c]*", "x", "abcbcdcdedef", "xdxdxexdxexfx", "false"},

    // Test empty input and/or replacement,
    // with pattern that matches the empty string.
    new string[]{"", "", "", "", "true"},
    new string[]{"", "x", "", "x", "true"},
    new string[]{"", "", "abc", "abc", "true"},
    new string[]{"", "x", "abc", "xabc", "true"},

    // Test empty input and/or replacement,
    // with pattern that does not match the empty string.
    new string[]{"b", "", "", "", "true"},
    new string[]{"b", "x", "", "", "true"},
    new string[]{"b", "", "abc", "ac", "true"},
    new string[]{"b", "x", "abc", "axc", "true"},
    new string[]{"y", "", "", "", "true"},
    new string[]{"y", "x", "", "", "true"},
    new string[]{"y", "", "abc", "abc", "true"},
    new string[]{"y", "x", "abc", "abc", "true"},

    // Multibyte characters -- verify that we don't try to match in the middle
    // of a character.
    new string[]{"[a-c]*", "x", "\u65e5", "x\u65e5", "true"},
    new string[]{"[^\u65e5]", "x", "abc\u65e5def", "xbc\u65e5def", "true"},

    // Start and end of a string.
    new string[]{"^[a-c]*", "x", "abcdabc", "xdabc", "true"},
    new string[]{"[a-c]*$", "x", "abcdabc", "abcdx", "true"},
    new string[]{"^[a-c]*$", "x", "abcdabc", "abcdabc", "true"},
    new string[]{"^[a-c]*", "x", "abc", "x", "true"},
    new string[]{"[a-c]*$", "x", "abc", "x", "true"},
    new string[]{"^[a-c]*$", "x", "abc", "x", "true"},
    new string[]{"^[a-c]*", "x", "dabce", "xdabce", "true"},
    new string[]{"[a-c]*$", "x", "dabce", "dabcex", "true"},
    new string[]{"^[a-c]*$", "x", "dabce", "dabce", "true"},
    new string[]{"^[a-c]*", "x", "", "x", "true"},
    new string[]{"[a-c]*$", "x", "", "x", "true"},
    new string[]{"^[a-c]*$", "x", "", "x", "true"},
    new string[]{"^[a-c]+", "x", "abcdabc", "xdabc", "true"},
    new string[]{"[a-c]+$", "x", "abcdabc", "abcdx", "true"},
    new string[]{"^[a-c]+$", "x", "abcdabc", "abcdabc", "true"},
    new string[]{"^[a-c]+", "x", "abc", "x", "true"},
    new string[]{"[a-c]+$", "x", "abc", "x", "true"},
    new string[]{"^[a-c]+$", "x", "abc", "x", "true"},
    new string[]{"^[a-c]+", "x", "dabce", "dabce", "true"},
    new string[]{"[a-c]+$", "x", "dabce", "dabce", "true"},
    new string[]{"^[a-c]+$", "x", "dabce", "dabce", "true"},
    new string[]{"^[a-c]+", "x", "", "", "true"},
    new string[]{"[a-c]+$", "x", "", "", "true"},
    new string[]{"^[a-c]+$", "x", "", "", "true"},

    // Other cases.
    new string[]{"abc", "def", "abcdefg", "defdefg", "true"},
    new string[]{"bc", "BC", "abcbcdcdedef", "aBCbcdcdedef", "true"},
    new string[]{"abc", "", "abcdabc", "dabc", "true"},
    new string[]{"x", "xXx", "xxxXxxx", "xXxxxXxxx", "true"},
    new string[]{"abc", "d", "", "", "true"},
    new string[]{"abc", "d", "abc", "d", "true"},
    new string[]{".+", "x", "abc", "x", "true"},
    new string[]{"[a-c]*", "x", "def", "xdef", "true"},
    new string[]{"[a-c]+", "x", "abcbcdcdedef", "xdcdedef", "true"},
    new string[]{"[a-c]*", "x", "abcbcdcdedef", "xdcdedef", "true"},
    };


    [TestMethod]
    public void ReplaceTest()
    {
    //private readonly string pattern;
    //private readonly string replacement;
    //private readonly string source;
    //private readonly string expected;
    //private bool replaceFirst;
        for(int i = 0; i < REPLACE_TESTS.Length; i++)
        {
            var t=REPLACE_TESTS[i];
            bool.TryParse(t[4], out var b);

            this.ReplaceTest(i, t[0], t[1], t[2], t[3], b);
        }
        Assert.IsTrue(true);
    }
    public void ReplaceTest(int i,string pattern,
        string replacement, string source, string expected,bool replaceFirst)
    {
        RE2 re = null;
        try
        {
            re = RE2.Compile(pattern);
        }
        catch (PatternSyntaxException e)
        {
            Fail(string.Format("Unexpected error compiling {0}: {1}", pattern, e.Message));
        }
        string actual =
            replaceFirst ? re.ReplaceFirst(source, replacement) : re.ReplaceAll(source, replacement);
        if (!actual.Equals(expected))
        {
            Fail(
                string.Format(
                    "{0}.replaceAll({1},{2}) = {3}; want {4}",
                    pattern,
                    source,
                    replacement,
                    actual,
                    expected));
        }
    }

    private void Fail(string p)
    {
        Assert.Fail(p);
    }
}
