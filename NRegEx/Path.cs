/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

using System.Text;

namespace NRegEx;

public class LinkedNode
{
    public readonly Node? Node;
    public LinkedNode? Previous;
    public LinkedNode(Node? Node, LinkedNode? Previous = null)
    {
        this.Node = Node;
        this.Previous = Previous;
    }
    public override string ToString()
        =>this.Node?.ToString()??string.Empty;
}
public class Path
{
    //public readonly List<Node> Nodes = new();

    public LinkedNode? ListTail = null;

    public readonly HashSet<Node> NodeSet = new();
    protected int length = 0;
    protected bool isCircle = false;

    public int Length => this.length;
    public bool IsCircle => this.isCircle;
    public Path(params Node[] nodes)
        :this(nodes.Select(n=>new LinkedNode(n)).ToList())
    {
    }
    public Path(List<LinkedNode> reversed_list, bool isCircle = false)
    {
        if (reversed_list.Count > 0)
        {
            for (int i = 0; i < reversed_list.Count; i++)
            {
                var node = reversed_list[i].Node;
                if(node is not null)
                    this.NodeSet.Add(node);
                reversed_list[i].Previous = i == 0 ? null : reversed_list[i - 1];
            }
            this.ListTail = reversed_list[^1];
            this.length = reversed_list.Count;
        }
        this.isCircle = isCircle;
    }
    public Path(Path path, Node node):
        this(node)
    {
        if(node is not null && this.ListTail is not null)
        {
            this.ListTail.Previous = path.ListTail;
            this.length += path.Length;
        }
    }
    public bool IsEmpty => this.Length == 0;

    public Node? End => !this.IsEmpty ? this.ListTail?.Node : null;


    public LinkedNode? Find(Node node)
    {
        LinkedNode? ln = this.ListTail;
        while (ln is not null)
        {
            if (ln.Node == node)
                return ln;
            ln = ln.Previous;
        }
        return null;
    }
    public Path Copy(Node node) 
        => new(this, node);
    public Path CopyAndCut(LinkedNode target, bool isCircle = true)
    {
        if (target is null)
        {
            return new (this.LinkedNodesReversed.ToList(), isCircle);
        }
        else
        {
            var path = this;
            var nodes = new List<LinkedNode>();
            var list = this.ListTail;
            while (list != null)
            {
                if (list == target)
                {
                    path = new(nodes, isCircle);
                    break;
                }
                else
                {
                    nodes.Add(new(list.Node));
                }
                list = list.Previous;
            }

            this.NodeSet.Clear();
            this.NodeSet.UnionWith(this.NodesReversed);
            return path;
        }
    }
    public IEnumerable<Node> NodesReversed
    {
        get
        {
            var list = this.ListTail;
            while (list != null)
            {
                var node = list.Node;
                if (node != null)
                    yield return node;
                list = list.Previous;
            }
        }
    }
    public IEnumerable<LinkedNode> LinkedNodesReversed
    {
        get
        {
            var node = this.ListTail;
            while (node != null)
            {
                yield return node;
                node = node.Previous;
            }
        }
    }
    public bool HasPathTo(Path path, bool original = true)
    {
        var list = this.ListTail;
        while(list!=null)
        {
            var node = list.Node;
            if (node != null)
            {
                var targets = new HashSet<Node>();
                node.FetchNodes(targets, true);
                if (original)
                    targets.Add(node); //count self

                if (targets.Overlaps(path.NodeSet))
                    return true;
            }
            list = list.Previous;
        }
        return false;
    }
    public override string ToString()
    {
        var builder = new StringBuilder();
        var list = this.ListTail;
        var first = true;
        while (list != null)
        {
            var node = list.Node;
            if (node != null)
            {
                if (!first) builder.Append("<-");
                builder.Append(node.Id);
                first = false;
            }
            list = list.Previous;
        }
        return builder.ToString();
    }
}
