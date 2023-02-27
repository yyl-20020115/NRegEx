using Microsoft.VisualStudio.TestTools.UnitTesting;
using NRegEx;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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
    public static bool Run(string filePath, string argument)
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



    [TestMethod]
    public void TestEscapse()
    {
        Assert.AreEqual(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\.", Regex.Escape(@"|()[]{}^$*+?\."));
        Assert.AreEqual(@"|()[]{}^$*+?\.", Regex.Unescape(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\."));
    }
    [TestMethod]
    public void TestMethod0()
    {
        var regexString0 = "ab";// "(ab|c)*abb";
        var regex0 = new Regex(regexString0);
        Debug.WriteLine(regex0);
        Debug.WriteLine(regex0.Pattern);
        Debug.WriteLine(regex0.Graph);
        var builder = Graph.ExportGraph(regex0.Graph);
        File.WriteAllText("graph.dot", builder.ToString());
        Run("dot.exe", $"-T png {Path.Combine(Environment.CurrentDirectory, "graph.dot")} -o {Path.Combine(Environment.CurrentDirectory, "graph.png")}");
        //dot -T png  graph.dot -o graph.png
        var b = regex0.IsMatch("ab");
        Debug.WriteLine(b);
    }


    [TestMethod]
    public void TestMethod1()
    {
        var regexString1 = "a*";
        var regex1 = new Regex(regexString1);
        Debug.WriteLine(regex1.Pattern);
        Debug.WriteLine(regex1.Graph);

        var regexString2 = "ab";
        var regex2 = new Regex(regexString2);
        Debug.WriteLine(regex2.Pattern);
        Debug.WriteLine(regex2.Graph);

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