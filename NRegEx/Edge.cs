/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Edge
{
    public readonly Node Head;
    public readonly Node Tail;
    public readonly int? MinRepeats;
    public readonly int? MaxRepeats;
    public Edge(Node Head, Node Tail,int? MinRepeats = null, int? MaxRepeats = null)
    {
        this.Head = Head;
        this.Tail = Tail;
        this.MinRepeats = MinRepeats;
        this.MaxRepeats = MaxRepeats;
        this.Head.Outputs.Add(Tail);
        this.Tail.Inputs.Add(Head);
    }
    public override int GetHashCode() 
        => Head.GetHashCode() ^ Tail.GetHashCode();
    public override bool Equals(object? o)
        => o is Edge e 
        ? this.Head == e.Head && this.Tail == e.Tail 
        : base.Equals(o);

    public override string ToString() 
        => $"[{this.Head}->{this.Tail}]";
}
