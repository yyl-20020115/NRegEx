namespace NRegEx;

[Flags]
public enum TokenTypes : int
{
    EOF = -1,
    Literal = 0,
    RuneClass = 1,
    FoldCase = 2, //NOT USED
    AnyCharExcludingNewLine = 3,
    AnyCharIncludingNewLine = 4,
    BeginLine = 5,
    EndLine = 6,
    BeginText = 7,
    EndText = 8,
    WordBoundary = 9,
    NotWordBoundary = 10,
    Capture = 11,       //NOT USED
    ZeroPlus = 12,      //*
    OnePlus = 13,       //+
    ZeroOne = 14,       //?
    Repeats = 15,
    Concate = 16,       //& = Sequence
    Sequence = 17,      //.&.&.&...
    Alternate = 18,     // |
    Union = 19,         //..|..|.. = Alternate
    OpenParenthesis = 20,     //(
    CloseParenthesis = 21,     //)
    BeginWord = 22, //<
    EndWord = 23, //>
}
