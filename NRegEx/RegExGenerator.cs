using System.Text;

namespace NRegEx;

public class RegExGenerator
{
    public static string RemoveStartEndMarkers(string regExp)
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
    protected virtual RegExNode GetRandomNode(List<RegExNode> nodes)
        => nodes[Random.Next(nodes.Count)];
    protected virtual int GetBetween(int minLength, int maxLength)
        => this.Random.Next(minLength, maxLength);
    public string Generate(int minLength = 0, int maxLength = -1)
    {
        maxLength = maxLength<0? this.Regex.Pattern.Length : maxLength;

        var builder= new StringBuilder();
        var model = this.Regex.Model;

        if(model.Type  == TokenTypes.Union)
        {
            var branch = this.GetRandomNode(model.Children);
            if(branch.Type== TokenTypes.Sequence)
            {
                var leaf = this.GetRandomNode(branch.Children);
                //TODO:
                

            }
        }


        //while (nodes.Count>0)
        //{
        //    //random walk to tail
        //    var node = GetRandomNode(nodes.ToArray());
        //    if (!node.IsLink)
        //        path.Add(node);
        //    nodes.Clear();
        //    node.FetchNodes(nodes);
        //}
        //var count = this.GetBetween(minLength, maxLength);
        
        //foreach(var n in path)
        //{
        //    var c = GetRandomRune(n.CharsArray);
        //    builder.Append(char.ConvertFromUtf32(c));
        //}

        return builder.ToString();
    }
}
