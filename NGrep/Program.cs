using CommandLine;
using System.Text.RegularExpressions;

namespace NGrep;

public class Options
{
    [Option('r', "regexp", Required = true,
      HelpText = "Regular expression to be processed.")]
    public string Regexpr { get; set; } = "";

    [Option('i', "input", Required = true,
      HelpText = "Input file to be processed.")]
    public string Inputfile { get; set; } = "";

    [Option('n', "line-number", Required = false,
      HelpText = "Print the line number.")]
    public bool PrintLineNumber { get; set; } = false;

    [Option('o', "only-matching", Required = false,
      HelpText = "Print only the matching part")]
    public bool PrintOnlymatchingPart { get; set; } = false;

    [Option('m', "max-count", Required = false,
      HelpText = "Stop after numer of matches")]
    public long MaxMatches { get; set; } = long.MaxValue;

    [Option('A', "after-context", Required = false,
      HelpText = "print number of context lines trailing match")]
    public int AfterContext { get; set; } = 0;

    [Option('B', "before-context", Required = false,
      HelpText = "print number of context lines leading match")]
    public int BeforeContext { get; set; } = 0;

    [Option('C', "context", Required = false,
      HelpText = "print number of context lines leading & trailing match")]
    public int Context { get; set; } = 0;

    [Option('c', "count", Required = false,
      HelpText = "Print only count of matching lines per FILE")]
    public bool Countonly { get; set; } = false;

    [Option('H', "with-filename", Required = false,
      HelpText = "Print filename for each match")]
    public bool WithFilename { get; set; } = false;

    [Option('U', "binary", Required = false,
      HelpText = "Do not strip CR characters at EOL (MSDOS/Windows)")]
    public bool DoNotStripCR { get; set; } = false;

    // Omitting long name, default --verbose
    [Option('V', "verbose", Required = false,
      HelpText = "Prints all diagnostic messages to standard output.")]
    public bool Verbose { get; set; } = false;
}

public class Program
{
    public static Options Options = new ();
    public static int Count = 0;
    public static Parser? Parser;

    public static void PrintLeadingContext(string[] lines, int linenumber, int num_context, Match m, string filename)
    {
        int start = linenumber - num_context;

        if (start < 0)
        {
            start = 0;
        }

        Console.WriteLine();
        for (int i = start; i < linenumber; i++)
        {
            PrintMatch(lines, i, m, filename);
        }
    }

    public static void PrintTrailingContext(string[] lines, int linenumber, int num_context, Match m, string filename)
    {
        int end = linenumber + num_context + 1;

        if (end > lines.Length)
        {
            end = lines.Length;
        }

        for (int i = linenumber + 1; i < end; i++)
        {
            PrintMatch(lines, i, m, filename);
        }
        Console.WriteLine();
    }

    public static void PrintMatch(string[] lines, int linenumber, Match m, string filename)
    {
        if (Options.PrintLineNumber)
        {
            Console.Write(string.Format("[{0}] ", linenumber + 1));
        }

        if (Options.WithFilename)
        {
            Console.Write(string.Format("[{0}] ", filename));
        }

        if (Options.PrintOnlymatchingPart)
        {
            Console.Write(lines[linenumber].Substring(m.Index, m.Length));
        }
        else
        {
            Console.Write(lines[linenumber]);
        }
        Console.WriteLine();
    }

    public static void Finalise()
    {
        if (Options.Countonly)
        {
            Console.WriteLine("Number of matches: {0}", Count);
        }

        if (Options.Verbose)
        {
            Console.WriteLine("Done.");
        }
    }

    static void OnCommandLineParseFail()
    {
        Console.WriteLine("Command line parse failure");

        Console.WriteLine("Help: " + Parser!.Settings.HelpWriter.ToString());
    }

    static void PrintOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("After Context: " + Options.AfterContext.ToString());
        Console.WriteLine("Before Context: " + Options.BeforeContext.ToString());
        Console.WriteLine("Context: " + Options.Context.ToString());
        Console.WriteLine("Count only: " + Options.Countonly.ToString());
        Console.WriteLine("Do not strip CR: " + Options.DoNotStripCR.ToString());
        Console.WriteLine("Input filename: " + Options.Inputfile.ToString());
        Console.WriteLine("Max Matches: " + Options.MaxMatches.ToString());
        Console.WriteLine("Print line number: " + Options.PrintLineNumber.ToString());
        Console.WriteLine("Print only matching part: " + Options.PrintOnlymatchingPart.ToString());
        Console.WriteLine("Regular expression: " + Options.Regexpr.ToString());
        Console.WriteLine("Verbose: " + Options.Verbose.ToString());
        Console.WriteLine("With filename: " + Options.WithFilename.ToString());
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
            
            var result = Parser.ParseArguments(()=>Options, args);

            if (result.Value!=null)
            {
                if (Options.Verbose)
                {
                    PrintOptions();
                }

                var s = File.ReadAllText(Options.Inputfile);

                var regex = new Regex(Options.Regexpr);

                if (Options.Context > 0)
                {
                    Options.AfterContext = Options.Context;
                    Options.BeforeContext = Options.Context;
                }

                var lines = s.Split(new[] { '\n' }, StringSplitOptions.None);

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
                        if (!Options.Countonly)
                        {
                            if (Options.BeforeContext > 0)
                            {
                                PrintLeadingContext(lines, i, Options.BeforeContext, m, Options.Inputfile);
                            }

                            PrintMatch(lines, i, m, Options.Inputfile);

                            if (Options.AfterContext > 0)
                            {
                                PrintTrailingContext(lines, i, Options.AfterContext, m, Options.Inputfile);
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
}
