namespace NRegEx;

public class RegExArray
{
    public Dictionary<string, Regex> Dict = new();
    public ICollection<Regex> Regs => Dict.Values;
    public string[] Regexs { get; protected set; }
        = System.Array.Empty<string>();
    public RegExArray(params string[] regexs) 
        => this.SetRegexs(regexs);

    public void SetRegexs(params string[] regexs)
    {
        this.Dict.Clear();
        foreach (var regex in this.Regexs = regexs)
            Dict.Add(regex, new(regex, regex));
    }
}
