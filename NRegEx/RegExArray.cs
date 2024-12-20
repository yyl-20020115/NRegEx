﻿/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace NRegEx;

/// <summary>
/// All Regexs will be matched parallelly
/// during the matching stage just like one Regex
/// Other operations are the same.
/// The Name field of Capture object is the source name or pattern 
/// (if name is not given) of the Regex which matched.
/// </summary>
public class RegExArray : Regex
{
    public readonly Dictionary<string, Regex> RegexDictionary = [];
    public readonly Regex[] Regexs;
    public readonly List<RegExNode> SubModels;
    public readonly List<Graph> SubGraphs = [];
    public RegExArray(params string[] regexs)
        : this(regexs.Select(r => new Regex(r)).ToArray()) { }
    public RegExArray(params Regex[] regexs)
        : base(string.Join('|', regexs.Select(r => $"({r.Pattern})").ToArray()))
    {
        if (regexs.Length == 0)
            throw new ArgumentException(null, nameof(regexs));
        this.Model = new(TokenTypes.Union) { PatternName = this.Pattern };
        this.Graph = new(this.Model.Name,this.Model);
        this.SubModels = this.Model.Children;
        foreach (var regex in this.Regexs = regexs)
        {
            this.RegexDictionary[regex.Name] = regex;
            this.Model.Children.Add(regex.Model);
            var sub = regex.Graph.Copy();
            this.SubGraphs.Add(sub);
            this.Graph.UnionWith(sub);
        }
    }
}
