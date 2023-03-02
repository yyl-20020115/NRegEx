using CommandLine;
using NRegEx;
using System.Diagnostics;
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

public static class Program
{
    private static readonly Options Options = new();
    private static int Count = 0;
    private static Parser? Parser;

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
            Console.Write(lines[linenumber][match.InclusiveStart.. match.ExclusiveEnd]);
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

    public static void TestMethod19()
    {
        var regs = new string[] {
            @"^\d+$", //非负整数（正整数 + 0） 
            @"^[0-9]*[1-9][0-9]*$", //正整数 
            @"^((-\d+)|(0+))$", //非正整数（负整数 + 0） 
            @"^-[0-9]*[1-9][0-9]*$", //负整数 
            @"^-?\d+$", //整数 
            @"^\d+(\.\d+)?$", //非负浮点数（正浮点数 + 0） 
            @"^(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*))$", //正浮点数
            @"^((-\d+(\.\d+)?)|(0+(\.0+)?))$", //非正浮点数（负浮点数 + 0） 
            @"^(-(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*)))$", //负浮点数 
            @"^(-?\d+)(\.\d+)?$", //浮点数 
            @"^[A-Za-z]+$", //由26个英文字母组成的字符串 
            @"^[A-Z]+$", //由26个英文字母的大写组成的字符串 
            @"^[a-z]+$", //由26个英文字母的小写组成的字符串 
            @"^[A-Za-z0-9]+$", //由数字和26个英文字母组成的字符串 
            @"^\w+$", //由数字、26个英文字母或者下划线组成的字符串 
            @"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$", //email地址 
            @"^[a-zA-z]+://(\w+(-\w+)*)(\.(\w+(-\w+)*))*(\?\S*)?$", //url
            @"[ab]{4,6}",
            @"[ab]{4,6}c",
            @"(a|b)*ab",
            @"[A-Za-z0-9]",
            @"[A-Za-z0-9_]",
            @"[A-Za-z]",
            @"[ \t]",
            @"[(?<=\W)(?=\w)|(?<=\w)(?=\W)]",
            @"[\x00-\x1F\x7F]",
            @"[0-9]",
            @"[^0-9]",
            @"[\x21-\x7E]",
            @"[a-z]",
            @"[\x20-\x7E]",
            @"[ \t\r\n\v\f]",
            @"[^ \t\r\n\v\f]",
            @"[A-Z]",
            @"[A-Fa-f0-9]",
            @"in[du]",
            @"x[0-9A-Z]",
            @"[^A-M]in",
            @".gr",
            @"\(.*l",
            @"W*in",
            @"[xX][0-9a-z]",
            @"\(\(\(ab\)*c\)*d\)\(ef\)*\(gh\)\{2\}\(ij\)*\(kl\)*\(mn\)*\(op\)*\(qr\)*",
            @"((mailto\:|(news|(ht|f)tp(s?))\://){1}\S+)",
            @"^http\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$",
            @"^([1-zA-Z0-1@.\s]{1,255})$",
            @"[A-Z][0-9A-Z]{10}",
            @"[A-Z][A-Za-z0-9]{10}",
            @"[A-Za-z0-9]{11}",
            @"[A-Za-z]{11}",
            @"^[a-zA-Z''-'\s]{1,40}$",
            @"^[_a-z0-9-]+(\.[_a-z0-9-]+)*@[a-z0-9-]+(\.[a-z0-9-]+)*(\.[a-z]{2,4})$",
            @"a[a-z]",
            @"[1-9][0-9]",
            @"\d{8}",
            @"\d{5}(-\d{4})?",
            @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}",
            @"\D{8}",
            @"\D{5}(-\D{4})?",
            @"\D{1,3}\.\D{1,3}\.\D{1,3}\.\D{1,3}",
            @"^(?:[a-z0-9])+$",
            @"^(?i:[a-z0-9])+$",
            @"^(?s:[a-z0-9])+$",
            @"^(?m:[a-z0-9])+$",
            //@"^(?n:[a-z0-9])+$",
            //@"^(?x:[a-z0-9])+$",
            @"\\S+.*",
            @"^(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?$",
            @"^\s1\s+2\s3\s?4\s*$",
            @"(\s123)+",
            @"\Sabc\S{3}111",
            @"^\S\S  (\S)+$",
            @"\\abc\\d",
            @"\w+1\w{4}",
            @"\W+1\w?2\W{4}",
            @"^[^$]$"
        };

        var results = new List<string>();
        foreach (var reg in regs)
        {
            string text = "";
            try
            {
                var g = new RegExGenerator(reg);
                text = g.Generate();
                var s = g.Regex.IsMatch(text);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BADBADBAD:REG:{reg}:TEXT:{text}");
            }
        }
    }
    public static void Main(string[] args)
    {
        TestMethod19();
        string greedyPattern = @".+(\d+)\.";
        string lazyPattern = @".+?(\d+)\.";

        var regex1 = new Regex(greedyPattern);

        var regex2 = new Regex(lazyPattern);
        string input = "This sentence ends with the number 107325.";
        try
        {
            var r1 = regex1.Match(input);
            var r2 = regex2.Match(input);

            var s = r1.ToString();
            var s2 = r2.ToString();


        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }




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

            foreach (var match in regex.Matches(lines[i]).Cast<Match>())
            {
                if (!Options.CountOnly)
                {
                    if (Options.BeforeContext > 0)
                    {
                        PrintLeadingContext(lines, i, Options.BeforeContext, match, Options.InputFile);
                    }

                    PrintMatch(lines, i, match, Options.InputFile);

                    if (Options.AfterContext > 0)
                    {
                        PrintTrailingContext(lines, i, Options.AfterContext, match, Options.InputFile);
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
