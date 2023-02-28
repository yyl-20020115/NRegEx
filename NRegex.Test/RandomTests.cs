using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace NRegex.Test;
[TestClass]
public class RandomTests
{

    [TestMethod]
    public void ShouldGenerateRandomNumberCorrectly()
    {
        var random = Random.Shared;
        for (int i = 0; i < 100; i++)
        {
            int number = random.Next(3,7);
            Assert.IsTrue(number >= 3);
            Assert.IsTrue(number <= 7);
        }
    }
}

