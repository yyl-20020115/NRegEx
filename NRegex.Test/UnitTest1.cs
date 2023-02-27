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
    public static bool RunProcess(string filePath, string argument)
    {
        var p = new Process();

        p.StartInfo.FileName = GetFullPath(filePath);
        p.StartInfo.Arguments = argument;
        if (p.Start())
        {
            p.WaitForExit();
            return true;
        }
        return false;
    }
    public static void ExportAsDot(Graph graph, string png = "graph.png", string dot = "graph.dot")
    {
        File.WriteAllText(dot, RegExGraphBuilder.ExportAsDot(graph).ToString());
        RunProcess("dot.exe", $"-T png {Path.Combine(Environment.CurrentDirectory, dot)} -o {Path.Combine(Environment.CurrentDirectory, png)}");
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
        Assert.IsTrue(regex0.IsMatch("abcd"));
    }
    [TestMethod]
    public void TestMethod1()
    {
        var regexString0 = "a|b";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
        //dot -T png  graph.dot -o graph.png
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("b"));
    }
    [TestMethod]
    public void TestMethod2()
    {
        var regexString0 = "a*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
        //dot -T png  graph.dot -o graph.png
        Assert.IsTrue(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("aaa"));
    }
    [TestMethod]
    public void TestMethod3()
    {
        var regexString0 = "a+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
        //dot -T png  graph.dot -o graph.png
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("aaa"));
    }
    [TestMethod]
    public void TestMethod4()
    {
        var regexString0 = "a+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0.Graph);
        //dot -T png  graph.dot -o graph.png
        Assert.IsFalse(regex0.IsMatch(""));
        Assert.IsTrue(regex0.IsMatch("a"));
        Assert.IsTrue(regex0.IsMatch("aa"));
        Assert.IsTrue(regex0.IsMatch("aaa"));
    }



    [TestMethod]
    public void TestMethod8()
    {
        var regexString3 = "a|b";
        var regex3 = new Regex(regexString3);
        Debug.WriteLine(regex3.Pattern);
        Debug.WriteLine(regex3.Graph);

        var regexString4 = "(a|b)*";
        var regex4 = new Regex(regexString4);
        Debug.WriteLine(regex4.Pattern);
        Debug.WriteLine(regex4.Graph);

        var regexString5 = "1(0|1)*101";
        var regex5 = new Regex(regexString5);
        Debug.WriteLine(regex5.Pattern);
        Debug.WriteLine(regex5.Graph);

        var regexString6 = "0*10*10*10*";
        var regex6 = new Regex(regexString6);
        Debug.WriteLine(regex6.Pattern);
        Debug.WriteLine(regex6.Graph);

        var regexString7 = "1(1010*|1(010)*1)*0";
        var regex7 = new Regex(regexString7);
        Debug.WriteLine(regex7.Pattern);
        Debug.WriteLine(regex7.Graph);
    }
}