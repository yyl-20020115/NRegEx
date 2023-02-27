namespace NRegEx;

public class PatternSyntaxException : Exception
{
    public readonly string Content;

    public PatternSyntaxException(string message, string content = "") 
        : base(message) => this.Content = content;

}