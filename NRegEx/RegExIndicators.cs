/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class RegExIndicators
{
    public readonly bool[] Indicators = new bool[8];

    protected const int BeginTextIndex = 0;
    protected const int EndTextIndex = 1;
    protected const int BeginLineIndex = 2;
    protected const int EndLineIndex = 3;
    protected const int BeginWordIndex = 4;
    protected const int EndWordIndex = 5;
    protected const int WordBoundaryIndex = 6;
    protected const int NotWordBoundaryIndex = 7;

    public readonly Dictionary<int, int> IndicatorsDict = new()
    {
        [BeginTextIndex] = RegExTextReader.BEGIN_TEXT,
        [EndTextIndex] = RegExTextReader.END_TEXT,
        [BeginLineIndex] = RegExTextReader.BEGIN_LINE,
        [EndLineIndex] = RegExTextReader.END_LINE,
        [BeginWordIndex] = RegExTextReader.BEGIN_WORD,
        [EndWordIndex] = RegExTextReader.END_WORD,
        [WordBoundaryIndex] = RegExTextReader.WORD_BOUNDARY,
        [NotWordBoundaryIndex] = RegExTextReader.NOT_WORD_BOUNDARY,
    };

    public void UpdateIndicators(string input, int i, int first, int tail, int direction)
    {
        direction = RegexHelpers.FixDirection(direction);
        int start = first;
        if (!(i >= first && i < tail)) return;

        int end = tail - 1;
        char? Last = i > start && i < tail ? input[i - direction] : null;
        char? This = i >= start && i < tail ? input[i + 0] : null;
        char? Next = i < end ? input[i + direction] : null;

        if (direction < 0) (start, end) = (end, first);

        this.Indicators[BeginTextIndex] = i == start;
        this.Indicators[EndTextIndex] = i == end;

        this.Indicators[BeginLineIndex] = Last is '\n' or null;
        this.Indicators[EndLineIndex] = This == '\n';

        this.Indicators[BeginWordIndex]
            = (Last is null || !Unicode.IsRuneWord(Last.Value))
            && (This is not null && Unicode.IsRuneWord(This.Value));

        this.Indicators[EndWordIndex]
            = (This is not null && Unicode.IsRuneWord(This.Value))
            && (Next is null || !Unicode.IsRuneWord(Next.Value));

        this.Indicators[WordBoundaryIndex]
            = this.Indicators[BeginWordIndex]
            || this.Indicators[EndWordIndex]
            ;

        this.Indicators[NotWordBoundaryIndex]
            = !this.Indicators[WordBoundaryIndex];
    }

}
