namespace NRegEx;
public class RegExTextReader
{
    public const int EOF = -1;
    public const int UNICODE_LIMIT = 0x10ffff;

    public const int WORD_BOUNDARY = UNICODE_LIMIT + 1;
    public const int NOT_WORD_BOUNDARY = UNICODE_LIMIT + 2;

    public const int BEGIN_TEXT = UNICODE_LIMIT + 3;
    public const int END_TEXT = UNICODE_LIMIT + 4;

    public const int BEGIN_LINE = BEGIN_TEXT + 2;
    public const int END_LINE = END_TEXT + 2;

    public const int BEGIN_WORD = BEGIN_LINE + 2;
    public const int END_WORD = END_LINE + 2;

    public readonly TextReader Reader;
    protected enum ReaderStates : uint
    {
        NotStarted = 0,
        InProgress = 1,
        PastEOF = 2,
        Finished = 3
    }
    protected ReaderStates State = ReaderStates.NotStarted;

    public readonly bool LineMode;
    public RegExTextReader(TextReader reader, bool lineMode )
    {
        this.Reader = reader;
        this.LineMode = lineMode;
    }

    public bool HasMore => this.Peek() != EOF;

    protected bool LastEndLine = true;
    protected int TryPeek()
    {
        int c = this.Reader.Peek();

        if (this.LineMode)
        {
            if (c == '\n')
            {
                this.LastEndLine = true;
                return END_LINE;
            }
            else if(this.LastEndLine)
            {
                return BEGIN_LINE;
            }
        }
        return c == EOF ? END_TEXT : c;
    }
    public int Peek() => this.State switch
    {
        ReaderStates.NotStarted => this.LineMode ? BEGIN_LINE : BEGIN_TEXT,
        ReaderStates.InProgress => this.TryPeek(),
        ReaderStates.PastEOF => EOF,
        _ => EOF,
    };
    public int Read()
    {
        switch (this.State)
        {
            case ReaderStates.NotStarted:
                this.State = ReaderStates.InProgress;
                return this.LineMode ? BEGIN_LINE : BEGIN_TEXT;
            case ReaderStates.InProgress:
                if ((this.Read() is int c) && c == EOF)
                {
                    State = ReaderStates.PastEOF;
                    c = this.LineMode ? END_LINE : END_TEXT;
                }
                if (c != '\n')
                {
                    this.LastEndLine = false;
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
