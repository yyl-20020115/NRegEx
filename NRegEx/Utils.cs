/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections;
using System.Text;

namespace NRegEx;

/**
 * Various constants and helper utilities.
 */
public static class Utils
{

    public static readonly int[] EMPTY_INTS = Array.Empty<int>();
    //|()[]{}^$*+?\.
    //there is no place for #
    public readonly static char[] MetaChars
        = { '|', '(', ')', '[', ']', '{', '}', '^', '$', '*', '+', '?', '\\', '.' };

    // Returns true iff |c| is an ASCII letter or decimal digit.
    public static bool Isalnum(int c)
        => c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    // If |c| is an ASCII hex digit, returns its value, otherwise -1.
    public static int Unhex(int c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    private const string METACHARACTERS = @"\.+*?()|[]{}^$";


    public static string EscapeString(string s, bool doubleSlash = false)
    {
        var builder = new StringBuilder();
        foreach (var c in StringToRunes(s))
        {
            EscapeRune(c, builder, doubleSlash);
        }
        return builder.ToString();
    }
    // Appends a RE2 literal to |out| for rune |rune|,
    // with regexp metacharacters escaped.
    public static StringBuilder EscapeRune(int rune, StringBuilder? builder = null, bool doubleSlash = false)
    {
        builder ??= new();
        var slashes = doubleSlash ? "\\\\" : "\\";

        if (Unicode.IsPrint(rune))
        {
            var r = new Rune(rune);
            var m = METACHARACTERS.EnumerateRunes().ToList();
            if (m.IndexOf(r) >= 0)
                builder.Append(slashes);
            builder.Append(r.ToString());
            return builder;
        }
        if (doubleSlash)
        {
            builder.Append(rune switch
            {
                '"' => ("\\\\\""),
                '\\' => ("\\\\\\\\"),
                '\t' => ("\\\\t"),
                '\n' => ("\\\\n"),
                '\r' => ("\\\\r"),
                '\b' => ("\\\\b"),
                '\f' => ("\\\\f"),
                _ => (rune < 0x100
                      ? (@"\\x" + (string.Format("{0:x}", rune) is string t && t.Length == 1 ? "0" : "")
                                + string.Format("{0:x}", rune))
                      : (@"\\x{" + string.Format("{0:x}", rune) + "}")),
            });
        }
        else
        {
            builder.Append(rune switch
            {
                '"' => ("\\\""),
                '\\' => ("\\\\"),
                '\t' => ("\\t"),
                '\n' => ("\\n"),
                '\r' => ("\\r"),
                '\b' => ("\\b"),
                '\f' => ("\\f"),
                _ => (rune < 0x100
                      ? (@"\x" + (string.Format("{0:x}", rune) is string t && t.Length == 1 ? "0" : "")
                                + string.Format("{0:x}", rune))
                      : (@"\x{" + string.Format("{0:x}", rune) + "}")),
            });

        }
        return builder;
    }

    // Returns the array of runes in the specified Java UTF-16 string.
    public static int[] StringToRunes(string str)
        => str.EnumerateRunes().Select(r => r.Value).ToArray();

    // Returns the Java UTF-16 string containing the single rune |r|.
    public static string RuneToString(int r)
        => new Rune(r).ToString();
    public static string RunesToString(IEnumerable<Rune> runes, string? separator = null)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var rune in runes)
        {
            if (!first && separator != null) builder.Append(separator);
            builder.Append(rune.ToString());
            first = false;
        }
        return builder.ToString();
    }

    public static string RunesToString(IEnumerable<int> runes, string? separator = null)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var rune in runes)
        {
            if (!first && separator != null) builder.Append(separator);
            builder.Append(new Rune(rune).ToString());
            first = false;
        }
        return builder.ToString();
    }

    // Returns a new copy of the specified subarray.
    public static int[] SubArray(int[] array, int start, int end)
    {
        var r = new int[end - start];
        for (int i = start; i < end; ++i)
            r[i - start] = array[i];
        return r;
    }

    // Returns a new copy of the specified subarray.
    public static byte[] SubArray(byte[] array, int start, int end)
    {
        var r = new byte[end - start];
        for (int i = start; i < end; ++i)
            r[i - start] = array[i];
        return r;
    }

    // Returns the index of the first occurrence of array |target| within
    // array |source| after |fromIndex|, or -1 if not found.
    public static int IndexOf(byte[] source, byte[] target, int fromIndex)
    {
        if (fromIndex >= source.Length)
            return target.Length == 0 ? source.Length : -1;
        if (fromIndex < 0)
            fromIndex = 0;
        if (target.Length == 0)
            return fromIndex;

        var first = target[0];
        for (int i = fromIndex, max = source.Length - target.Length; i <= max; i++)
        {
            // Look for first byte.
            if (source[i] != first)
                while (++i <= max && source[i] != first) ;

            // Found first byte, now look at the rest of v2.
            if (i <= max)
            {
                int j = i + 1;
                int end = j + target.Length - 1;
                for (int k = 1; j < end && source[j] == target[k]; j++, k++) { }

                if (j == end) return i; // found whole array
            }
        }
        return -1;
    }

    // isWordRune reports whether r is consider a ``word character''
    // during the evaluation of the \b and \B zero-width assertions.
    // These assertions are ASCII-only: the word characters are [A-Za-z0-9_].
    public static bool IsWordRune(int r)
        => r is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_';

    //// EMPTY_* flags

    public const int EMPTY_BEGIN_LINE = 0x01;
    public const int EMPTY_END_LINE = 0x02;
    public const int EMPTY_BEGIN_TEXT = 0x04;
    public const int EMPTY_END_TEXT = 0x08;
    public const int EMPTY_WORD_BOUNDARY = 0x10;
    public const int EMPTY_NO_WORD_BOUNDARY = 0x20;
    public const int EMPTY_ALL = -1; // (impossible)

    // emptyOpContext returns the zero-width assertions satisfied at the position
    // between the runes r1 and r2, a bitmask of EMPTY_* flags.
    // Passing r1 == -1 indicates that the position is at the beginning of the
    // text.
    // Passing r2 == -1 indicates that the position is at the end of the text.
    // TODO(adonovan): move to Machine.
    public static int EmptyOpContext(int r1, int r2)
    {
        var op = 0;
        if (r1 < 0)
            op |= EMPTY_BEGIN_TEXT | EMPTY_BEGIN_LINE;
        if (r1 == '\n')
            op |= EMPTY_BEGIN_LINE;
        if (r2 < 0)
            op |= EMPTY_END_TEXT | EMPTY_END_LINE;
        if (r2 == '\n')
            op |= EMPTY_END_LINE;
        if (IsWordRune(r1) != IsWordRune(r2))
            op |= EMPTY_WORD_BOUNDARY;
        else
            op |= EMPTY_NO_WORD_BOUNDARY;
        return op;
    }

    public static int GetHashCode<T>(IStructuralEquatable s)
        => s.GetHashCode(EqualityComparer<T>.Default);

    public static int CodePointBefore(this string s, int i)
    {
        if (i > 0)
        {
            var c = s[--i];
            if (char.IsLowSurrogate(c) && i > 0)
            {
                var d = s[--i];
                if (char.IsHighSurrogate(d))
                    return char.ConvertToUtf32(d, c);
            }
            return c;
        }
        return -1;
    }
    public static int FastIndexOf(string source, string pattern)
    {
        var limit = source.Length - pattern.Length + 1;
        if (limit < 1) return -1;

        // Store the first 2 characters of "pattern"
        var c0 = pattern[0];
        var c1 = pattern.Length > 1 ? pattern[1] : ' ';

        // Find the first occurrence of the first character
        var first = source.IndexOf(c0, 0, limit);

        while (first != -1)
        {
            // Check if the following character is the same like the 2nd character of "pattern"
            if (pattern.Length > 1 && source[first + 1] != c1)
            {
                first = source.IndexOf(c0, ++first, limit - first);
                continue;
            }

            // Check the rest of "pattern" (starting with the 3rd character)
            var found = true;
            for (int j = 2; j < pattern.Length; j++)
                if (source[first + j] != pattern[j])
                {
                    found = false;
                    break;
                }

            // If the whole word was found, return its index, otherwise try again
            if (found) return first;
            first = source.IndexOf(c0, ++first, limit - first);
        }
        return -1;
    }

    public static bool IsMetachar(char ch)
        => Array.IndexOf(MetaChars, ch) >= 0;

    public static string Escape(string input)
    {
        var chars = input.ToArray();
        if ((Array.FindIndex(chars, ch => IsMetachar(ch)) is int i) && (-1 == i)) return input;
        var builder = new StringBuilder(input.Length * 3);
        var last = 0;
        while (true)
        {
            builder.Append(chars[last..i]);
            if (i >= chars.Length) break;
            var ch = chars[i++];
            last = i;
            builder.Append('\\');
            builder.Append(ch switch
            {
                '\n' => 'n',
                '\r' => 'r',
                '\t' => 't',
                '\f' => 'f',
                _ => ch,
            });

            var tail = chars[last..];
            if (-1 == (i = Array.FindIndex(tail, ch => IsMetachar(ch))))
            {
                builder.Append(tail);
                break;
            }
            else
            {
                i += last;
            }
        }
        return builder.ToString();
    }

    public static string Unescape(string input)
    {
        var chars = input.ToArray();
        if ((Array.IndexOf(chars, '\\') is int i) && (-1 == i)) return input;
        var last = 0;
        var builder = new StringBuilder(input.Length * 3);
        while (true)
        {
            builder.Append(chars[last..i]);
            if (i == chars.Length) break;
            var ch = chars[last = ++i];
            builder.Append(ch switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'f' => '\f',
                _ => ch,
            });

            last = ch == '\\' ? last + 2 : last + 1;
            var tail = chars[last..];
            if (-1 == (i = Array.IndexOf(tail, '\\')))
            {
                builder.Append(tail);
                break;
            }
            else
            {
                i += last;
            }
        }
        return builder.ToString();
    }
}
