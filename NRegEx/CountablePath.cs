/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

namespace NRegEx;

public class CountablePath : Path
{
    public int? MinRepeats;
    public int? MaxRepeats;
    public Edge? CountableEdge;
    public CountablePath(params Node[] nodes)
        : base(nodes) { }

    protected CountablePath(List<LinkedNode> reversed_list, bool isCircle = false)
        : base(reversed_list) { }

    protected CountablePath(Path path, Node node)
        : base(path, node) { }
    /// <summary>
    /// call this function to make MinRepeats-- and MaxRepeats--
    /// and if 0, return false
    /// </summary>
    /// <returns>true if having another try</returns>
    public bool TryPassingOnceAndClear()
    {
        var again = true;
        if (this.CountableEdge == null || !this.MinRepeats.HasValue && !this.MaxRepeats.HasValue)
            return again = false;

        if(!this.MinRepeats.HasValue && this.MaxRepeats.HasValue)
        {
            this.MinRepeats = 0;
        }
        if (this.MinRepeats.HasValue)
        {
            this.MinRepeats = this.MinRepeats.Value - 1;
            if (this.MaxRepeats.HasValue)
            {
                this.MaxRepeats = this.MaxRepeats.Value - 1;
                if (this.MaxRepeats.Value <= 0)
                    again = false;
            }
            if (this.MinRepeats.Value <= 0)
                again = false;
        }
        if (!again)
        {
            this.CountableEdge = null;
            this.MinRepeats = null;
            this.MaxRepeats = null;
        }
        return again;
    }
    public bool IsUncompleted => this.MinRepeats.HasValue && this.MinRepeats.Value > 0;
    protected override Path Create(List<LinkedNode> reversed_list, bool isCircle = false)
    {
        var cp = new CountablePath(reversed_list, isCircle)
        {
            CountableEdge = CountableEdge,
            MinRepeats = this.MinRepeats,
            MaxRepeats = this.MaxRepeats
        };
        return cp;
    }
    protected override Path Create(Path path, Node node)
    {
        var cp = new CountablePath(path, node)
        {
            CountableEdge = CountableEdge,
            MinRepeats = this.MinRepeats,
            MaxRepeats = this.MaxRepeats
        };
        return cp;
    }
}