using NRegEx;
using System;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NRegex.Test;

[TestClass]
public class BasicUnitTests
{
    static BasicUnitTests()
    {
        Environment.CurrentDirectory =
            Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Graphs\\");
    }
    public static string GetApplicationFullPath(string filePath)
    {
        var text = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(text))
        {
            var paths = text.Split(";");

            foreach (var path in paths)
            {
                var fp = Path.Combine(path, filePath);
                if (File.Exists(fp))
                {
                    filePath = fp;
                    break;
                }
            }
        }
        return filePath;
    }
    public static int RunProcess(string filePath, string argument)
    {
        var p = new Process();

        p.StartInfo.FileName = GetApplicationFullPath(filePath);
        p.StartInfo.Arguments = argument;
        if (p.Start())
        {
            p.WaitForExit();
            return p.ExitCode;
        }
        return -1;
    }

    public static int ExportAsDot(Regex regex, string? png = null, string? dot = null)
        => ExportAsDot(regex.Graph, png, dot);

    public static int ExportAsDot(Graph graph, string? png = null, string? dot = null) 
        => ExportAsDot(RegExGraphBuilder.ExportAsDot(graph).ToString(), png, dot);
    public static int ExportAsDot(string content, string? png = null, string? dot = null)
    {
        var trace = new StackTrace();
        var fnn = "graph";
        var depth = 1;
        do
        {
            fnn = trace?.GetFrame(depth++)?.GetMethod()?.Name;
        } while (fnn == nameof(ExportAsDot));

        fnn ??= "graph";
        png ??= fnn + ".png";
        dot ??= fnn + ".dot";
        dot = Path.Combine(Environment.CurrentDirectory, dot);
        png = Path.Combine(Environment.CurrentDirectory, png);
        File.WriteAllText(dot, content);
        return RunProcess("dot.exe", $"-Grankdir=LR -T png {dot} -o {png}");
    }

    [TestMethod]
    public void TestEscapse()
    {
        Assert.AreEqual(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\.", Regex.Escape(@"|()[]{}^$*+?\."));
        Assert.AreEqual(@"|()[]{}^$*+?\.", Regex.Unescape(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\."));
    }
    [TestMethod]
    public void TestMethod00()
    {
        var regexString0 = "abcd";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        //dot -T png  graph.dot -o graph.png
        Assert.IsFalse(regex0.IsCompletelyMatch("bcda"));
        Assert.IsTrue(regex0.IsCompletelyMatch("abcd"));
    }

    [TestMethod]
    public void TestMethod01()
    {
        var regexString0 = "a|ab|c";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsTrue(regex0.IsCompletelyMatch("ab"));
        Assert.IsTrue(regex0.IsCompletelyMatch("c"));
    }
    [TestMethod]
    public void TestMethod02()
    {
        var regexString0 = "a*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("b"));
    }
    [TestMethod]
    public void TestMethod03()
    {
        var regexString0 = "a+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("b"));
    }
    [TestMethod]
    public void TestMethod04()
    {
        var regexString0 = "a?";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aa"));
    }
    [TestMethod]
    public void TestMethod05()
    {
        var regexString0 = "(a|b)+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("b"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod06()
    {
        var regexString0 = "(a|b)*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsTrue(regex0.IsCompletelyMatch("b"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod07()
    {
        var regexString0 = "1(0|1)*101";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("1101"));
        Assert.IsTrue(regex0.IsCompletelyMatch("11101"));
        Assert.IsTrue(regex0.IsCompletelyMatch("1111101"));
        Assert.IsTrue(regex0.IsCompletelyMatch("1000000101"));
    }
    [TestMethod]
    public void TestMethod08()
    {
        var regexString0 = "0*10*10*10*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("00010010001000"));
    }
    [TestMethod]
    public void TestMethod09()
    {
        var regexString0 = "1(1010*|1(010)*1)*0";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("110100101000"));
    }
    [TestMethod]
    public void TestMethod10()
    {
        var regexString0 = ".";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsFalse(regex0.IsCompletelyMatch("ab"));
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
    }
    [TestMethod]
    public void TestMethod11()
    {
        var regexString0 = ".*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("abcdef"));

    }
    [TestMethod]
    public void TestMethod12()
    {
        var regexString0 = ".+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("abcdef"));

    }
    [TestMethod]
    public void TestMethod13()
    {
        var regexString0 = "a{2}";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaa"));

    }
    [TestMethod]
    public void TestMethod14()
    {
        var regexString0 = "a{2,}";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaaa"));

    }
    [TestMethod]
    public void TestMethod15()
    {
        var regexString0 = "a{2,3}";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));

    }
    [TestMethod]
    public void TestMethod16()
    {
        var regexString0 = "[a-zA-Z0-9_]";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("a"));
        Assert.IsTrue(regex0.IsCompletelyMatch("A"));
        Assert.IsTrue(regex0.IsCompletelyMatch("_"));
        Assert.IsTrue(regex0.IsCompletelyMatch("8"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));
    }
    [TestMethod]
    public void TestMethod17()
    {
        var regexString0 = "[^a-zA-Z0-9_]";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsFalse(regex0.IsCompletelyMatch("a"));
        Assert.IsFalse(regex0.IsCompletelyMatch("A"));
        Assert.IsFalse(regex0.IsCompletelyMatch("_"));
        Assert.IsFalse(regex0.IsCompletelyMatch("8"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));
    }
    [TestMethod]
    public void TestMethod18()
    {
        var regexString0 = @"\b\B\cx\d\D\f\n\r\s\S\t\v\w\W\xn\num\n\nm\nml\un";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));

    }

    [TestMethod]
    public void TestMethod19()
    {
        var regexString0 = @"";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsCompletelyMatch(""));
        Assert.IsTrue(regex0.IsCompletelyMatch("aa"));
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));
    }
    [TestMethod]
    public void TestMethod20()
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
        };

        var regs2 = new string[]
        {
            "[ab]{4,6}",
            "[ab]{4,6}c",
            "(a|b)*ab",
            "[A-Za-z0-9]",
            "[A-Za-z0-9_]",
            "[A-Za-z]",
            "[ \t]",
            @"[(?<=\W)(?=\w)|(?<=\w)(?=\W)]",
            "[\x00-\x1F\x7F]",
            "[0-9]",
            "[^0-9]",
            "[\x21-\x7E]",
            "[a-z]",
            "[\x20-\x7E]",
            "[ \t\r\n\v\f]",
            "[^ \t\r\n\v\f]",
            "[A-Z]",
            "[A-Fa-f0-9]",
            "in[du]",
            "x[0-9A-Z]",
            "[^A-M]in",
            ".gr",
            @"\(.*l",
            "W*in",
            "[xX][0-9a-z]",
            @"\(\(\(ab\)*c\)*d\)\(ef\)*\(gh\)\{2\}\(ij\)*\(kl\)*\(mn\)*\(op\)*\(qr\)*",
            @"((mailto\:|(news|(ht|f)tp(s?))\://){1}\S+)",
            @"^http\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$",
            @"^([1-zA-Z0-1@.\s]{1,255})$",
            "[A-Z][0-9A-Z]{10}",
            "[A-Z][A-Za-z0-9]{10}",
            "[A-Za-z0-9]{11}",
            "[A-Za-z]{11}",
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
            @"^(?n:[a-z0-9])+$",
            @"^(?x:[a-z0-9])+$",
            "\\S+.*",
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
    }
}
