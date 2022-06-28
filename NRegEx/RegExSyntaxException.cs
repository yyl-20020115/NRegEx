namespace NRegEx;

public class RegExSyntaxException : Exception
{
    public readonly string Error;
    public readonly string Patttern;
    public RegExSyntaxException(string error, string patttern = "")
          : base("error parsing regexp: " + error + (String.IsNullOrEmpty(patttern) ? "": ": `" + patttern + "`"))
    {
        this.Error = error;
        this.Patttern = patttern;
    }
}
