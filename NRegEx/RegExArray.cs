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

        HashSet<Node> nodes = allGraph.Heads??new ();
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
    public Dictionary<string,Capture> Match(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "," + nameof(length));
        
        var dict = new Dictionary<string,Capture>();

        var s = start;
        var graph = new Graph().UnionWith(this.Array.Select(a=>a.Graph));
        var heads = graph.Heads;
    repeat:
        var nodes = heads?.ToHashSet() ?? new();
        var last = nodes;

        var i = start;
        var m = 0;
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
                if (hit)
                {
                    m++;
                    i++;
                    foreach(var node in nodes)
                    {
                        if(node.Outputs.Count == 0)
                        {
                            dict[node.Name]=( 
                                new(start,m,
                                    input[start..(start+m)]));
                        }
                    }
                }
                else
                {
                    start+=m;
                    goto repeat;
                }
            }
        }

        return dict;
    }

    public HashLookups<string,Capture> Matches(string input, int start = 0, int length = -1)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (start >= input.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0) length = input.Length;
        if (start + length > input.Length) throw new ArgumentOutOfRangeException(nameof(start) + "," + nameof(length));

        var lookups = new HashLookups<string,Capture>();

        var s = start;
        var graph = new Graph().UnionWith(this.Array.Select(a => a.Graph));
        var heads = graph.Heads;
    repeat:
        var nodes = heads.ToHashSet();
        var last = nodes;

        var i = start;
        var m = 0;
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
                if (hit)
                {
                    m++;
                    i++;
                    foreach (var node in nodes)
                    {
                        if (node.Outputs.Count == 0)
                        {
                            lookups[node.Name].Add(
                                new(start, m,
                                    input[start..(start + m)]));
                            //reset for this name
                            nodes.UnionWith(heads.Where(n => n.Name == node.Name));
                        }
                    }
                }
                else
                {
                    start += m;
                    goto repeat;
                }
            }
        }
        return lookups;
    }
}
