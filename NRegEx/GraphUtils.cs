/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace NRegEx;

public static class GraphUtils
{
    public static Graph Compact(Graph graph, int minlength = 2)
    {
        var pairs = new List<(List<Node> nodes, List<Edge> edges)>();
        var bridges = new HashSet<Node>();
        var headsDict = new Dictionary<Node, Edge>();
        var tailsDict = new Dictionary<Node, Edge>();
        foreach (var e in graph.Edges)
        {
            headsDict[e.Head] = e;
            tailsDict[e.Tail] = e;
        }
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
                var edge = tailsDict[next];
                do
                {
                    dots.Add(next);
                    stream.Add(edge);
                    edge = headsDict[next];
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
    public static StringBuilder ExportAsDotText(Graph graph, StringBuilder? builder = null)
    {
        builder ??= new StringBuilder();
        builder.AppendLine("digraph g {");
        foreach (var node in graph.Nodes.OrderBy(n => n.Id))
        {
            var label = !string.IsNullOrEmpty(node.Name) 
                ? $"[label=\"{node.Id}({node.Name})\"]" 
                : string.Empty;

            builder.AppendLine($"\t{node.Id} {label};");
        }
        foreach (var edge in graph.Edges.OrderBy(e => e.Head.Id).OrderBy(e => e.Tail.Id))
        {
            if (edge.MinRepeats.HasValue && edge.MaxRepeats.HasValue)
            {
                builder.AppendLine(
                    $"\t{edge.Head.Id}->{edge.Tail.Id} [label=\"min={edge.MinRepeats},max={edge.MaxRepeats}\"];");
            }
            else if(edge.MinRepeats.HasValue)
            {

                builder.AppendLine(
                    $"\t{edge.Head.Id}->{edge.Tail.Id} [label=\"min={edge.MinRepeats}\"];");
            }
            else
            {
                builder.AppendLine(
                    $"\t{edge.Head.Id}->{edge.Tail.Id};");
            }
        }
        builder.AppendLine("}");
        return builder;
    }
    public static string GetApplicationFullPath(string filePath)
    {
        var text = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(text))
        {
            foreach (var path in text.Split(";"))
            {
                var fullPath = System.IO.Path.Combine(path, filePath);
                if (File.Exists(fullPath))
                {
                    filePath = fullPath;
                    break;
                }
            }
        }
        return filePath;
    }
    public static int RunProcess(string filePath, string argument)
    {
        var process = new Process();

        process.StartInfo.FileName = GetApplicationFullPath(filePath);
        process.StartInfo.Arguments = argument;
        if (process.Start())
        {
            process.WaitForExit();
            return process.ExitCode;
        }
        return -1;
    }

    public static int ExportAsDot(Regex regex, string? png = null, string? dot = null)
        => ExportAsDotText(ExportAsDotText(regex.Graph).ToString(), png, dot);
    public static int ExportAsDot(Graph graph, string? png = null, string? dot = null)
        => ExportAsDotText(ExportAsDotText(graph).ToString(), png, dot);

    public static int ExportAsDotText(string content, string? png = null, string? dot = null)
    {
        var trace = new StackTrace();
        var fnn = "graph";
        var depth = 1;
        do
        {
            fnn = trace?.GetFrame(depth++)?.GetMethod()?.Name;
        } while (fnn == nameof(ExportAsDot));

        fnn ??= "graph";
        png ??= fnn + ".png";
        dot ??= fnn + ".dot";
        dot = System.IO.Path.Combine(Environment.CurrentDirectory, dot);
        png = System.IO.Path.Combine(Environment.CurrentDirectory, png);
        File.WriteAllText(dot, content);
        return RunProcess("dot.exe", $"-Grankdir=LR -T png {dot} -o {png}");
    }


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
    public static Graph Reform(Graph graph, int id = 0)
        => Reorder(Compact(graph), id);

    public static Graph Reorder(Graph graph, int id = 0)
    {
        var cdict = new ConcurrentDictionary<Node, HashSet<Node>>();
        Parallel.ForEach(graph.Nodes, node =>
        {
            cdict[node] = graph.Edges.Where(
                e => e.Head == node).Select(e => e.Tail).ToHashSet();
        });
        
        var dict = new Dictionary<Node, HashSet<Node>>(cdict);

        var visited = new HashSet<Node>() { graph.Head };

        var follows = visited.ToHashSet();
        
        var list = new List<List<Node>>
        {
            follows.OrderBy(c => c.Id).ToList(),
        };

        var collects = new HashSet<Node>();
        do
        {
            foreach (var node in follows)
                collects.UnionWith(
                    dict[node].Where(n => visited.Add(n)));

            if ((follows = collects.ToHashSet()).Count > 0)
            {
                list.Add(collects.OrderBy(c => c.Id).ToList()); 
                collects = new();
            }
        } while (follows.Count > 0);
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