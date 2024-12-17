/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class CharGroup(int Sign, int[] Class)
{
    public readonly int Sign = Sign;
    public readonly int[] Class = Class;
    private static readonly int[] Code1 = [
        /* \d */
        0x30, 0x39,
    ];

    private static readonly int[] Code2 = [
        /* \s */
        0x9, 0xa, 0xc, 0xd, 0x20, 0x20,
    ];

    private static readonly int[] Code3 = [
        /* \w */
        0x30, 0x39, 0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a,
    ];

    private static readonly int[] Code4 = [
        /* [:alnum:] */
        0x30, 0x39, 0x41, 0x5a, 0x61, 0x7a,
    ];

    private static readonly int[] Code5 = [
        /* [:alpha:] */
        0x41, 0x5a, 0x61, 0x7a,
    ];

    private static readonly int[] Code6 = [
        /* [:ascii:] */
        0x0, 0x7f,
    ];

    private static readonly int[] Code7 = [
        /* [:blank:] */
        0x9, 0x9, 0x20, 0x20,
    ];

    private static readonly int[] Code8 = [
        /* [:cntrl:] */
        0x0, 0x1f, 0x7f, 0x7f,
    ];

    private static readonly int[] Code9 = [
        /* [:digit:] */
        0x30, 0x39,
    ];

    private static readonly int[] Code10 = [
        /* [:graph:] */
        0x21, 0x7e,
    ];

    private static readonly int[] Code11 = [
        /* [:lower:] */
        0x61, 0x7a,
    ];

    private static readonly int[] Code12 = [
        /* [:print:] */
        0x20, 0x7e,
    ];

    private static readonly int[] Code13 = [
        /* [:punct:] */
        0x21, 0x2f, 0x3a, 0x40, 0x5b, 0x60, 0x7b, 0x7e,
    ];

    private static readonly int[] Code14 = [
        /* [:space:] */
        0x9, 0xd, 0x20, 0x20,
    ];

    private static readonly int[] Code15 = [
        /* [:upper:] */
        0x41, 0x5a,
    ];

    private static readonly int[] Code16 = [
        /* [:word:] */
        0x30, 0x39, 0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a,
    ];

    private static readonly int[] Code17 = [
        /* [:xdigit:] */
        0x30, 0x39, 0x41, 0x46, 0x61, 0x66,
    ];

    public static readonly Dictionary<string, CharGroup> PERL_GROUPS = new()
    {
        { "\\d", new(+1, Code1) },
        { "\\D", new(-1, Code1) },
        { "\\s", new(+1, Code2) },
        { "\\S", new(-1, Code2) },
        { "\\w", new(+1, Code3) },
        { "\\W", new(-1, Code3) }
    };
    public static readonly Dictionary<string, CharGroup> POSIX_GROUPS = new()
    {
        { "[:alnum:]", new(+1, Code4) },
        { "[:^alnum:]", new(-1, Code4) },
        { "[:alpha:]", new(+1, Code5) },
        { "[:^alpha:]", new(-1, Code5) },
        { "[:ascii:]", new(+1, Code6) },
        { "[:^ascii:]", new(-1, Code6) },
        { "[:blank:]", new(+1, Code7) },
        { "[:^blank:]", new(-1, Code7) },
        { "[:cntrl:]", new(+1, Code8) },
        { "[:^cntrl:]", new(-1, Code8) },
        { "[:digit:]", new(+1, Code9) },
        { "[:^digit:]", new(-1, Code9) },
        { "[:graph:]", new(+1, Code10) },
        { "[:^graph:]", new(-1, Code10) },
        { "[:lower:]", new(+1, Code11) },
        { "[:^lower:]", new(-1, Code11) },
        { "[:print:]", new(+1, Code12) },
        { "[:^print:]", new(-1, Code12) },
        { "[:punct:]", new(+1, Code13) },
        { "[:^punct:]", new(-1, Code13) },
        { "[:space:]", new(+1, Code14) },
        { "[:^space:]", new(-1, Code14) },
        { "[:upper:]", new(+1, Code15) },
        { "[:^upper:]", new(-1, Code15) },
        { "[:word:]", new(+1, Code16) },
        { "[:^word:]", new(-1, Code16) },
        { "[:xdigit:]", new(+1, Code17) },
        { "[:^xdigit:]", new(-1, Code17) }
    };
}
