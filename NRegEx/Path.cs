/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class Path
{
    public readonly List<Node> Nodes = new();
    public readonly HashSet<Node> InternalNodeSet = new();
    public Path(params Node[] nodes) => this.AddNodes(nodes);

    protected Path(List<Node> nodes, HashSet<Node> internalNodeSet)
    {
        Nodes = nodes;
        InternalNodeSet = internalNodeSet;
    }
    public int Length => this.Nodes.Count;
    public bool IsEmpty => this.Nodes.Count == 0;

    public Node? Start => !this.IsEmpty ? this.Nodes[0] : null;
    public Node? End => !this.IsEmpty ? this.Nodes[^1] : null;

    public Path AddNodes(params Node[] nodes) => AddNodes(nodes as IEnumerable<Node>);
    public Path AddNodes(IEnumerable<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            this.Nodes.Add(node);
            this.InternalNodeSet.Add(node);
        }
        return this;
    }
    public bool Contains(Node node) => InternalNodeSet.Contains(node);

    public Path Copy() => new(
            this.Nodes.ToList(),
            this.InternalNodeSet.ToHashSet());

    public bool HasPathTo(Path path, bool original = true)
    {
        foreach (var node in this.Nodes)
        {
            var targets = new HashSet<Node>();
            node.FetchNodes(targets, true);
            if (original)
                targets.Add(node); //count self

            if (targets.Overlaps(path.InternalNodeSet))
                return true;

        }
        return false;
    }
}
