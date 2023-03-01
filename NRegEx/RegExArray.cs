namespace NRegEx;

/// <summary>
/// All Regexs will be matched parallelly
/// during the matching stage just like one Regex
/// Other operations are the same.
/// The Name field of Capture object is the source name or pattern 
/// (if name is not given) of the Regex which matched.
/// </summary>
public class RegExArray : Regex
{
    public readonly Dictionary<string, Regex> RegexDictionary = new();
    public readonly Regex[] Regexs;
    public RegExArray(params string[] regexs)
        : this(regexs.Select(r => new Regex(r)).ToArray()) { }
    public RegExArray(params Regex[] regexs)
        : base("", "")
    {
        if (regexs.Length == 0)
            throw new ArgumentException(nameof(regexs));
        this.Model = new(TokenTypes.Union) { };
        this.Graph = new() { SourceNode = this.Model };
        foreach (var regex in this.Regexs = regexs)
        {
            this.RegexDictionary[regex.Name] = regex;
            this.Model.Children.Add(regex.Model);
            this.Graph.UnionWith(regex.Graph.Copy());
        }
    }
}
