namespace NRegEx;
public class RegExTextReader
{
    public const int EOF = -1;
    public const int UNICODE_LIMIT = 0x10ffff;
    public const int BEGIN_TEXT = UNICODE_LIMIT + 1;
    public const int END_TEXT = UNICODE_LIMIT + 2;
    public const int WORD_BOUNDARY = UNICODE_LIMIT + 1;

    public readonly TextReader Reader;
    protected enum ReaderStates : uint
    {
        NotStarted,
        InProgress,
        PastEOF,
        Finished
    }
    protected ReaderStates State = ReaderStates.NotStarted;

    public RegExTextReader(TextReader reader) => this.Reader = reader;

    public bool HasMore => this.Peek() != EOF;

    public int Peek() => this.State switch
    {
        ReaderStates.NotStarted => BEGIN_TEXT,
        ReaderStates.InProgress => (this.Reader.Peek() is int c)?(c == EOF ? END_TEXT : c) : EOF ,
        ReaderStates.PastEOF => EOF,
        _ => EOF,
    };
    public int Read()
    {
        var c = -1;
        switch (this.State)
        {
            case ReaderStates.NotStarted:
                this.State = ReaderStates.InProgress;
                return BEGIN_TEXT;
            case ReaderStates.InProgress:
                if((c = this.Read()) == EOF)
                {
                    State = ReaderStates.PastEOF;
                    c = END_TEXT;
                }
                return c;
            case ReaderStates.PastEOF:
                this.State = ReaderStates.Finished; 
                return EOF;
            default: //Finished etc
                return EOF;
        }
    }
}
