/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

public class RegExSyntaxException(string error, string patttern = "") : Exception("Error parsing regexp: " + error + (string.IsNullOrEmpty(patttern) ? "" : ": `" + patttern + "`"))
{
    public readonly string Error = error;
    public readonly string Patttern = patttern;
}
