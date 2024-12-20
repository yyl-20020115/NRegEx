﻿/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

using System.Text;

namespace NRegEx;

public class LinkedNode(Node Node, LinkedNode? Previous = null)
{
    public readonly Node Node = Node;
    public LinkedNode? Previous = Previous;

    public override string ToString()
        =>this.Node?.ToString()??string.Empty;
}
public class Path
{
    public LinkedNode? ListTail = null;
    public Node? Tail => this.ListTail?.Node;
    protected int length = 0;
    protected int hash = 0;
    protected bool isCircle = false;

    public int Length => this.length;
    public bool IsCircle => this.isCircle;
    public Path(params Node[] nodes)
        :this(nodes.Select(n=>new LinkedNode(n)).ToList()) { }
    protected Path(List<LinkedNode> reversed_list, bool isCircle = false)
    {
        if (reversed_list.Count > 0)
        {
            for (int i = 0; i < reversed_list.Count; i++)
            {
                var node = reversed_list[i].Node;
                this.hash ^= node.GetHashCode();
                this.hash *= 31;
                reversed_list[i].Previous = i == 0 ? null : reversed_list[i - 1];
            }
            this.ListTail = reversed_list[^1];
            this.length = reversed_list.Count;
        }
        this.isCircle = isCircle;
    }

    protected Path(Path path, Node node):
        this(node)
    {
        if(node != null && this.ListTail != null)
        {
            this.ListTail.Previous = path.ListTail;
            this.length += path.Length;
        }
    }
    public bool IsEmpty 
        => this.Length == 0
        ;

    public Node? End 
        => !this.IsEmpty 
        ? this.ListTail?.Node 
        : null
        ;

    public LinkedNode? Find(Node node)
    {
        var list = this.ListTail;
        while (list != null)
        {
            if (list.Node == node)
                return list;
            list = list.Previous;
        }
        return null;
    }
    public Path CopyWith(Node node) 
        => this.Create(this, node);
    public Path CopyUntil(LinkedNode target, bool isCircle = true)
    {
        if (target == null)
        {
            return this;
        }
        else
        {
            var path = this;
            var nodes = new List<LinkedNode>();
            var list = this.ListTail;
            while (list != null)
            {
                nodes.Add(new(list.Node));
                if (list == target)
                {
                    nodes.Reverse();
                    path = this.Create(nodes, isCircle);
                    break;
                }
                list = list.Previous;
            }

            return path;
        }
    }
    public List<Node> ComposeNodesList()
        => this.NodesReversed.Reverse().ToList();

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

    public override int GetHashCode() 
        => this.hash;
    public override string ToString()
    {
        var builder = new StringBuilder();
        var first = true;
        var list = this.ListTail;
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

    protected virtual Path Create(List<LinkedNode> reversed_list, bool isCircle = false) 
        => new (reversed_list, isCircle);
    protected virtual Path Create(Path path, Node node)
        => new (path, node);
}
