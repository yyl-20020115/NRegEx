/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Diagnostics.CodeAnalysis;

namespace NRegEx;

public class Path
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
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return SequenceEquals(x.Nodes,y.Nodes);
        }

        public int GetHashCode([DisallowNull] Path obj) => 0;
    }
    public readonly List<Node> Nodes = new();
    public readonly HashSet<Node> NodeSet = new();
    public Path(params Node[] nodes) => this.AddNodes(nodes);

    protected Path(List<Node> nodes, HashSet<Node> internalNodeSet, params Node[] ns)
    {
        Nodes = nodes;
        Nodes.AddRange(ns);
        NodeSet = internalNodeSet;
        NodeSet.UnionWith(ns);
    }
    public int Length => this.Nodes.Count;
    public bool IsEmpty => this.Length == 0;

    public Node? Start => !this.IsEmpty ? this.Nodes[0] : null;
    public Node? End => !this.IsEmpty ? this.Nodes[^1] : null;

    public Path AddNodes(params Node[] nodes) 
        => AddNodes(nodes as IEnumerable<Node>);
    public Path AddNodes(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
        {
            this.Nodes.Add(node);
            this.NodeSet.Add(node);
        }
        return this;
    }
    public Path CutTo(int index)
    {
        this.Nodes.RemoveRange(0, index);
        return this;
    }
    public int IndexOf(Node node) => Nodes.IndexOf(node);
    public Path CopyWith(params Node[] ns)=> new(
            this.Nodes.ToList(),
            this.NodeSet.ToHashSet(), ns);
    public bool HasPathTo(Path path, bool original = true)
    {
        foreach (var node in this.Nodes)
        {
            var targets = new HashSet<Node>();
            node.FetchNodes(targets, true);
            if (original)
                targets.Add(node); //count self

            if (targets.Overlaps(path.NodeSet))
                return true;

        }
        return false;
    }
    public override string ToString() 
        => string.Join("->", this.Nodes.Select(n=>n.Id).ToArray());
}
