using System.Text;

namespace NRegEx;

public partial class Regex
{
    public delegate bool? TryWithGroupsFunction(Node node, Rune[] input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, HashSet<Edge> edges, int direction);

    protected static bool VerifyMatchCore(
        Graph graph, Rune[] input, int first, int length,
        int i, int direction, Options options)
    {
        Node? last = null;
        var paths = new HashSet<CountablePath>();

        return IsMatchCore(graph, input, first, length, ref i, ref last, false, paths, null, direction, options, null);
    }

    protected bool? TryWithGroups(Node node, Rune[] input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, HashSet<Edge> edges, int direction)
    {
        if (groups != null)
        {
            foreach (var index in node.Groups)
            {
                if (this.GroupTypes.TryGetValue(index, out var type))
                {
                    this.TryWithGroup(node, input, i, groups, backs, edges, direction, index, type);
                }
            }
        }
        return null;
    }
    protected bool? TryWithGroup(Node node, Rune[] input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, HashSet<Edge> edges, int direction, int index, GroupType type)
    {
        switch (type)
        {
            case GroupType.NotGroup:
            case GroupType.NotCaptiveGroup:
                break;
            case GroupType.NormalGroup:
            case GroupType.AtomicGroup:
                if (groups != null)
                {
                    EmitPosition(node, i, groups[index]);
                    //Dealing with back reference
                    if (this.BackRefPoints.TryGetValue(index, out var graph) && graph != null)
                    {
                        graph.InsertPointBeforeTail(new(input[i].Value) { Parent = graph });
                        backs.Add(graph.Nodes.Single(n => n.Groups.Contains(i)));
                    }
                }
                break;
            case GroupType.LookAroundConditionGroup:
            case GroupType.BackReferenceConditionGroup:
                {
                    //TODO: how to do conditions 
                    // we can link the conditioned graphs with edges
                }
                break;
            case GroupType.BackReferenceCondition:
                {
                    //TODO: how to do conditions 
                    // we can link the conditioned graphs with edges
                }
                break;
            default:
                return this.TryWithConditionGroup(node, input, i, groups, backs, direction, index, type);
        }
        return null;
    }
    protected bool? TryWithConditionGroup(Node node, Rune[] input, int i, ListLookups<int, List<int>>? groups, HashSet<Node> backs, int direction, int index, GroupType type)
    {
        switch (type)
        {
            case GroupType.ForwardPositiveGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return VerifyMatchCore(g, input, 0, input.Length, i, direction, Options);
                    }
                }
                break;
            case GroupType.ForwardNegativeGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return !VerifyMatchCore(g, input, 0, input.Length, i, direction, Options);
                    }
                }
                break;
            case GroupType.BackwardPositiveGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return VerifyMatchCore(g, input, 0, input.Length, i, -direction, Options);
                    }
                }
                break;
            case GroupType.BackwardNegativeGroup:
                {
                    if (this.GroupGraphs.TryGetValue(index, out var g))
                    {
                        return !VerifyMatchCore(g, input, 0, input.Length, i, -direction, Options);
                    }
                }
                break;
        }
        return null;
    }
    protected static void EmitPosition(Node node, int i, ICollection<List<int>> captures)
    {
        if ((node.Ending & Endings.Start) == Endings.Start
            || captures.Count == 0)
        {
            captures.Add([]);
        }
        captures.Last().Add(i);
    }

}
