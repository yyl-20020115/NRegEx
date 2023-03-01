using NRegEx;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Many of these were derived from the corresponding Go functions in
// http://code.google.com/p/go/source/browse/src/pkg/unicode/letter.go

namespace NRegEx;

/**
 * Utilities for dealing with Unicode better than Java does.
 *
 * @author adonovan@google.com (Alan Donovan)
 */
public static class Unicode
{

    // The highest legal rune value.
    public const int MAX_RUNE = 0x10FFFF;

    // The highest legal ASCII value.
    public const int MAX_ASCII = 0x7f;

    // The highest legal Latin-1 value.
    public const int MAX_LATIN1 = 0xFF;

    // Minimum and maximum runes involved in folding.
    // Checked during test.
    public const int MIN_FOLD = 0x0041;
    public const int MAX_FOLD = 0x1044f;

    public const int SURROGATE_FIRST = 0xd800;
    public const int SURROGATE_LAST = 0xdfff;
    public static bool IsSurrogate(int c) => c >= SURROGATE_FIRST && c <= SURROGATE_LAST;
    public static bool IsValidUTF32(int c) => c >= 0 && c <= MAX_RUNE && !IsSurrogate(c);
    // is32 uses binary search to test whether rune is in the specified
    // slice of 32-bit ranges.
    // TODO(adonovan): opt: consider using int[n*3] instead of int[n][3].
    private static bool Is32(int[][] ranges, int r)
    {
        // binary search over ranges
        for (int lo = 0, hi = ranges.Length; lo < hi;)
        {
            var m = lo + (hi - lo) / 2;
            var range = ranges[m]; // [lo, hi, stride]
            if (range[0] <= r && r <= range[1])
                return ((r - range[0]) % range[2]) == 0;
            if (r < range[0])
                hi = m;
            else
                lo = m + 1;
        }
        return false;
    }

    // is tests whether rune is in the specified table of ranges.
    private static bool IsIn(int[][] ranges, int r)
    {
        // common case: rune is ASCII or Latin-1, so use linear search.
        if (r <= MAX_LATIN1)
        {
            foreach (int[] range in ranges)
                // range = [lo, hi, stride]
                if (r > range[1])
                    continue;
                else
                    return r >= range[0] && ((r - range[0]) % range[2]) == 0;
            return false;
        }
        return ranges.Length > 0 && r >= ranges[0][0] && Is32(ranges, r);
    }

    // isUpper reports whether the rune is an upper case letter.
    public static bool IsUpper(int r) =>
        // See comment in isGraphic.
        r <= MAX_LATIN1 ? char.IsUpper((char)r) : IsIn(UnicodeTables.Upper, r);

    // isPrint reports whether the rune is printable (Unicode L/M/N/P/S or ' ').
    public static bool IsPrint(int r) => r <= MAX_LATIN1
            ? (r >= 0x20 && r < 0x7F) || (r >= 0xA1 && r != 0xAD)
            : IsIn(UnicodeTables.L, r)
            || IsIn(UnicodeTables.M, r)
            || IsIn(UnicodeTables.N, r)
            || IsIn(UnicodeTables.P, r)
            || IsIn(UnicodeTables.S, r);

    // simpleFold iterates over Unicode code points equivalent under
    // the Unicode-defined simple case folding.  Among the code points
    // equivalent to rune (including rune itself), SimpleFold returns the
    // smallest r >= rune if one exists, or else the smallest r >= 0.
    //
    // For example:
    //      SimpleFold('A') = 'a'
    //      SimpleFold('a') = 'A'
    //
    //      SimpleFold('K') = 'k'
    //      SimpleFold('k') = '\u212A' (Kelvin symbol, K)
    //      SimpleFold('\u212A') = 'K'
    //
    //      SimpleFold('1') = '1'
    //
    // Derived from Go's unicode.SimpleFold.
    //
    public static int SimpleFold(int r)
    {
        // Consult caseOrbit table for special cases.
        if (r < UnicodeTables.CASE_ORBIT.Length && UnicodeTables.CASE_ORBIT[r] != 0)
            return UnicodeTables.CASE_ORBIT[r];
        if (r >= char.MinValue && r <= char.MaxValue)
        {
            var s = char.ToLower((char)r);
            return s != r ? s : char.ToUpper((char)r);
        }
        // No folding specified.  This is a one- or two-element
        // equivalence class containing rune and toLower(rune)
        // and toUpper(rune) if they are different from rune.
        int l = Characters.ToLowerCase(r);
        return l != r ? l : Characters.ToUpperCase(r);
    }
    public static bool IsRuneLetter(int i)
        => Rune.GetUnicodeCategory(new Rune(i))
            is System.Globalization.UnicodeCategory.LowercaseLetter
            or System.Globalization.UnicodeCategory.UppercaseLetter
            or System.Globalization.UnicodeCategory.TitlecaseLetter
            or System.Globalization.UnicodeCategory.ModifierLetter
            or System.Globalization.UnicodeCategory.OtherLetter
            ;
    public static bool IsRuneWord(int i)
         => IsRuneLetter(i) || 
        (Rune.GetUnicodeCategory(new Rune(i))
        is System.Globalization.UnicodeCategory.DecimalDigitNumber
        or System.Globalization.UnicodeCategory.LetterNumber
        or System.Globalization.UnicodeCategory.OtherNumber
        )||(i=='_');
}
public static class Characters
{
    public static int ToLowerCase(int codePoint)
        => (codePoint >= 0 && codePoint <= char.MaxValue)
        ? (char.IsSurrogate((char)codePoint)
        ? -1 : char.ToLower((char)codePoint)) :
        char.ConvertToUtf32(char.ConvertFromUtf32(codePoint).ToLower(), 0);

    public static int ToUpperCase(int codePoint)
        => (codePoint >= 0 && codePoint <= char.MaxValue)
        ? (char.IsSurrogate((char)codePoint)
        ? -1 : char.ToUpper((char)codePoint)) :
        char.ConvertToUtf32(char.ConvertFromUtf32(codePoint).ToUpper(), 0);

}
