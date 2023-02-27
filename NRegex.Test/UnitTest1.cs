using Microsoft.VisualStudio.TestTools.UnitTesting;
using NRegEx;
using System;

namespace NRegex.Test;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestEscapse()
    {
        Assert.AreEqual(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\.", Regex.Escape(@"|()[]{}^$*+?\."));
        Assert.AreEqual(@"|()[]{}^$*+?\.", Regex.Unescape(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\."));

    }
    [TestMethod]
    public void TestMethod0()
    {
        var regexString0 = "(ab|c)*abb";
        var regex0 = new Regex(regexString0);
        Console.WriteLine(regex0);
        Console.WriteLine(regex0.Pattern);
        Console.WriteLine(regex0.Graph);
        var b = regex0.IsMatch("ccabb");
        Console.WriteLine(b);
    }


    [TestMethod]
    public void TestMethod1()
    {




      

        var regexString1 = "a*";
        var regex1 = new Regex(regexString1);
        Console.WriteLine(regex1.Pattern);
        Console.WriteLine(regex1.Graph);

        var regexString2 = "ab";
        var regex2 = new Regex(regexString2);
        Console.WriteLine(regex2.Pattern);
        Console.WriteLine(regex2.Graph);

        var regexString3 = "a|b";
        var regex3 = new Regex(regexString3);
        Console.WriteLine(regex3.Pattern);
        Console.WriteLine(regex3.Graph);

        var regexString4 = "(a|b)*";
        var regex4 = new Regex(regexString4);
        Console.WriteLine(regex4.Pattern);
        Console.WriteLine(regex4.Graph);

        var regexString5 = "1(0|1)*101";
        var regex5 = new Regex(regexString5);
        Console.WriteLine(regex5.Pattern);
        Console.WriteLine(regex5.Graph);

        var regexString6 = "0*10*10*10*";
        var regex6 = new Regex(regexString6);
        Console.WriteLine(regex6.Pattern);
        Console.WriteLine(regex6.Graph);

        var regexString7 = "1(1010*|1(010)*1)*0";
        var regex7 = new Regex(regexString7);
        Console.WriteLine(regex7.Pattern);
        Console.WriteLine(regex7.Graph);

    }
}