/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/parse.go

/**
 * A "builder"-style helper class for manipulating character classes represented as an array of
 * pairs of runes [lo, hi], each denoting an inclusive interval.
 *
 * All methods mutate the internal state and return {@code this}, allowing operations to be chained.
 */
using System.Text;

namespace NRegEx;
public class CharClass
{
    private int[] range; // inclusive ranges, pairs of [lo,hi].  r.Length is even.
    private int length; // prefix of |r| that is defined.  Even.

    // Constructs a CharClass with initial ranges |r|.
    // The right to mutate |r| is passed to the callee.
    public CharClass(int[]? r = null)
    {
        this.range = r ?? Utils.EMPTY_INTS;
        this.length = this.range.Length;
    }


    // After a call to ensureCapacity(), |r.Length| is at least |newLen|.
    private void EnsureCapacity(int newLen)
    {
        if (range.Length < newLen)
        {
            // Expand by at least doubling, except when len == 0.
            // TODO(adonovan): opt: perhaps it would be better to allocate exactly
            // newLen, since the number of expansions is typically very small?
            if (newLen < length * 2)
            {
                newLen = length * 2;
            }
            int[] r2 = new int[newLen];
            Array.Copy(range, 0, r2, 0, length);
            range = r2;
        }
    }

    // Returns the character class as an int array.  Subsequent CharClass
    // operations may mutate this array, so typically this is the last operation
    // performed on a given CharClass instance.
    public int[] ToArray()
    {
        var ret = new int[this.length];
        Array.Copy(range, 0, ret, 0, length);
        return ret;
    }

    // cleanClass() sorts the ranges (pairs of elements) of this CharClass,
    // merges them, and eliminates duplicates.
    public CharClass CleanClass()
    {
        if (length < 4)
        {
            return this;
        }

        // Sort by lo increasing, hi decreasing to break ties.
        QSortIntPair(range, 0, length - 2);

        // Merge abutting, overlapping.
        int w = 2; // write index
        for (int i = 2; i < length; i += 2)
        {
            int lo = range[i];
            int hi = range[i + 1];
            if (lo <= range[w - 1] + 1)
            {
                // merge with previous range
                if (hi > range[w - 1])
                {
                    range[w - 1] = hi;
                }
                continue;
            }
            // new disjoint range
            range[w] = lo;
            range[w + 1] = hi;
            w += 2;
        }
        length = w;

        return this;
    }

    // appendLiteral() appends the literal |x| to this CharClass.
    public CharClass AppendLiteral(int x, RegExParserOptions flags)
        => ((flags & RegExParserOptions.FOLD_CASE) != 0) ? AppendFoldedRange(x, x) : AppendRange(x, x);

    // appendRange() appends the range [lo-hi] (inclusive) to this CharClass.
    public CharClass AppendRange(int lo, int hi)
    {
        // Expand last range or next to last range if it overlaps or abuts.
        // Checking two ranges helps when appending case-folded
        // alphabets, so that one range can be expanding A-Z and the
        // other expanding a-z.
        if (length > 0)
        {
            for (int i = 2; i <= 4; i += 2)
            { // twice, using i=2, i=4
                if (length >= i)
                {
                    int rlo = range[length - i];
                    int rhi = range[length - i + 1];
                    if (lo <= rhi + 1 && rlo <= hi + 1)
                    {
                        if (lo < rlo)
                        {
                            range[length - i] = lo;
                        }
                        if (hi > rhi)
                        {
                            range[length - i + 1] = hi;
                        }
                        return this;
                    }
                }
            }
        }
        // Can't coalesce; append.   Expand capacity by doubling as needed.
        EnsureCapacity(length + 2);
        range[length++] = lo;
        range[length++] = hi;
        return this;
    }

    // appendFoldedRange() appends the range [lo-hi] and its case
    // folding-equivalent runes to this CharClass.
    public CharClass AppendFoldedRange(int lo, int hi)
    {
        // Optimizations.
        if (lo <= Unicode.MIN_FOLD && hi >= Unicode.MAX_FOLD)
        {
            // Range is full: folding can't add more.
            return AppendRange(lo, hi);
        }
        if (hi < Unicode.MIN_FOLD || lo > Unicode.MAX_FOLD)
        {
            // Range is outside folding possibilities.
            return AppendRange(lo, hi);
        }
        if (lo < Unicode.MIN_FOLD)
        {
            // [lo, minFold-1] needs no folding.
            AppendRange(lo, Unicode.MIN_FOLD - 1);
            lo = Unicode.MIN_FOLD;
        }
        if (hi > Unicode.MAX_FOLD)
        {
            // [maxFold+1, hi] needs no folding.
            AppendRange(Unicode.MAX_FOLD + 1, hi);
            hi = Unicode.MAX_FOLD;
        }
        var visited = new HashSet<int>();
        // Brute force.  Depend on appendRange to coalesce ranges on the fly.
        for (int c = lo; c <= hi; c++)
        {
            AppendRange(c, c);
            for (int f = Unicode.SimpleFold(c); f != c; f = Unicode.SimpleFold(f))
            {
                if (!visited.Add(f)) break;
                AppendRange(f, f);
            }
        }
        return this;
    }

    // appendClass() appends the class |x| to this CharClass.
    // It assumes |x| is clean.  Does not mutate |x|.
    public CharClass AppendClass(int[] x)
    {
        for (int i = 0; i < x.Length; i += 2)
        {
            AppendRange(x[i], x[i + 1]);
        }
        return this;
    }

    // appendFoldedClass() appends the case folding of the class |x| to this
    // CharClass.  Does not mutate |x|.
    public CharClass AppendFoldedClass(int[] x)
    {
        for (int i = 0; i < x.Length; i += 2)
        {
            AppendFoldedRange(x[i], x[i + 1]);
        }
        return this;
    }

    // appendNegatedClass() append the negation of the class |x| to this
    // CharClass.  It assumes |x| is clean.  Does not mutate |x|.
    public CharClass AppendNegatedClass(int[] x)
    {
        int nextLo = 0;
        for (int i = 0; i < x.Length; i += 2)
        {
            int lo = x[i];
            int hi = x[i + 1];
            if (nextLo <= lo - 1)
            {
                AppendRange(nextLo, lo - 1);
            }
            nextLo = hi + 1;
        }
        if (nextLo <= Unicode.MAX_RUNE)
        {
            AppendRange(nextLo, Unicode.MAX_RUNE);
        }
        return this;
    }

    // appendTable() appends the Unicode range table |table| to this CharClass.
    // Does not mutate |table|.
    public CharClass AppendTable(int[][] table)
    {
        foreach (int[] triple in table)
        {
            int lo = triple[0], hi = triple[1], stride = triple[2];
            if (stride == 1)
            {
                AppendRange(lo, hi);
                continue;
            }
            for (int c = lo; c <= hi; c += stride)
            {
                AppendRange(c, c);
            }
        }
        return this;
    }

    // appendNegatedTable() returns the result of appending the negation of range
    // table |table| to this CharClass.  Does not mutate |table|.
    public CharClass AppendNegatedTable(int[][] table)
    {
        int nextLo = 0; // lo end of next class to add
        foreach (int[] triple in table)
        {
            int lo = triple[0], hi = triple[1], stride = triple[2];
            if (stride == 1)
            {
                if (nextLo <= lo - 1)
                {
                    AppendRange(nextLo, lo - 1);
                }
                nextLo = hi + 1;
                continue;
            }
            for (int c = lo; c <= hi; c += stride)
            {
                if (nextLo <= c - 1)
                {
                    AppendRange(nextLo, c - 1);
                }
                nextLo = c + 1;
            }
        }
        if (nextLo <= Unicode.MAX_RUNE)
        {
            AppendRange(nextLo, Unicode.MAX_RUNE);
        }
        return this;
    }

    // appendTableWithSign() calls append{,Negated}Table depending on sign.
    // Does not mutate |table|.
    public CharClass AppendTableWithSign(int[][] table, int sign)
    {
        return sign < 0 ? AppendNegatedTable(table) : AppendTable(table);
    }

    // negateClass() negates this CharClass, which must already be clean.
    public CharClass NegateClass()
    {
        int nextLo = 0; // lo end of next class to add
        int w = 0; // write index
        for (int i = 0; i < length; i += 2)
        {
            int lo = range[i], hi = range[i + 1];
            if (nextLo <= lo - 1)
            {
                range[w] = nextLo;
                range[w + 1] = lo - 1;
                w += 2;
            }
            nextLo = hi + 1;
        }
        length = w;

        if (nextLo <= Unicode.MAX_RUNE)
        {
            // It's possible for the negation to have one more
            // range - this one - than the original class, so use append.
            EnsureCapacity(length + 2);
            range[length++] = nextLo;
            range[length++] = Unicode.MAX_RUNE;
        }
        return this;
    }

    // appendClassWithSign() calls appendClass() if sign is +1 or
    // appendNegatedClass if sign is -1.  Does not mutate |x|.
    public CharClass AppendClassWithSign(int[] x, int sign) 
        => sign < 0 ? AppendNegatedClass(x) : AppendClass(x);

    // appendGroup() appends CharGroup |g| to this CharClass, folding iff
    // |foldCase|.  Does not mutate |g|.
    public CharClass AppendGroup(CharGroup g, bool foldCase)
    {
        var cls = g.Class;
        if (foldCase)
        {
            cls = new CharClass().AppendFoldedClass(cls).CleanClass().ToArray();
        }
        return AppendClassWithSign(cls, g.Sign);
    }

    // cmp() returns the ordering of the pair (a[i], a[i+1]) relative to
    // (pivotFrom, pivotTo), where the first component of the pair (lo) is
    // ordered naturally and the second component (hi) is in reverse order.
    private static int Cmp(int[] array, int i, int pivotFrom, int pivotTo)
    {
        int cmp = array[i] - pivotFrom;
        return cmp != 0 ? cmp : pivotTo - array[i + 1];
    }

    // qsortIntPair() quicksorts pairs of ints in |array| according to lt().
    // Precondition: |left|, |right|, |this.len| must all be even; |this.len > 1|.
    private static void QSortIntPair(int[] array, int left, int right)
    {
        int pivotIndex = ((left + right) / 2) & ~1;
        int pivotFrom = array[pivotIndex], pivotTo = array[pivotIndex + 1];
        int i = left, j = right;

        while (i <= j)
        {
            while (i < right && Cmp(array, i, pivotFrom, pivotTo) < 0)
            {
                i += 2;
            }
            while (j > left && Cmp(array, j, pivotFrom, pivotTo) > 0)
            {
                j -= 2;
            }
            if (i <= j)
            {
                if (i != j)
                {
                    (array[j], array[i]) = (array[i], array[j]);
                    (array[j + 1], array[i + 1]) = (array[i + 1], array[j + 1]);
                }
                i += 2;
                j -= 2;
            }
        }
        if (left < j)
        {
            QSortIntPair(array, left, j);
        }
        if (i < right)
        {
            QSortIntPair(array, i, right);
        }
    }

    // Exposed, since useful for debugging CharGroups too.
    public static string CharClassToString(int[] r, int len)
    {
        var builder = new StringBuilder();
        builder.Append('[');
        for (int i = 0; i < len; i += 2)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }
            int lo = r[i], hi = r[i + 1];
            // Avoid string.format (not available on GWT).
            // Cf. https://code.google.com/p/google-web-toolkit/issues/detail?id=3945
            if (lo == hi)
            {
                builder.Append($"0x{lo:x}");
            }
            else
            {
                builder.Append($"0x{lo:x}-0x{hi:x}");
            }
        }
        builder.Append(']');
        return builder.ToString();
    }

    public override string ToString()
        => CharClassToString(range, length);
}