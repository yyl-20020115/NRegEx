/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

using System.IO;
using System.Text;
/**
* A simple reader of lines from a UNIX character stream, like java.io.BufferedReader, but doesn't
* consider '\r' a line terminator.
*
* @author adonovan@google.com (Alan Donovan)
*/
namespace NRegex.Tests;

public class UNIXBufferedReader : StreamReader
{
    public UNIXBufferedReader(Stream stream)
        : base(stream) { }

    public UNIXBufferedReader(Stream stream, bool detectEncodingFromByteOrderMarks)
        : base(stream, detectEncodingFromByteOrderMarks) { }

    public UNIXBufferedReader(Stream stream, Encoding encoding)
        : base(stream, encoding) { }
    public UNIXBufferedReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        : base(stream, encoding, detectEncodingFromByteOrderMarks) { }

    public UNIXBufferedReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize) { }
    public UNIXBufferedReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
        : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen) { }
    public UNIXBufferedReader(string path)
        : base(path) { }
    public UNIXBufferedReader(string path, FileStreamOptions options)
        : base(path, options) { }
    public UNIXBufferedReader(string path, bool detectEncodingFromByteOrderMarks)
        : base(path, detectEncodingFromByteOrderMarks) { }
    public UNIXBufferedReader(string path, Encoding encoding)
        : base(path, encoding) { }
    public UNIXBufferedReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        : base(path, encoding, detectEncodingFromByteOrderMarks) { }
    public UNIXBufferedReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        : base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize) { }
    public UNIXBufferedReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
        : base(path, encoding, detectEncodingFromByteOrderMarks) { }

    protected char[] Buffer = new char[4096];
    protected int BufferLength = 0; // length prefix of |buf| that is filled
    protected int NextIndex = 0; // index in buf of next char
    public override string? ReadLine()
    {
        StringBuilder? builder = null; // holds '\n'-free gulps of input
        int _StartIndex; // index of first char
        for (; ; )
        {
            // Should we refill the buffer?
            if (NextIndex >= BufferLength)
            {
                int n = this.Read(Buffer, 0, Buffer.Length);
                if (n > 0)
                {
                    BufferLength = n;
                    NextIndex = 0;
                }
                else
                {
                    return null;
                }
            }
            // Did we reach end-of-file?
            if (NextIndex >= BufferLength)
            {
                return builder != null && builder.Length > 0 ? builder.ToString() : null;
            }
            // Did we read a newline?
            var i = NextIndex;
            for (; i < BufferLength; i++)
            {
                if (Buffer[i] == '\n')
                {
                    _StartIndex = NextIndex;
                    NextIndex = i;
                    string line;
                    if (builder == null)
                    {
                        line = new(Buffer, _StartIndex, i - _StartIndex);
                    }
                    else
                    {
                        builder.Append(Buffer, _StartIndex, i - _StartIndex);
                        line = builder.ToString();
                    }
                    NextIndex++;
                    return line;
                }
            }
            _StartIndex = NextIndex;
            NextIndex = i;
            builder ??= new(80);
            builder.Append(Buffer, _StartIndex, i - _StartIndex);
        }
    }
}
