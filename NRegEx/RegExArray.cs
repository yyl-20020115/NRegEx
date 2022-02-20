using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRegEx;

public class RegExArray
{
    public Dictionary<string, Regex> Dict = new ();    
    public ICollection<Regex> Array =>Dict.Values;
    public string[] Regexs { get; protected set; } 
        = System.Array.Empty<string>();
    public RegExArray(params string[] regexs)
    {
        this.SetRegexs(regexs);
    }

    public void SetRegexs(params string[] regexs)
    {
        this.Dict.Clear();
        foreach(var regex in this.Regexs = regexs)
            Dict.Add(regex,new(regex,regex));
    }

    public HashSet<string> IsMatch(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "," + nameof(length));

        var allGraph = new Graph();
        foreach(var r in this.Array)
            allGraph.UnionWith(r.Graph);

        var nodes = allGraph.Heads;
        var last = nodes;
        var i = start;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            last = nodes;
            nodes = new HashSet<Node>();
            if (copies.All(copy => copy.IsVirtual))
            {
                nodes.UnionWith(copies.SelectMany(n => n.Outputs));
            }
            else
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    if (node.Hit(c))
                    {
                        hit = true;
                        //needs all hits
                        nodes.UnionWith(node.Outputs);
                    }
                }
                if (hit) i++;
            }
        }

        if(i == input.Length && (nodes.Count == 0 
            || nodes.Any(n => n.Outputs.Count == 0)))
        {
            return last.Select(n => n.Name).ToHashSet();
        }
        return new();
    }
    public HashLookups<string, Capture> Match(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "," + nameof(length));
        
        var lookups = new HashLookups<string, Capture>();

        var s = start;
        var allGraph = new Graph();
        foreach (var r in this.Array)
            allGraph.UnionWith(r.Graph);

    repeat:

        var nodes = allGraph.Heads;
        var i = start;
        var m = 0;
        while (nodes.Count > 0 && i < length)
        {
            var copies = nodes.ToArray();
            nodes.Clear();
            if (copies.All(copy => copy.IsVirtual))
            {
                nodes.UnionWith(copies.SelectMany(n => n.Outputs));
            }
            else
            {
                var c = input[i];
                var hit = false;
                foreach (var node in copies)
                {
                    if (node.Hit(c))
                    {
                        hit = true;
                        //needs all hits
                        nodes.UnionWith(node.Outputs);
                    }
                }
                if (hit)
                {
                    m++;
                    i++;
                }
                else
                {
                    start++;
                    goto repeat;
                }
            }
        }


        //TODO:
        return lookups;
        //return start > s && nodes.Count == 0
        //    ? new Capture(start, m,
        //        input[start..(start + m)], "")
        //    : new Capture(start, -1);
    }

}
