using CommandLine;
using System.Text.RegularExpressions;

namespace NGrep;

public class Options
{
    [Option('r', "regexp", Required = true,
      HelpText = "Regular expression to be processed.")]
    public string RegExpr { get; set; } = "";

    [Option('i', "input", Required = true,
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
      HelpText = "print number of context lines trailing match")]
    public int AfterContext { get; set; } = 0;

    [Option('b', "before-context", Required = false,
      HelpText = "print number of context lines leading match")]
    public int BeforeContext { get; set; } = 0;

    [Option('t', "context", Required = false,
      HelpText = "print number of context lines leading & trailing match")]
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
}

public class Program
{
    protected static Options Options = new ();
    protected static int Count = 0;
    protected static Parser? Parser;

    public static void PrintLeadingContext(string[] lines, int line_number, int num_context, Match m, string filename)
    {
        int start = line_number - num_context;

        if (start < 0)
        {
            start = 0;
        }

        Console.WriteLine();
        for (int i = start; i < line_number; i++)
        {
            PrintMatch(lines, i, m, filename);
        }
    }

    public static void PrintTrailingContext(string[] lines, int line_number, int num_context, Match m, string filename)
    {
        int end = line_number + num_context + 1;

        if (end > lines.Length)
        {
            end = lines.Length;
        }

        for (int i = line_number + 1; i < end; i++)
        {
            PrintMatch(lines, i, m, filename);
        }
        Console.WriteLine();
    }

    public static void PrintMatch(string[] lines, int linenumber, Match match, string filename)
    {
        if (Options.PrintLineNumber)
        {
            Console.Write(string.Format("[{0}] ", linenumber + 1));
        }

        if (Options.WithFileName)
        {
            Console.Write(string.Format("[{0}] ", filename));
        }

        if (Options.PrintOnlyMatchingPart)
        {
            Console.Write(lines[linenumber].Substring(match.Index, match.Length));
        }
        else
        {
            Console.Write(lines[linenumber]);
        }
        Console.WriteLine();
    }

    public static void Finalise()
    {
        if (Options.CountOnly)
        {
            Console.WriteLine("Number of matches: {0}", Count);
        }

        if (Options.Verbose)
        {
            Console.WriteLine("Done.");
        }
    }

    public static void OnCommandLineParseFail()
    {
        Console.WriteLine("Command line parse failure");

        Console.WriteLine("Help: " + Parser!.Settings.HelpWriter);
    }

    public static void PrintOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("After Context: " + Options.AfterContext);
        Console.WriteLine("Before Context: " + Options.BeforeContext);
        Console.WriteLine("Context: " + Options.Context);
        Console.WriteLine("Count only: " + Options.CountOnly);
        Console.WriteLine("Do not strip CR: " + Options.DoNotStripCR);
        Console.WriteLine("Input filename: " + Options.InputFile);
        Console.WriteLine("Max Matches: " + Options.MaxMatches);
        Console.WriteLine("Print line number: " + Options.PrintLineNumber);
        Console.WriteLine("Print only matching part: " + Options.PrintOnlyMatchingPart);
        Console.WriteLine("Regular expression: " + Options.RegExpr);
        Console.WriteLine("Verbose: " + Options.Verbose);
        Console.WriteLine("With filename: " + Options.WithFileName);
    }

    public static void Main(string[] args)
    {
        try
        {
            Parser = new Parser(s =>
            {
                s.CaseSensitive = true;
                s.HelpWriter = Console.Out;
                s.IgnoreUnknownArguments = false;
                s.AutoHelp = true;
                s.AutoVersion = true;
            });
            
            var result = Parser.ParseArguments<Options>(args);

            if (result.Value != null)
            {
                PlatformRegEx();
            }
            else
            {
                Console.WriteLine("Input was not valid.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error (Exception): " + ex.Message);
        }
    }
    public static void PlatformRegEx()
    {
        if (Options.Verbose)
        {
            PrintOptions();
        }

        var input = File.ReadAllText(Options.InputFile);

        var regex = new Regex(Options.RegExpr);

        if (Options.Context > 0)
        {
            Options.AfterContext = Options.Context;
            Options.BeforeContext = Options.Context;
        }

        var lines = input.Split(new[] { '\n' }, StringSplitOptions.None);

        if (Options.Verbose)
        {
            Console.WriteLine("Lines read: " + lines.Length);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (!Options.DoNotStripCR)
            {
                lines[i] = lines[i].Replace("\r", "");
            }

            foreach (Match m in regex.Matches(lines[i]))
            {
                if (!Options.CountOnly)
                {
                    if (Options.BeforeContext > 0)
                    {
                        PrintLeadingContext(lines, i, Options.BeforeContext, m, Options.InputFile);
                    }

                    PrintMatch(lines, i, m, Options.InputFile);

                    if (Options.AfterContext > 0)
                    {
                        PrintTrailingContext(lines, i, Options.AfterContext, m, Options.InputFile);
                    }
                }

                Count++;
                if (Count >= Options.MaxMatches)
                {
                    Console.WriteLine("Maximum number of matches reached ({0})", Options.MaxMatches);
                    Finalise();
                    return;
                }
            }
        }

        Finalise();
    }
}
