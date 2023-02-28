using System.Data;
using System.Text;

namespace NRegEx;

public class RegExGenerator
{
    private static string RemoveStartEndMarkers(string regExp)
    {
        if (regExp.StartsWith("^", StringComparison.Ordinal))
        {
            regExp = regExp[1..];
        }
        if (regExp.EndsWith("$", StringComparison.Ordinal))
        {
            regExp = regExp[..^1];
        }

        return regExp;
    }

    public readonly Regex Regex;
    public readonly Random Random;
    public RegExGenerator(string regex, Random? random = null)
            : this(new Regex(RemoveStartEndMarkers(regex)), random) { }
    public RegExGenerator(Regex regex, Random? random = null)
    {
        this.Regex = regex;
        Random = random ?? Random.Shared;
    }
    
    protected virtual int GetRandomRune(int[]? runes) 
        => runes == null ? '\0' : runes[Random.Next(runes.Length)];
    protected virtual Node GetRandomNode(Node[] nodes)
        => nodes[Random.Next(nodes.Length)];
    protected virtual int GetBetween(int minLength, int maxLength)
        => this.Random.Next(minLength, maxLength);
    public string Generate(int minLength = 0, int maxLength = -1)
    {
        maxLength = maxLength<0? this.Regex.Pattern.Length : maxLength;
        var builder= new StringBuilder();
        var graph = this.Regex.Graph; 
        var heads = graph.Nodes.Where(n => n.Inputs.Count == 0);
        var nodes = heads.ToHashSet();
        var path = new List<Node> { graph.Head };
        
        while (nodes.Count>0)
        {
            //random walk to tail
            var node = GetRandomNode(nodes.ToArray());
            if (!node.IsLink)
                path.Add(node);
            nodes.Clear();
            node.FetchNodes(nodes);
        }
        //TODO:
        var count = this.GetBetween(minLength, maxLength);
        
        foreach(var n in path)
        {
            var c = GetRandomRune(n.CharsArray);
            builder.Append(char.ConvertFromUtf32(c));
        }

        return builder.ToString();
    }
}
