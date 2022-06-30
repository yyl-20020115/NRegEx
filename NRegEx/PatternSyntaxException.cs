using System.Runtime.Serialization;

namespace NRegEx;

[Serializable]
internal class PatternSyntaxException : Exception
{
    private object e = new();
    private string s = "";

    public PatternSyntaxException()
    {
    }

    public PatternSyntaxException(string? message) : base(message)
    {
    }

    public PatternSyntaxException(object e, string s)
    {
        this.e = e;
        this.s = s;
    }

    public PatternSyntaxException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected PatternSyntaxException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}