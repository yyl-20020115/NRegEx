using System.Text;

namespace NRegEx;
public class RegExReplacementParser
{
    public readonly StringReader Reader;
    public readonly string Replacement;
    public RegExReplacementParser(string replacement) 
        => this.Reader = new (this.Replacement = replacement);
    public bool HasMore => this.Peek() != -1;
    public int Peek() => this.Reader.Peek();
    public int Read() => this.Reader.Read();
    public char PeekChar()=>(char)this.Peek();
    public char ReadChar()=> (char)this.Read();

    public List<RegExReplacement> Parse()
    {
        var repls = new List<RegExReplacement>();
        if(!string.IsNullOrEmpty(this.Replacement))
        {
            var builder = new StringBuilder();
            while(this.HasMore)
            {
                var c = this.ReadChar();
                if (c == '$')
                {
                    if (builder.Length > 0)
                    {
                        repls.Add(new(ReplacementType.PlainText, builder.ToString()));
                        builder.Clear();
                    }
                    if (!this.HasMore)
                    {
                        builder.Append(c);
                        break;
                    }
                    switch (c = this.ReadChar())
                    {
                        //$number
                        case >='1' and <='9':
                            {
                                var lb = new StringBuilder(c.ToString());
                                while (this.HasMore)
                                {
                                    c = this.PeekChar();
                                    if (c is >= '0' and <= '9')
                                    {
                                        this.Read();
                                        lb.Append(c);
                                    }
                                    else break;
                                }
                                var text = lb.ToString();
                                if(int.TryParse(text,out var index))
                                {
                                    repls.Add(new (ReplacementType.GroupIndex,text, index, text));
                                }
                                else
                                {
                                    throw new Exception("invalid group index");
                                }
                            }
                            continue;
                        //${name}
                        case '{':
                            {
                                var lb = new StringBuilder();
                                while (this.HasMore)
                                {
                                    c = this.PeekChar();
                                    if (c == '}')
                                    {
                                        this.ReadChar();
                                        var name = lb.ToString();
                                        repls.Add(new(ReplacementType.GroupName, name, -1, name));
                                        continue;
                                    }
                                    else
                                    {
                                        builder.Append(c = this.ReadChar());
                                    }
                                    if (!RegExDomParser.IsValidCaptureNameChar(c))
                                        throw new Exception($"invalid group name:{lb}");
                                }
                                throw new Exception($"invalid group name{lb}");
                            }
                        case '$':
                            repls.Add(new (ReplacementType.Dollar, "$"));
                            continue;
                        case '&':
                            repls.Add(new (ReplacementType.WholeMatch, "$&"));
                            continue;
                        case '`':
                            repls.Add(new (ReplacementType.PreMatch, "$`"));
                            continue;
                        case '\'':
                            repls.Add(new (ReplacementType.PostMatch, "$`"));
                            continue;
                        case '+':
                            repls.Add(new (ReplacementType.LastGroup, "$`"));
                            continue;
                        case '_':
                            repls.Add(new (ReplacementType.Input, "$`"));
                            continue;
                    }
                }
                builder.Append(c);
            }
            if (builder.Length > 0)
                repls.Add(new (ReplacementType.PlainText, builder.ToString()));
        }

        return repls;
    }
}
