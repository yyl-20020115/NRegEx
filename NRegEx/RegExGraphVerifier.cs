using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRegEx;

public static class RegExGraphVerifier
{
    /// <summary>
    /// 判断是否可能出现灾难性回溯
    /// 
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static bool IsPossiblyCatastrophicBacktracking(Regex regex)
        => IsPossiblyCatastrophicBacktracking(regex.Graph);
    public static bool IsPossiblyCatastrophicBacktracking(Graph graph)
    {
        //TODO:
        return false;
    }


    /*
public static bool IsBacktracingFriendly(Graph graph)
{
    //对于支持回溯的RegEx引擎，
    //  1.如果相继&的两个node之间具有交集，则有可能出现回溯灾难。
    //  2.如果前一个是{0,n}后一个和前一个无交集，则有可能出现回溯灾难。
    //本引擎不处理回溯灾难
    var nodeSets = new Dictionary<Node, HashSet<int>>();
    var nodes = graph.Nodes.Where(n => n.Inputs.Count == 0).ToHashSet();
    nodes = nodes.SelectMany(n => n.Outputs).ToHashSet();

    //TODO:
    var copies = nodes.ToHashSet();
    do
    {
        foreach (var node in nodes)
        {
            nodeSets[node] 
                = new (node.Inputs.SelectMany(i => i.CharSet ?? new()));
        }
        nodes = nodes.SelectMany(h => h.Outputs).ToHashSet();
    } while (nodes.Count > 0);

    nodes = copies;
    do
    {
        foreach (var node in nodes)
            //a&b: a and b's charset shares elements
            if (node.Inputs.Count == 1
                && node.CharSet.Overlaps(node.Inputs.Single().CharSet))
                return false;

        nodes = nodes.SelectMany(h => h.Outputs).ToHashSet();
    } while (nodes.Count > 0);
    return true;
}
*/

}
