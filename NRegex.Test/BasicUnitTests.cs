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
        Assert.IsTrue(regex0.IsCompletelyMatch("aaa"));
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));

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
        Assert.IsFalse(regex0.IsCompletelyMatch("aaaa"));

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

}
