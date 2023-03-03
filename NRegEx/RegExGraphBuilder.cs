namespace NRegEx;
public class RegExGraphBuilder
{
    public readonly Dictionary<int, Graph> GroupGraphs = new();
    public readonly Dictionary<int, Graph> BackRefPoints = new();
    public readonly Dictionary<int, GroupType> GroupTypes = new();
    public readonly ListLookups<int, Graph> ConditionsGraphs = new();
    public Graph Build(RegExNode node, int id = 0, bool caseInsensitive = false)
        => GraphUtils.Reform(BuildInternal(node, caseInsensitive),id);
    protected Graph BuildInternal(RegExNode node, bool caseInsensitive = false)
    {
        var graph = new Graph(node.Name) { SourceNode = node };
        switch (node.Type)
        {
            case TokenTypes.EOF:
                {
                    graph.ComposeLiteral(new Node() { Parent = graph});
                }
                break;
            case TokenTypes.OnePlus:
                {
                    if (node.Children.Count >= 1)
                        graph.OnePlus(BuildInternal(node.Children[0]));
                }
                break;
            case TokenTypes.ZeroPlus:
                {
                    if (node.Children.Count >= 1)
                        graph.ZeroPlus(BuildInternal(node.Children[0]));
                }
                break;
            case TokenTypes.ZeroOne:
                {
                    if (node.Children.Count >= 1)
                        graph.ZeroOne(BuildInternal(node.Children[0]));
                }
                break;
            case TokenTypes.Literal:
                {
                    graph.ComposeLiteral(node.Value.EnumerateRunes().Select(c => 
                        caseInsensitive 
                        ? new Node(Characters.ToUpperCase(c.Value),
                                   Characters.ToLowerCase(c.Value)) { Parent = graph } 
                        : new Node(c.Value) { Parent = graph }));
                }
                break;
            case TokenTypes.RuneClass:
                {
                    graph.ComposeLiteral(new Node(node.Inverted, 
                        node.Runes ?? Array.Empty<int>()) { Parent = graph });
                }
                break;
            case TokenTypes.AnyCharIncludingNewLine:
                {
                    //inverted empty means everything
                    graph.ComposeLiteral(new Node(true, 
                        Array.Empty<int>()) { Parent = graph });
                }
                break;
            case TokenTypes.AnyCharExcludingNewLine:
                {
                    //\n excluded
                    graph.UnionWith(new Node(true,
                        Node.NewLineChar) { Parent = graph });
                }
                break;
            case TokenTypes.Sequence:
                {
                    if (node.Children.Count > 0)
                        graph.Concate(node.Children.Select(c => BuildInternal(c)));
                }
                break;
            case TokenTypes.Group:
                {
                    //this is for condition
                    if ((node.GroupType == GroupType.BackReferenceConditionGroup 
                        ||node.GroupType == GroupType.LookAroundConditionGroup)
                        && node.Children.Count > 0 && node.Children[0].Type == TokenTypes.Union)
                    {
                        var unode = node.Children[0];
                        RegExNode? condition = null;
                        List<RegExNode> actions = new();
                        List<RegExNode> elseAction = new();
                        if (unode.Children.Count == 1)
                        {
                            var snode = unode.Children[0];
                            if (snode.Type == TokenTypes.Sequence && snode.Children.Count >= 2)
                            {
                                condition = snode.Children[0];
                                actions.AddRange(snode.Children.Skip(1));
                            }
                        }
                        else if (unode.Children.Count == 2)
                        {
                            var snode = unode.Children[0];
                            var tnode = unode.Children[1];
                            if (snode.Type == TokenTypes.Sequence && snode.Children.Count >= 2)
                            {
                                condition = snode.Children[0];
                                actions.AddRange(snode.Children.Skip(1));
                            }
                            if (tnode.Type == TokenTypes.Sequence && tnode.Children.Count >0)
                            {
                                elseAction.AddRange(tnode.Children);
                            }
                        }
                        if (condition != null)
                        {
                            var conditionGroupGraph = this.BuildInternal(condition, caseInsensitive);
                            var actionGroupGraph = new Graph() { SourceNode = node };
                            var elseActionGroupGraph = new Graph() { SourceNode = node };

                            var index = condition.CaptureIndex;
                            if (index is not null)
                            {
                                ConditionsGraphs[index.Value] = new List<Graph> {
                                        actionGroupGraph, elseActionGroupGraph };
                                //TODO:
                                if (condition.Type == TokenTypes.BackReference)
                                {
                                    graph.GroupWith(new Node() { Parent = conditionGroupGraph }, index.Value);
                                }
                                else
                                {
                                    //TODO: 
                                    graph.BackReferenceWith(conditionGroupGraph, index.Value);
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException($"index is not found conditon");
                            }
                        }
                    }
                    //this is for lookaround
                    else if (node.Children.Count > 0 && node.CaptureIndex is int index)
                    {
                        var capture = BuildInternal(node.Children[0]);

                        switch (GroupTypes[index] = node.GroupType)
                        {
                            case GroupType.NotGroup:
                                throw new InvalidOperationException("should be a group");
                            case GroupType.AtomicGroup:
                            case GroupType.NormalGroup:
                            case GroupType.NotCaptiveGroup:
                                graph.GroupWith(capture, index);
                                break;
                            case GroupType.ForwardPositiveGroup:
                            case GroupType.ForwardNegativeGroup:
                            case GroupType.BackwardPositiveGroup:
                            case GroupType.BackwardNegativeGroup:
                                //this null node points to the graph for group function
                                graph.GroupWith(new Node($"GroupRef({index})") { Parent = graph }, index);
                                this.GroupGraphs.Add(index, capture);
                                break;
                            case GroupType.LookAroundConditionGroup:
                                break;
                            default:
                                break;
                        }
                    }
                }
                break;
            case TokenTypes.BackReference:
                {
                    if (node.Children.Count > 0 && node.CaptureIndex is int index)
                        this.BackRefPoints[index] 
                            = graph.BackReferenceWith(BuildInternal(node.Children[0]), index);
                }
                break;
            case TokenTypes.Union:
                {
                    if (node.Children.Count > 0)
                        graph.UnionWith(node.Children.Select(c => BuildInternal(c)));
                }
                break;
            case TokenTypes.Repeats:
                {
                    if (node.Children.Count > 0)
                        graph.ComposeRepeats(BuildInternal(node.Children[0]),
                            node.Min.GetValueOrDefault(),
                            node.Max.GetValueOrDefault());
                }
                break;
            case TokenTypes.BeginLine:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.BEGIN_LINE) { Parent = graph });
                }
                break;
            case TokenTypes.EndLine:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.END_LINE) { Parent = graph });
                }
                break;
            case TokenTypes.BeginText:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.BEGIN_TEXT) { Parent = graph });
                }
                break;
            case TokenTypes.EndText:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.END_TEXT) { Parent = graph });
                }
                break;
            case TokenTypes.WordBoundary:
                {
                    graph.UnionWith(new Node(false, RegExTextReader.WORD_BOUNDARY) { Parent = graph });
                }
                break;
            case TokenTypes.NotWordBoundary:
                {
                    graph.UnionWith(new Node(true,RegExTextReader.NOT_WORD_BOUNDARY) { Parent = graph });
                }
                break;
        }
        return graph.TryComplete();
    }
}
