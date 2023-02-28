using Microsoft.VisualStudio.TestTools.UnitTesting;
using NRegEx;
using System;
using System.Diagnostics;
using System.IO;

namespace NRegex.Test;

[TestClass]
public class UnitTest1
{
    public static string GetFullPath(string filePath)
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

        p.StartInfo.FileName = GetFullPath(filePath);
        p.StartInfo.Arguments = argument;
        if (p.Start())
        {
            p.WaitForExit();
            return p.ExitCode;
        }
        return -1;
    }
    public static int ExportAsDot(Graph graph, string png = "graph.png", string dot = "graph.dot")
    {
        dot = Path.Combine(Environment.CurrentDirectory, dot);
        png = Path.Combine(Environment.CurrentDirectory, png);
        File.WriteAllText(dot, RegExGraphBuilder.ExportAsDot(graph).ToString());
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
        ExportAsDot(regex0.Graph);
        //dot -T png  graph.dot -o graph.png
        Assert.IsFalse(regex0.IsMatch("bcda"));
        Assert.IsTrue(regex0.IsMatch("abcd"));
    }

    [TestMethod]
    public void TestMethod1()
    {
        var regexString0 = "a|ab|c";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("ab"));
        Assert.IsTrue(regex0.IsMatch("c"));
    }
    [TestMethod]
    public void TestMethod2()
    {
        var regexString0 = "a*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
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
        ExportAsDot(regex0.Graph);
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
        ExportAsDot(regex0.Graph);
        Assert.IsTrue(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsFalse(regex0.IsMatch("aa"));
    }
    [TestMethod]
    public void TestMethod5()
    {
        var regexString0 = "(a|b)+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
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
        ExportAsDot(regex0.Graph);
        Assert.IsTrue(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("b"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod7()
    {
        var regexString5 = "1(0|1)*101";
        var regex5 = new Regex(regexString5);
        ExportAsDot(regex5.Graph);
        Assert.IsFalse(regex5.IsMatch(""));
        Assert.IsTrue(regex5.IsMatch("11101"));
        Assert.IsTrue(regex5.IsMatch("1111101"));
    }
    [TestMethod]
    public void TestMethod8()
    {
        var regexString6 = "0*10*10*10*";
        var regex6 = new Regex(regexString6);
        ExportAsDot(regex6.Graph);
        Assert.IsFalse(regex6.IsMatch(""));
        Assert.IsTrue(regex6.IsMatch("00010010001000"));

    }
    [TestMethod]
    public void TestMethod9()
    {
        var regexString7 = "1(1010*|1(010)*1)*0";
        var regex7 = new Regex(regexString7);
        ExportAsDot(regex7.Graph);
        Assert.IsFalse(regex7.IsMatch(""));
        Assert.IsTrue(regex7.IsMatch("110100101000"));
    }
}