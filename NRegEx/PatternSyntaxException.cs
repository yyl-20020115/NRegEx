/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class PatternSyntaxException : Exception
{
    public readonly string Content;

    public PatternSyntaxException(string message, string content = "")
        : base(message) => this.Content = content;

}