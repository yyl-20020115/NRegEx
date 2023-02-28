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
        var fnn = new StackTrace()?.GetFrame(1)?.GetMethod()?.Name;
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
    public void TestMethod0()
    {
        var regexString0 = "abcd";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        //dot -T png  graph.dot -o graph.png
        Assert.IsFalse(regex0.IsMatch("bcda"));
        Assert.IsTrue(regex0.IsMatch("abcd"));
    }

    [TestMethod]
    public void TestMethod1()
    {
        var regexString0 = "a|ab|c";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("ab"));
        Assert.IsTrue(regex0.IsMatch("c"));
    }
    [TestMethod]
    public void TestMethod2()
    {
        var regexString0 = "a*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("aaa"));
        Assert.IsFalse(regex0.IsMatch("b"));
    }
    [TestMethod]
    public void TestMethod3()
    {
        var regexString0 = "a+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("aaa"));
        Assert.IsFalse(regex0.IsMatch("b"));
    }
    [TestMethod]
    public void TestMethod4()
    {
        var regexString0 = "a?";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsFalse(regex0.IsMatch("aa"));
    }
    [TestMethod]
    public void TestMethod5()
    {
        var regexString0 = "(a|b)+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("b"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod6()
    {
        var regexString0 = "(a|b)*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("b"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod7()
    {
        var regexString0 = "1(0|1)*101";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("1101"));
        Assert.IsTrue(regex0.IsMatch("11101"));
        Assert.IsTrue(regex0.IsMatch("1111101"));
        Assert.IsTrue(regex0.IsMatch("1000000101"));
    }
    [TestMethod]
    public void TestMethod8()
    {
        var regexString0 = "0*10*10*10*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("00010010001000"));
    }
    [TestMethod]
    public void TestMethod9()
    {
        var regexString0 = "1(1010*|1(010)*1)*0";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("110100101000"));
    }
}
