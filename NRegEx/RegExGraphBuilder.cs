namespace NRegEx;
public class RegExGraphBuilder
{
    public readonly Dictionary<int, Graph> BackReferencesPoints = new();
    public readonly Dictionary<int, GroupType> GroupTypes = new();
    public Graph Build(RegExNode node, int id = 0, bool caseInsensitive = false)
        => GraphUtils.Reform(
            BuildInternal(node, caseInsensitive),id);
    private Graph BuildInternal(RegExNode node, bool caseInsensitive = false)
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
            case TokenTypes.Capture:
                {
                    if (node.Children.Count > 0 && node.CaptureIndex is int index)
                    {
                        graph.GroupWith(BuildInternal(node.Children[0]), index);
                        GroupTypes[index] = node.GroupType;
                    }
                }
                break;
            case TokenTypes.BackReference:
                {
                    if (node.Children.Count > 0 && node.CaptureIndex is int index)
                        this.BackReferencesPoints[index] 
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
