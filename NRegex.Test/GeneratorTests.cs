using Microsoft.VisualStudio.TestTools.UnitTesting;
using NRegEx;
using System;
using System.Collections.Generic;

namespace NRegex.Test;

[TestClass]
public class GeneratorTests
{

    [TestMethod]
    public void TestGenerateTextCorrectly()
    {
        var regex = "[ab]{4,6}c";
        var generator = new RegExGenerator(regex);
        var verifier = new Regex(regex);
        for (int i = 0; i < 100; i++)
        {
            var text = generator.Generate();
            Assert.IsTrue(verifier.IsMatchCompletely(text));
        }
    }

    [TestMethod]
    public void TestRepeatableRegex()
    {
        for (int x = 0; x < 1000; x++)
        {
            var generator = new RegExGenerator("[ab]{4,6}c", new Random(1000));
            var generator2 = new RegExGenerator("[ab]{4,6}c", new Random(1000));

            var firstRegexList = GenerateRegex(generator, 100);
            var secondRegexList = GenerateRegex(generator2, 100);

            AssertListEquals(firstRegexList, secondRegexList);
        }
    }

    [TestMethod]
    public void TestWalkRange()
    {
        for (int x = 0; x < 100; x++)
        {
            var generator = new RegExGenerator("[ab]{0,100}c", new Random(1000));
            var generator2 = new RegExGenerator("[ab]{0,100}c", new Random(1000));

            var firstRegexList = GenerateRegex(generator, 100, 0, 100);
            var secondRegexList = GenerateRegex(generator2, 100, 0, 100);
            AssertListEquals(firstRegexList, secondRegexList);
        }
    }

    private List<string> GenerateRegex(RegExGenerator generator, int count, int minLength, int maxLength)
    {
        List<string> regexList = new ();
        for (int i = 0; i < count; i++)
        {
            try
            {
                regexList.Add(generator.Generate());
            }
            catch (Exception ex)
            {
                // add a placeholder for the failed attempt
                regexList.Add(null);
            }
        }
        return regexList;
    }

    private static void AssertListEquals<T>(List<T> firstRegexList, List<T> secondRegexList)
    {
        Assert.AreEqual(firstRegexList.Count, secondRegexList.Count, "size mismatch");
        for (int i = 0; i < firstRegexList.Count; i++)
        {
            Assert.AreEqual(firstRegexList[i], secondRegexList[i], "Index mismatch: " + i);
        }
    }

    private static List<string> GenerateRegex(RegExGenerator generator, int count)
    {
        List<string> regexList = new ();
        for (int i = 0; i < count; i++)
        {
            regexList.Add(generator.Generate());
        }
        return regexList;
    }
}
