/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace NRegEx;

public class RegExPatternReader(string pattern)
{
    public readonly string Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    public readonly Stack<int> PositionStack = [];
    protected int position = 0;
    public int Position
        => this.position;
    public bool HasMore
        => this.position < this.Pattern.Length;
    public string Rest
        => this.Pattern[this.position..];

    public void RewindTo(int pos)
        => this.position = pos;
    public int Peek() => this.position < this.Pattern.Length
            ? char.ConvertToUtf32((this.position < this.Pattern.Length - 1
                ? this.Pattern[this.position..(this.position + 2)]
                : this.Pattern[this.position..] + ' '), 0)
            : -1;

    public void Skip(int n = 1)
        => this.position += n;
    public void SkipString(string s)
        => this.position += s.Length;
    public int Pop()
    {
        var r = this.Peek();
        this.position += r >= 0 ? new Rune(r).Utf16SequenceLength : 0;
        return r;
    }
    public string Take()
        => char.ConvertFromUtf32(this.Pop());
    public bool LookingAt(char c)
        => Pattern[this.position] == c;
    public bool LookingAt(string s)
        => Rest.StartsWith(s);
    public string From(int previous)
        => Pattern[previous..position];
    public override string ToString()
        => Rest;
    public int Enter()
    {
        this.PositionStack.Push(this.position);
        return this.position;
    }
    public bool Leave()
    {
        if (this.PositionStack.Count > 0)
        {
            this.position = this.PositionStack.Pop();
            return true;
        }
        return false;
    }
    public bool Discard()
    {
        if (this.PositionStack.Count > 0)
        {
            this.PositionStack.Pop();
            return true;
        }
        return false;
    }
}
