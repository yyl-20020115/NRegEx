namespace NRegEx;
public class RegExGraphBuilder
{
    public RegExGraphBuilder() { }
    public Graph Build(RegExNode node)
    {
        var graph = new Graph(node.Name);
        switch (node.Type)
        {
            case RegExTokenType.EOF:
                {
                    graph.Compose(new Node());
                }
                break;
            case RegExTokenType.OnePlus:
                {
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.OnePlus) + nameof(node));
                    graph.OnePlus(this.Build(node.Children[0]));
                }
                break;
            case RegExTokenType.ZeroPlus:
                {
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.ZeroPlus) + nameof(node));
                    graph.ZeroPlus(this.Build(node.Children[0]));
                }
                break;
            case RegExTokenType.ZeroOne:
                {
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.ZeroOne) + nameof(node));
                    graph.ZeroOne(this.Build(node.Children[0]));
                }
                break;
            case RegExTokenType.Literal:
                {
                    graph.Compose(node.Value.Select(c => new Node(c)));
                }
                break;
            case RegExTokenType.CharClass:
                {
                    graph.Compose(new Node().UnionWith(node.Runes ?? Array.Empty<int>()));
                }
                break;
            case RegExTokenType.AnyCharIncludingNewLine:
                {
                    graph.Compose(new Node().UnionWith(Node.AllChars));
                }
                break;
            case RegExTokenType.AnyCharExcludingNewLine:
                {
                    graph.UnionWith(new Node(true, Node.ReturnChar), new Node(true, Node.NewLineChar));
                }
                break;
            case RegExTokenType.Sequence:
                {
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Sequence) + nameof(node));
                    graph.Concate(node.Children.Select(c => this.Build(c)));
                }
                break;

            case RegExTokenType.Union:
                {
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Union) + nameof(node));
                    graph.UnionWith(node.Children.Select(c => this.Build(c)));
                }
                break;
            case RegExTokenType.Repeats:
                {
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Union) + nameof(node));
                    graph.ComposeRepeats(this.Build(node.Children[0]),
                        node.Min.GetValueOrDefault(),
                        node.Max.GetValueOrDefault());
                }
                break;
            case RegExTokenType.BeginLine:
                {
                    graph.Head.UnionWith(Node.NewLineChar);
                    graph.Tail.UnionWith(Node.AllChars);
                }
                break;
            case RegExTokenType.EndLine:
                {
                    graph.Head.UnionWith(Node.AllChars);
                    graph.Tail.UnionWith(Node.NewLineChar);
                }
                break;
            case RegExTokenType.BeginText:
                {
                    graph.Head.UnionWith(Node.EOFChar);
                    graph.Tail.UnionWith(Node.AllChars);
                }
                break;
            case RegExTokenType.EndText:
                {
                    graph.Head.UnionWith(Node.AllChars);
                    graph.Tail.UnionWith(Node.EOFChar);
                }
                break;
            case RegExTokenType.WordBoundary:
                {
                    var g1 = new Graph();
                    var g2 = new Graph();
                    g1.Head.UnionWith(Node.WordChars);
                    g1.Tail.UnionWith(Node.NonWordChars);

                    g2.Head.UnionWith(Node.NonWordChars);
                    g2.Tail.UnionWith(Node.WordChars);

                    graph.UnionWith(g1, g2);
                }
                break;
            case RegExTokenType.NotWordBoundary:
                {
                    var g1 = new Graph();
                    var g2 = new Graph();
                    g1.Head.UnionWith(Node.WordChars);
                    g1.Tail.UnionWith(Node.WordChars);

                    g2.Head.UnionWith(Node.NonWordChars);
                    g2.Tail.UnionWith(Node.NonWordChars);

                    graph.UnionWith(g1, g2);
                }
                break;
        }

        return graph;
    }
}
