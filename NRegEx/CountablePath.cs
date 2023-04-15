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
        :base(nodes)
    {

    }
    protected CountablePath(List<LinkedNode> reversed_list, bool isCircle = false)
        :base(reversed_list)
    {

    }

    protected CountablePath(Path path, Node node)
        :base(path,node)
    {


    }
    /// <summary>
    /// call this function to make MinRepeats-- and MaxRepeats--
    /// and if 0, return false
    /// </summary>
    /// <returns></returns>
    public bool TryPassingOnce()
    {

        if (!this.MinRepeats.HasValue && !this.MaxRepeats.HasValue)
            return false;

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
                    return false;
            }
            if (this.MinRepeats.Value <= 0)
                return false;
        }
        return true;
    }
    protected override Path Create(List<LinkedNode> reversed_list, bool isCircle = false)
    {
        var cp = new CountablePath(reversed_list, isCircle);
        cp.CountableEdge = CountableEdge;
        cp.MinRepeats = this.MinRepeats;
        cp.MaxRepeats = this.MaxRepeats;
        return cp;
    }
    protected override Path Create(Path path, Node node)
    {
        var cp = new CountablePath(path, node);
        cp.CountableEdge = CountableEdge;
        cp.MinRepeats = this.MinRepeats;
        cp.MaxRepeats = this.MaxRepeats;
        return cp;
    }
}