/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            int number = random.Next(3, 7);
            Assert.IsTrue(number >= 3);
            Assert.IsTrue(number <= 7);
        }
    }
}

