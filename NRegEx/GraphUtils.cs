﻿using System.Text;

namespace NRegEx;

public static class GraphUtils
{
    public static Graph Compact(Graph graph, int minlength = 2)
    {
        var pairs = new List<(List<Node> nodes, List<Edge> edges)>();
        var bridges = new HashSet<Node>();
        foreach (var node in graph.Nodes)
        {
            if (bridges.Contains(node))
                continue;
            if (node.IsBridge)
            {
                var dots = new List<Node>();
                var stream = new List<Edge>();
                var next = node;
                var last = node;
                var edge = graph.Edges.Single(e => e.Tail == next);
                do
                {
                    dots.Add(next);
                    stream.Add(edge);
                    edge = graph.Edges.Single(e => e.Head == next);
                    last = next;
                    next = edge.Tail;
                } while (next != null && next.IsBridge);
                if (next != null && last != null && last != next)
                {
                    edge = graph.Edges.FirstOrDefault(
                        e => e.Head == last && e.Tail == next);
                    if (edge != null)
                        stream.Add(edge);
                }
                bridges.UnionWith(dots);
                pairs.Add((dots, stream));
            }
        }
        foreach (var (nodes, edges) in pairs)
        {
            if (nodes.Count >= minlength)
            {
                var first = edges[0];
                var last = edges[^1];
                graph.Edges.Add(new(first.Head, last.Tail));
                graph.Clean(nodes);
            }
        }
        return graph.Clean();
    }
    public static StringBuilder ExportAsDot(Graph graph, StringBuilder? builder = null)
    {
        builder ??= new StringBuilder();
        builder.AppendLine("digraph g {");
        foreach (var node in graph.Nodes.OrderBy(n => n.Id))
        {
            var label = (!string.IsNullOrEmpty(node.Name) ? $"[label=\"{node.Id}({node.Name})\"]" : "");
            builder.AppendLine($"\t{node.Id} {label};");
        }
        foreach (var edge in graph.Edges.OrderBy(e => e.Head.Id).OrderBy(e => e.Tail.Id))
        {
            builder.AppendLine($"\t{edge.Head.Id}->{edge.Tail.Id};");
        }
        builder.AppendLine("}");
        return builder;
    }

    public static HashSet<Node> GetFollowings(Graph graph, Node current, HashSet<Node> visited)
        => graph.Edges.Where(e => e.Head == current && visited.Add(e.Tail)).Select(e => e.Tail).ToHashSet();
    public static bool HasPassThrough(Graph graph, int direction = 1)
        => HasPassThrough(direction >= 0 ? graph.Tail : graph.Head, new Node[] { direction >= 1 ? graph.Head : graph.Tail });
    public static bool HasPassThrough(Graph graph, IEnumerable<Node> nodes, int direction = 1)
        => HasPassThrough((direction >= 0 ? graph.Tail : graph.Head), nodes, direction);
    public static bool HasPassThrough(Node target, IEnumerable<Node> nodes, int direction = 1)
        => HasPassThrough(target, nodes.ToHashSet(), direction);
    /// <summary>
    /// Check if there is a way to pass through the whole graph
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool HasPassThrough(Node target, HashSet<Node> nodes, int direction = 1)
    {
        var visited = new HashSet<Node>();
        do
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            foreach (var node in copies)
            {
                if (visited.Add(node))
                {
                    if (node == target)
                        return true;
                    else if (node.IsLink)
                        nodes.UnionWith(direction >= 0 ? node.Outputs : node.Inputs);
                }
            }
        } while (nodes.Count > 0);

        return false;
    }
    public static Graph Reform(Graph graph, int id = 0) => Reorder(Compact(graph), id);
    public static Graph Reorder(Graph graph, int id = 0)
    {

        var visited = new HashSet<Node>();
        var followings = GetFollowings(graph, graph.Head, visited);
        var list = new List<HashSet<Node>>
    {
        new() { graph.Head },
        followings
    };

        var collectings = new HashSet<Node>();
        do
        {
            foreach (var node in followings)
            {
                collectings.UnionWith(GetFollowings(graph, node, visited));
            }
            followings = collectings.ToHashSet();
            if (collectings.Count > 0)
            {
                list.Add(collectings);
                collectings = new();
            }
        } while (followings.Count > 0);
        foreach (var line in list)
        {
            foreach (var node in line)
            {
                node.SetId(id++);
            }
        }
        return graph;
    }
}