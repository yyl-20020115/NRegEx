using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRegEx
{
    public class RegExGraphBuilder
    {
        public Graph Build(RegExNode node)
        {
            var graph = new Graph(node.Name);
            switch (node.Type)
            {
                case RegExTokenType.EOF:
                    graph.Compose(new Node("", '\0'));
                    break;
                case RegExTokenType.OnePlus:
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.OnePlus) + nameof(node));
                    graph.OnePlus(this.Build(node.Children[0]));
                    break;
                case RegExTokenType.ZeroPlus:
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.ZeroPlus) + nameof(node));
                    graph.ZeroPlus(this.Build(node.Children[0]));
                    break;
                case RegExTokenType.ZeroOne:
                    if (node.Children.Count != 1) throw new PatternSyntaxException(
                        nameof(RegExTokenType.ZeroOne) + nameof(node));
                    graph.ZeroOne(this.Build(node.Children[0]));
                    break;
                case RegExTokenType.Literal:
                    graph.Compose(node.Value.Select(c => new Node("", c)));
                    break;
                case RegExTokenType.CharClass:
                    graph.Compose(new Node().UnionWith(node.Runes??Array.Empty<int>()));
                    break;
                case RegExTokenType.AnyCharIncludingNewLine:
                    graph.Compose(new Node().UnionWith(Node.AllChars));
                    break;
                case RegExTokenType.AnyCharExcludingNewLine:
                    graph.UnionWith(new ("",'\r',true), new ("", '\n', true));
                    break;

                case RegExTokenType.Sequence:
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Sequence) + nameof(node));
                    graph.Concate(node.Children.Select(c => this.Build(c)));
                    break;

                case RegExTokenType.Union:
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Union) + nameof(node));
                    graph.UnionWith(node.Children.Select(c => this.Build(c)));
                    break;
                case RegExTokenType.Repeats:
                    if (node.Children.Count == 0) throw new PatternSyntaxException(
                        nameof(RegExTokenType.Union) + nameof(node));
                    //
                    break;
                case RegExTokenType.BeginLine:
                    //TODO:
                    break;
                case RegExTokenType.EndLine:
                    //TODO:
                    break;
                case RegExTokenType.BeginText:
                    //TODO:
                    break;
                case RegExTokenType.EndText:
                    //TODO:
                    break;
                case RegExTokenType.WordBoundary:
                    //TODO:
                    break;
                case RegExTokenType.NotWordBoundary:
                    //TODO:
                    break;
            }

            return graph;
        }
    }
}
