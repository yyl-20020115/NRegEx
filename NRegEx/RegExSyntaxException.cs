/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class RegExSyntaxException : Exception
{
    public readonly string Error;
    public readonly string Patttern;
    public RegExSyntaxException(string error, string patttern = "")
          : base("Error parsing regexp: " + error + (string.IsNullOrEmpty(patttern) ? "" : ": `" + patttern + "`"))
    {
        this.Error = error;
        this.Patttern = patttern;
    }
}
