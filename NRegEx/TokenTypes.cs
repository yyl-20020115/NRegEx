namespace NRegEx;

[Flags]
public enum TokenTypes : int
{
    EOF = -1,
    Literal = 0,
    RuneClass = 1,
    CaseInsensitive = 2,
    AnyCharExcludingNewLine = 3,
    AnyCharIncludingNewLine = 4,
    BeginLine = 5,
    EndLine = 6,
    BeginText = 7,
    EndText = 8,
    WordBoundary = 9,
    NotWordBoundary = 10,
    Group = 11,       //
    BackReference = 12,
    ZeroPlus = 13,      //*
    OnePlus = 14,       //+
    ZeroOne = 15,       //?
    Repeats = 16,
    Concate = 17,       //& = Sequence
    Sequence = 18,      //.&.&.&...
    Alternate = 19,     // |
    Union = 20,         //..|..|.. = Alternate
    OpenParenthesis = 21,     //(
    CloseParenthesis = 22,     //)
    BeginWord = 23, //<
    EndWord = 24, //>
}
