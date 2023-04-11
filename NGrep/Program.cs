/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NRegEx;
using CommandLine;
namespace NGrep;

public class Options
{
    [Option('r', "regexp", Required = true,
      HelpText = "Regular expression to be processed.")]
    public string RegExpr { get; set; } = "";

    [Option('i', "input", Required = false,
      HelpText = "Input file to be processed.")]
    public string InputFile { get; set; } = "";

    [Option('n', "line-number", Required = false,
      HelpText = "Print the line number.")]
    public bool PrintLineNumber { get; set; } = false;

    [Option('o', "only-matching", Required = false,
      HelpText = "Print only the matching part")]
    public bool PrintOnlyMatchingPart { get; set; } = false;

    [Option('m', "max-count", Required = false,
      HelpText = "Stop after numer of matches")]
    public long MaxMatches { get; set; } = long.MaxValue;

    [Option('a', "after-context", Required = false,
      HelpText = "Print number of context lines trailing match")]
    public int AfterContext { get; set; } = 0;

    [Option('b', "before-context", Required = false,
      HelpText = "Print number of context lines leading match")]
    public int BeforeContext { get; set; } = 0;

    [Option('t', "context", Required = false,
      HelpText = "Print number of context lines leading & trailing match")]
    public int Context { get; set; } = 0;

    [Option('c', "count", Required = false,
      HelpText = "Print only count of matching lines per FILE")]
    public bool CountOnly { get; set; } = false;

    [Option('f', "with-filename", Required = false,
      HelpText = "Print filename for each match")]
    public bool WithFileName { get; set; } = false;

    [Option('u', "binary", Required = false,
      HelpText = "Do not strip CR characters at EOL (MSDOS/Windows)")]
    public bool DoNotStripCR { get; set; } = false;

    // Omitting long name, default --verbose
    [Option('v', "verbose", Required = false,
      HelpText = "Prints all diagnostic messages to standard output.")]
    public bool Verbose { get; set; } = false;

    [Option('d', "detect", Required = false,
      HelpText = "Detect Catastrophic Backtracking problem")]
    public bool DetectCBT { get; set; } = false;
}

public static class Program
{
    private static int Count = 0;
    private static Parser Parser = Parser.Default;
    public static void PrintMatch(Options options, string line, int linenumber, Match match, string filename)
    {
        if (options.PrintLineNumber)
            Console.Write($"[{linenumber + 1}] ");
        if (options.WithFileName)
            Console.Write($"[{filename}] ");
        if (options.PrintOnlyMatchingPart)
            Console.Write(line[match.InclusiveStart..match.ExclusiveEnd]);
        else
            Console.Write(line);
        Console.WriteLine();
    }

    public static void Finalise(Options options)
    {
        if (options.CountOnly)
            Console.WriteLine($"Number of matches: {Count}");
        if (options.Verbose)
            Console.WriteLine("Done.");
    }

    public static void OnCommandLineParseFail()
    {
        Console.WriteLine("Command line parse failure");
        Console.WriteLine($"Help: {Parser.Settings.HelpWriter}");
    }

    public static void PrintOptions(Options options)
    {
        Console.WriteLine("Options:");
        Console.WriteLine($"After Context: {options.AfterContext}");
        Console.WriteLine($"Before Context: {options.BeforeContext}");
        Console.WriteLine($"Context: {options.Context}");
        Console.WriteLine($"Count only: {options.CountOnly}");
        Console.WriteLine($"Do not strip CR: {options.DoNotStripCR}");
        Console.WriteLine($"Input filename: {options.InputFile}");
        Console.WriteLine($"Max Matches: {options.MaxMatches}");
        Console.WriteLine($"Print line number: {options.PrintLineNumber}");
        Console.WriteLine($"Print only matching part: {options.PrintOnlyMatchingPart}");
        Console.WriteLine($"Regular expression: {options.RegExpr}");
        Console.WriteLine($"Verbose: {options.Verbose}");
        Console.WriteLine($"With filename: {options.WithFileName}");
        Console.WriteLine($"Detect Catastrophic Backtracking problem: {options.DetectCBT}");
    }
    public static void Main(string[] args)
    {
        try
        {
            Parser = new(settings =>
            {
                settings.CaseSensitive = true;
                settings.HelpWriter = Console.Out;
                settings.IgnoreUnknownArguments = false;
                settings.AutoHelp = true;
                settings.AutoVersion = true;
            });

            var result = Parser.ParseArguments<Options>(args);

            if (result.Value != null)
            {
                PlatformRegEx(result.Value);
            }
            else
            {
                Console.WriteLine("Input was not valid.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error (Exception): {ex.Message}");
        }
    }
    public static void PlatformRegEx(Options options)
    {
        if (options.Verbose) PrintOptions(options);
        if (options.DetectCBT)
        {
            var regexpr = options.RegExpr;
            if (!string.IsNullOrEmpty(regexpr)
                && regexpr.StartsWith('"')
                && regexpr.EndsWith('"'))
            {
                regexpr = regexpr[1..^1];
            }
            if (!string.IsNullOrEmpty(regexpr))
            {
                var result = RegExGraphCBTDetector.DetectCatastrophicBacktracking(regexpr);
                if (result != null)
                {
                    Console.WriteLine($"RegEx: {result.Regex}");
                    Console.WriteLine($"  CBT: {result.Type}");
                    Console.WriteLine($"  NID: {result.NodeId}");
                    Console.WriteLine($"  Pos: {result.Position}");
                    Console.WriteLine($"  Len: {result.Length}");
                    Console.WriteLine($"  Att: {result.Attacker}");
                }
            }

            return;
        }

        var regex = new Regex(options.RegExpr);

        if (options.Context > 0)
        {
            options.AfterContext = options.Context;
            options.BeforeContext = options.Context;
        }

        using var reader = new StreamReader(options.InputFile);
        string? line;
        int i = 0;
        while((line = reader.ReadLine()) != null)
        {
            i++;
            if (!options.DoNotStripCR)
            {
                line = line.Replace("\r", "");
            }

            foreach (var match in regex.Matches(line))
            {
                if (!options.CountOnly)
                {
                    PrintMatch(options, line, i, match, options.InputFile);
                }
                if (++Count >= options.MaxMatches)
                {
                    Console.WriteLine($"Maximum number of matches reached ({options.MaxMatches})");
                    Finalise(options);
                    return;
                }
            }
        }

        Finalise(options);
    }
}
