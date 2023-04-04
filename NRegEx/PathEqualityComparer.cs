/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Diagnostics.CodeAnalysis;

namespace NRegEx;

public class PathEqualityComparer : IEqualityComparer<Path>
{
    public static bool SequenceEquals(IEnumerable<Node> nodes1, IEnumerable<Node> nodes2)
    {
        if (nodes1 == null || nodes2 == null) return true;
        var e1 = nodes1.GetEnumerator();
        var e2 = nodes2.GetEnumerator();
        var b1 = false;
        var b2 = false;
        while (true)
        {
            b1 = e1.MoveNext();
            b2 = e2.MoveNext();
            if (!b1 || !b2) break;
            if (e1.Current.Id != e2.Current.Id) return false;
        }
        return b1 == b2;
    }
    public bool Equals(Path? x, Path? y)
        => (x == null && y == null) || (x != null && y != null
        && SequenceEquals(x.NodesReversed, y.NodesReversed));

    public int GetHashCode([DisallowNull] Path _) => 0;
}
