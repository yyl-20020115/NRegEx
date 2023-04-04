/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Diagnostics.CodeAnalysis;

namespace NRegEx;

public partial class Path
{
    public class PathEqualityComparer : IEqualityComparer<Path>
    {
        public static bool SequenceEquals(List<Node> nodes1, List<Node> nodes2)
        {
            if (nodes1 == null || nodes2 == null) return true;
            if (nodes1.Count != nodes2.Count) return false;
            for (int i = 0; i < nodes1.Count; i++)
                if (nodes1[i].Id != nodes2[i].Id) return false;
            return true;
        }
        public bool Equals(Path? x, Path? y)
            => (x == null && y == null) || (x != null && y != null && SequenceEquals(x.Nodes, y.Nodes));

        public int GetHashCode([DisallowNull] Path _) => 0;
    }
}
