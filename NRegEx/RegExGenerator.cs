/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace NRegEx;

public class RegExGenerator
{
    public static string RemoveStartEndMarkers(string regExp)
    {
        if (regExp.StartsWith("^", StringComparison.Ordinal))
        {
            regExp = regExp[1..];
        }
        if (regExp.EndsWith("$", StringComparison.Ordinal))
        {
            regExp = regExp[..^1];
        }

        return regExp;
    }

    public readonly Regex Regex;
    public readonly Random Random;
    public readonly int MaxRetries;

    public RegExGenerator(string regex, Random? random = null, int maxRetries = 8)
            : this(new Regex(RemoveStartEndMarkers(regex)), random) { }
    public RegExGenerator(Regex regex, Random? random = null, int maxRetries = 8)
    {
        this.Regex = regex;
        this.MaxRetries = maxRetries;
        this.Random = random ?? Random.Shared;
    }

    protected virtual int GenerateRandomRune(int[]? runes)
        => runes == null ? '\0' : runes[Random.Next(runes.Length)];

    protected virtual RegExNode GetRandomNode(List<RegExNode> nodes)
        => nodes[Random.Next(nodes.Count)];

    protected virtual int GetBetween(int minLength, int maxLength)
        => this.Random.Next(minLength, maxLength);

    protected virtual StringBuilder? GenerateUnion(RegExNode node, StringBuilder? builder = null)
        => this.Generate(this.GetRandomNode(node.Children), builder);
    protected virtual StringBuilder? GenerateSequence(RegExNode node, StringBuilder? builder = null)
    {
        node.Children.ForEach(c => this.Generate(c, builder));
        return builder;
    }
    protected virtual StringBuilder? GenerateCapture(RegExNode node, StringBuilder? builder)
    {
        if (node.Children.Count > 0) this.Generate(node.Children[0], builder);
        return builder;
    }

    protected virtual StringBuilder? GenerateRune(int[] runes, int count, StringBuilder? builder)
    {
        for (int i = 0; i < count; i++)
        {
            builder?.Append(char.ConvertFromUtf32(GenerateRandomRune(runes)));
        }
        return builder;
    }
    protected virtual int[] GetRunes(int[] runes, bool inverted)
    {
        if (inverted)
        {
            var set = new HashSet<int>(Node.AllChars);
            set.ExceptWith(runes);
            runes = set.ToArray();
        }
        return runes;
    }
    protected virtual StringBuilder? GenerateLiteral(RegExNode node, StringBuilder? builder = null)
        => !string.IsNullOrEmpty(node.Value) && node.Length >= 0 ? (builder?.Append(node.Value)) : builder;
    protected virtual StringBuilder? GenerateRune(RegExNode node, StringBuilder? builder = null)
        => this.GenerateRune(
            this.GetRunes(node.Runes ?? Array.Empty<int>(), node.Inverted), 1, builder);

    protected virtual StringBuilder? GenerateAnyChar(bool withNewLine, StringBuilder? builder = null)
        => builder?.Append(char.ConvertFromUtf32(GenerateRandomRune(
                withNewLine
                ? Node.AllChars
                : Node.AllCharsWithoutNewLine)));

    protected virtual StringBuilder? GenerateRepeats(RegExNode node, StringBuilder? builder = null)
    {
        if (node.Children.Count >= 1)
        {
            var min = node.Min ?? 0;
            var max = node.Max ?? 0;
            min = min <= 0 ? 0 : min;
            max = max <= 0 ? MaxRetries : max;
            var count = this.GetBetween(min, max);
            for (int i = 0; i < count; i++)
            {
                this.Generate(node.Children[0], builder);
            }
        }
        return builder;
    }
    protected virtual StringBuilder? Generate(RegExNode node, StringBuilder? builder = null) => node.Type switch
    {
        TokenTypes.Literal => this.GenerateLiteral(node, builder),
        TokenTypes.Union => this.GenerateUnion(node, builder),
        TokenTypes.Group => this.GenerateCapture(node, builder),
        TokenTypes.Sequence => this.GenerateSequence(node, builder),
        TokenTypes.AnyCharExcludingNewLine => this.GenerateAnyChar(false, builder),
        TokenTypes.AnyCharIncludingNewLine => this.GenerateAnyChar(true, builder),
        TokenTypes.RuneClass => this.GenerateRune(node, builder),
        TokenTypes.ZeroPlus => this.GenerateRepeats(node, builder),
        TokenTypes.OnePlus => this.GenerateRepeats(node, builder),
        TokenTypes.ZeroOne => this.GenerateRepeats(node, builder),
        TokenTypes.Repeats => this.GenerateRepeats(node, builder),
        _ => new StringBuilder(),
    };


    public string Generate()
        => Generate(Regex.Model, new())?.ToString() ?? "";
}
