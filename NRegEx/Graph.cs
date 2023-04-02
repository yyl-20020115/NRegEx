/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;
public class Graph
{
    protected static int Gid = 0;

    protected int id = Gid++;
    public int Id => id;
    public int SetId(int id) => this.id = id;
    public readonly string Name;

    public readonly HashSet<Node> Nodes = new();
    public readonly HashSet<Edge> Edges = new();
    public Node Head;
    public Node Tail;
    public RegExNode? SourceNode = null;
    public readonly List<int> BackReferences = new();
    public readonly List<Graph> ReferenceGraphs = new();
    public Graph(string name = "", params int[] cs)
    {
        this.Name = name;
        if (cs.Length > 0)
        {
            this.Nodes.Add(this.Head
                = this.Tail
                = new(cs) { Parent = this });
        }
        else
        {
            this.Nodes.Add(this.Head = new(name) { Parent = this });
            this.Nodes.Add(this.Tail = new(name) { Parent = this });
        }
    }
    public Graph Copy()
    {
        var graph = new Graph(this.Name)
        { SourceNode = SourceNode };
        foreach (var node in this.Nodes)
        {
            graph.Nodes.Add(node.Copy(graph));
        }
        graph.Head = graph.Nodes.First(n => n.Id == this.Head.Id);
        graph.Tail = graph.Nodes.First(n => n.Id == this.Tail.Id);
        foreach (var edge in this.Edges)
        {
            graph.Edges.Add(new(
                graph.Nodes.First(n => n.Id == edge.Head.Id),
                graph.Nodes.First(n => n.Id == edge.Tail.Id)
                ));
        }

        return graph;
    }
    public Graph TryComplete()
    {
        if (this.Edges.Count == 0)
            this.Edges.Add(new(this.Head, this.Tail));
        return this;
    }
    public Graph ComposeLiteral(params Node[] sequence)
        => this.ComposeLiteral(sequence as IEnumerable<Node>);

    public Graph ComposeLiteral(IEnumerable<Node> sequence)
        => this.ComposeLiteral(sequence.ToList());

    public Graph ComposeLiteral(List<Node> sequence)
    {
        for (int i = 0; i < sequence.Count; i++)
        {
            var before = i <= 0 ? this.Head : sequence[i - 1];
            var current = sequence[i];
            var after = i >= sequence.Count - 1 ? this.Tail : sequence[i + 1];
            this.Edges.Add(new(before, current));
            this.Edges.Add(new(current, after));
            this.Nodes.Add(current);
        }
        this.Head.Ending |= Endings.Start;
        this.Tail.Ending |= Endings.End;
        return this;
    }
    public Graph Concate(IEnumerable<Graph> graphs, bool plus = false)
        => this.Concate(graphs.ToList(), plus);
    public Graph Concate(params Graph[] graphs)
        => this.Concate(graphs.ToList());
    public Graph Concate(List<Graph> graphs, bool plus = false)
    {
        if (graphs.Count == 0)
        {
            return this;
        }
        else if (graphs.Count == 1)
        {
            this.Edges.Clear();
            this.Nodes.Clear();
            this.Edges.UnionWith(graphs[0].Edges);
            this.Nodes.UnionWith(graphs[0].Nodes);
            this.Head = graphs[0].Head;
            this.Tail = graphs[0].Tail;
        }
        else if (graphs.Count == 2)
        {
            this.Nodes.Clear();
            this.Edges.Clear();

            this.Head = graphs[0].Head;
            this.Tail = graphs[^1].Tail;
            this.Nodes.UnionWith(graphs[0].Nodes);
            this.Nodes.UnionWith(graphs[^1].Nodes);
            this.Edges.UnionWith(graphs[0].Edges);
            this.Edges.UnionWith(graphs[^1].Edges);
            this.Edges.Add(new(graphs[0].Tail, graphs[^1].Head));
        }
        else
        {
            this.Nodes.Clear();
            this.Edges.Clear();

            this.Head = graphs[0].Head;
            this.Tail = graphs[^1].Tail;
            this.Nodes.UnionWith(graphs[0].Nodes);
            this.Nodes.UnionWith(graphs[^1].Nodes);
            this.Edges.UnionWith(graphs[0].Edges);
            this.Edges.UnionWith(graphs[^1].Edges);

            for (int i = 1; i < graphs.Count - 1; i++)
            {
                var before = graphs[i - 1];
                var current = graphs[i + 0];
                var after = graphs[i + 1];
                this.Nodes.UnionWith(current.Nodes);
                this.Edges.UnionWith(current.Edges);
                this.Edges.Add(new(before.Tail, current.Head));
                this.Edges.Add(new(current.Tail, after.Head));
            }
        }
        var starts = new HashSet<Node>();
        var ends = new HashSet<Node>();
        this.Head.FetchNodes(starts, true, 1);
        this.Tail.FetchNodes(ends, true, -1);

        foreach (var s in starts) s.Ending |= Endings.Start;
        foreach (var e in ends) e.Ending |= Endings.End;

        if (plus)
            this.Edges.Add(new(graphs[^1].Tail, graphs[^1].Head));

        return this;
    }

    public Graph UnionWith(params Graph[] gs)
        => this.UnionWith(gs as IEnumerable<Graph>);
    public Graph UnionWith(IEnumerable<Graph> gs)
    {
        foreach (var g in gs) this.UnionWith(g);
        return this;
    }
    public Graph UnionWith(params Node[] nodes)
        => this.UnionWith(nodes as IEnumerable<Node>);
    public Graph UnionWith(IEnumerable<Node> nodes)
        => this.UnionWith(nodes.ToList());
    public Graph UnionWith(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            this.Nodes.Add(node);
            this.Edges.Add(new Edge(this.Head, node));
            this.Edges.Add(new Edge(node, this.Tail));
        }
        return this;
    }
    public Graph UnionWith(Graph g)
    {
        if (g != null)
        {
            this.Edges.Add(new(this.Head, g.Head));
            this.Edges.Add(new(g.Tail, this.Tail));

            this.Edges.UnionWith(g.Edges);
            this.Nodes.UnionWith(g.Nodes);
        }
        return this;
    }
    public Graph GroupWith(Graph g, int i)
    {
        this.UnionWith(g);
        foreach (var node in this.Nodes)
            node.Groups.Add(i);

        return this;
    }
    public Graph GroupWith(Node node, int i)
    {
        this.UnionWith(node);
        foreach (var _node in this.Nodes)
            _node.Groups.Add(i);
        return this;
    }
    public Graph BackReferenceWith(Graph g, int i)
    {
        this.UnionWith(g);

        return this;
    }
    public Graph ZeroPlus(Graph g, HashSet<Node>? loopset = null)
    {
        this.Edges.Add(new(this.Head, this.Tail)); //direct pass
        this.Edges.Add(new(g.Tail, g.Head)); //back link
        loopset?.UnionWith(this.Nodes);
        loopset?.UnionWith(g.Nodes);
        return this.EmbedOne(g);
    }
    public Graph OnePlus(Graph g, HashSet<Node>? loopset = null)
    {
        this.Edges.Add(new(g.Tail, g.Head)); //back link
        loopset?.UnionWith(this.Nodes);
        loopset?.UnionWith(g.Nodes);
        return this.EmbedOne(g);
    }
    public Graph ZeroOne(Graph g)
    {
        this.Edges.Add(new(this.Head, this.Tail)); //direct pass
        return this.EmbedOne(g);
    }
    public Graph EmbedOne(Graph g)
    {
        this.Edges.Add(new(this.Head, g.Head));
        this.Edges.Add(new(g.Tail, this.Tail));
        this.Edges.UnionWith(g.Edges);
        this.Nodes.UnionWith(g.Nodes);
        return this;
    }

    public Graph ComposeRepeats(Graph graph, int min, int max)
    {
        var plus = max < 0;

        min = min <= 0 ? 0 : min;
        max = max <= 0 ? min : max;

        var all = new List<Graph>();
        var others = new List<Graph>();
        for (int i = 0; i < max; i++)
        {
            var g = graph.Copy();
            all.Add(g);
            if (i >= min) others.Add(g);
        }

        this.Concate(all, plus);

        for (int i = 0; i < others.Count; i++)
        {
            this.Edges.Add(new(others[i].Head, this.Tail));
        }

        return this;
    }

    public override string ToString() => $"H:{this.Head},T:{this.Tail}";

    public Node InsertPointBeforeTail(Node node)
    {
        this.Edges.RemoveWhere(e => this.Tail.Inputs.Contains(e.Head) && e.Tail == this.Tail);

        foreach (var pre in this.Tail.Inputs)
        {
            pre.Outputs.Remove(this.Tail);
            this.Edges.Add(new(pre, node));
        }
        this.Tail.Inputs.Clear();
        this.Edges.Add(new(node, this.Tail));
        this.Nodes.Add(node);

        return node;
    }

    public Graph RestoreNode(Node node)
    {
        this.Nodes.RemoveWhere(n => n != this.Head && n != this.Tail);
        this.Edges.Clear();
        this.Edges.Add(new(this.Head, node));
        this.Edges.Add(new(node, this.Tail));
        this.Nodes.Add(node);

        return this;
    }

    public Graph Clean(IEnumerable<Node> nodes)
    {
        var set = nodes.ToHashSet();
        foreach (var n in this.Nodes)
        {
            n.Inputs.ExceptWith(set);
            n.Outputs.ExceptWith(set);
        }

        this.Edges.RemoveWhere(
            e => nodes.Contains(e.Head)
                || nodes.Contains(e.Tail));
        this.Nodes.ExceptWith(set);
        return this;
    }
    public Graph Clean()
    {
        this.Nodes.RemoveWhere(n => n.IsBroken);
        return this;
    }

    public Graph RemoveEdges(IEnumerable<Edge> edges)
    {
        foreach (var edge in edges)
        {
            edge.Head.Outputs.Remove(edge.Tail);
            edge.Tail.Inputs.Remove(edge.Head);
            this.Edges.Remove(edge);
        }

        return this;
    }
}
