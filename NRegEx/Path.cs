/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

namespace NRegEx;

public partial class Path
{
    public readonly List<Node> Nodes = new();
    public readonly HashSet<Node> NodeSet = new();
    public Path(params Node[] nodes) => this.AddNodes(nodes);

    protected Path(List<Node> nodes, HashSet<Node> nodeSet, params Node[] others)
    {
        Nodes.AddRange(nodes);
        Nodes.AddRange(others);
        NodeSet.UnionWith(nodeSet);
        NodeSet.UnionWith(others);
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
    public Path CutFrom(int index)
    {
        this.Nodes.RemoveRange(0, index);
        if (index > 0)
        {
            this.NodeSet.Clear();
            this.NodeSet.UnionWith(this.Nodes);
        }
        return this;
    }
    public int IndexOf(Node node) 
        => Nodes.IndexOf(node);
    public Path Copy(params Node[] ns)=> new(
            this.Nodes,
            this.NodeSet, ns);
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
