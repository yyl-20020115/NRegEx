/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NRegEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using static System.Net.WebRequestMethods;

namespace NRegex.Test;

[TestClass]
public class BasicUnitTests
{
    static BasicUnitTests()
    {
        Environment.CurrentDirectory =
            System.IO.Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Graphs\\");
    }

    public static int ExportAsDot(Regex regex, string? png = null, string? dot = null)
        => GraphUtils.ExportAsDot(regex, png, dot);

    [TestMethod]
    public void TestEscapes()
    {
        Assert.AreEqual(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\.", Utils.Escape(@"|()[]{}^$*+?\."));
        Assert.AreEqual(@"|()[]{}^$*+?\.", Utils.Unescape(@"\|\(\)\[\]\{\}\^\$\*\+\?\\\."));
    }
    [TestMethod]
    public void TestMethod00()
    {
        var regexString0 = "a";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsFalse(regex0.IsFullyMatch("bcda"));
    }

    [TestMethod]
    public void TestMethod01()
    {
        var regexString0 = "a|ab|c";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsTrue(regex0.IsFullyMatch("ab"));
        Assert.IsTrue(regex0.IsFullyMatch("c"));
    }
    [TestMethod]
    public void TestMethod02()
    {
        var regexString0 = "a*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsTrue(regex0.IsFullyMatch("aaa"));
        Assert.IsFalse(regex0.IsFullyMatch("b"));
    }
    [TestMethod]
    public void TestMethod03()
    {
        var regexString0 = "a+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsTrue(regex0.IsFullyMatch("aaa"));
        Assert.IsFalse(regex0.IsFullyMatch("b"));
    }
    [TestMethod]
    public void TestMethod04()
    {
        var regexString0 = "a?";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsFalse(regex0.IsFullyMatch("aa"));
    }
    [TestMethod]
    public void TestMethod05()
    {
        var regexString0 = "(a|b)+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("b"));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsTrue(regex0.IsFullyMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod06()
    {
        var regexString0 = "(a|b)*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsTrue(regex0.IsFullyMatch("b"));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsTrue(regex0.IsFullyMatch("bbb"));
    }
    [TestMethod]
    public void TestMethod07()
    {
        var regexString0 = "1(0|1)*101";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("1101"));
        Assert.IsTrue(regex0.IsFullyMatch("11101"));
        Assert.IsTrue(regex0.IsFullyMatch("1111101"));
        Assert.IsTrue(regex0.IsFullyMatch("1000000101"));
    }
    [TestMethod]
    public void TestMethod08()
    {
        var regexString0 = "0*10*10*10*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("00010010001000"));
    }
    [TestMethod]
    public void TestMethod09()
    {
        var regexString0 = "1(1010*|1(010)*1)*0";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("110100101000"));
    }
    [TestMethod]
    public void TestMethod10()
    {
        var regexString0 = ".";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsFalse(regex0.IsFullyMatch("ab"));
        Assert.IsFalse(regex0.IsFullyMatch(""));
    }
    [TestMethod]
    public void TestMethod11()
    {
        var regexString0 = ".*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsTrue(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("abcdef"));

    }
    [TestMethod]
    public void TestMethod12()
    {
        var regexString0 = ".+";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("abcdef"));

    }
    [TestMethod]
    public void TestMethod13()
    {
        var regexString0 = "a{2}";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsFalse(regex0.IsFullyMatch("aaa"));

    }
    [TestMethod]
    public void TestMethod14()
    {
        var regexString0 = "a{2,}";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsTrue(regex0.IsFullyMatch("aaa"));
        Assert.IsTrue(regex0.IsFullyMatch("aaaa"));

    }
    [TestMethod]
    public void TestMethod15()
    {
        var regexString0 = "a{2,3}";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("aa"));
        Assert.IsTrue(regex0.IsFullyMatch("aaa"));
        Assert.IsFalse(regex0.IsFullyMatch("aaaa"));

    }
    [TestMethod]
    public void TestMethod16()
    {
        var regexString0 = "[a-zA-Z0-9_]";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsTrue(regex0.IsFullyMatch("a"));
        Assert.IsTrue(regex0.IsFullyMatch("A"));
        Assert.IsTrue(regex0.IsFullyMatch("_"));
        Assert.IsTrue(regex0.IsFullyMatch("8"));
        Assert.IsFalse(regex0.IsFullyMatch("aaaa"));
    }
    [TestMethod]
    public void TestMethod17()
    {
        var regexString0 = "[^a-zA-Z0-9_]";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsFullyMatch(""));
        Assert.IsFalse(regex0.IsFullyMatch("a"));
        Assert.IsFalse(regex0.IsFullyMatch("A"));
        Assert.IsFalse(regex0.IsFullyMatch("_"));
        Assert.IsFalse(regex0.IsFullyMatch("8"));
        Assert.IsFalse(regex0.IsFullyMatch("aaaa"));
    }
    [TestMethod]
    public void TestMethod18()
    {
        var regexString0 = @"\A\b\B\d\D\f\n\r\s\S\t\v\w\W\x12\023";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);

    }

    [TestMethod]
    public void TestMethod19()
    {
        var regs = new string[] {
            @"^\d+$", //�Ǹ������������� + 0�� 
            @"^[0-9]*[1-9][0-9]*$", //������ 
            @"^((-\d+)|(0+))$", //���������������� + 0�� 
            @"^-[0-9]*[1-9][0-9]*$", //������ 
            @"^-?\d+$", //���� 
            @"^\d+(\.\d+)?$", //�Ǹ����������������� + 0�� 
            @"^(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*))$", //��������
            @"^((-\d+(\.\d+)?)|(0+(\.0+)?))$", //�������������������� + 0�� 
            @"^(-(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*)))$", //�������� 
            @"^(-?\d+)(\.\d+)?$", //������ 
            @"^[A-Za-z]+$", //��26��Ӣ����ĸ��ɵ��ַ��� 
            @"^[A-Z]+$", //��26��Ӣ����ĸ�Ĵ�д��ɵ��ַ��� 
            @"^[a-z]+$", //��26��Ӣ����ĸ��Сд��ɵ��ַ��� 
            @"^[A-Za-z0-9]+$", //�����ֺ�26��Ӣ����ĸ��ɵ��ַ��� 
            @"^\w+$", //�����֡�26��Ӣ����ĸ�����»�����ɵ��ַ��� 
            @"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$", //email��ַ 
            @"^[a-zA-z]+://(\w+(-\w+)*)(\.(\w+(-\w+)*))*(\?\S*)?$", //url
            @"[ab]{4,6}",
            @"[ab]{4,6}c",
            @"(a|b)*ab",
            @"[A-Za-z0-9]",
            @"[A-Za-z0-9_]",
            @"[A-Za-z]",
            @"[ \t]",
            @"[(?<=\W)(?=\w)|(?<=\w)(?=\W)]",
            @"[\x00-\x1F\x7F]",
            @"[0-9]",
            @"[^0-9]",
            @"[\x21-\x7E]",
            @"[a-z]",
            @"[\x20-\x7E]",
            @"[ \t\r\n\v\f]",
            @"[^ \t\r\n\v\f]",
            @"[A-Z]",
            @"[A-Fa-f0-9]",
            @"in[du]",
            @"x[0-9A-Z]",
            @"[^A-M]in",
            @".gr",
            @"\(.*l",
            @"W*in",
            @"[xX][0-9a-z]",
            @"\(\(\(ab\)*c\)*d\)\(ef\)*\(gh\)\{2\}\(ij\)*\(kl\)*\(mn\)*\(op\)*\(qr\)*",
            @"((mailto\:|(news|(ht|f)tp(s?))\://){1}\S+)",
            @"^http\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$",
            @"^([1-zA-Z0-1@.\s]{1,255})$",
            @"[A-Z][0-9A-Z]{10}",
            @"[A-Z][A-Za-z0-9]{10}",
            @"[A-Za-z0-9]{11}",
            @"[A-Za-z]{11}",
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
            //@"^(?n:[a-z0-9])+$",
            //@"^(?x:[a-z0-9])+$",
            @"\\S+.*",
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

        var results = new List<string>();
        foreach (var reg in regs)
        {
            string text = "";
            try
            {
                var g = new RegExGenerator(reg);
                text = g.Generate();
                Assert.IsTrue(g.Regex.IsMatch(text));

            }
            catch (Exception)
            {
                Debug.WriteLine($"BADBADBAD:REG:{reg}:TEXT:{text}");
            }
        }
    }
    [TestMethod]
    public void TestMethod20()
    {
        var regexString0 = "[a-zA-Z]*";
        var regex0 = new Regex(regexString0);
        ExportAsDot(regex0);
        Assert.IsFalse(regex0.IsMatch("123456"));
        Assert.IsTrue(regex0.IsMatch("123abc456xyz"));
        var capture = regex0.Match("123abc456");

        Assert.AreEqual("abc", capture.Value);

        var captures = regex0.Matches("123abc456xyz888tmt");

        Assert.AreEqual(captures.Count, 3);
        Assert.AreEqual("abc", captures[0].Value);
        Assert.AreEqual("xyz", captures[1].Value);
        Assert.AreEqual("tmt", captures[2].Value);

        var parts = regex0.Split("123abc456xyz888tmt");

        Assert.AreEqual(parts.Length, 3);
        Assert.AreEqual("123", parts[0]);
        Assert.AreEqual("456", parts[1]);
        Assert.AreEqual("888", parts[2]);

        var ret = regex0.ReplaceFirst("123abc456xyz888tmt", "hahaha");
        Assert.AreEqual(ret, "123hahaha456xyz888tmt");
        ret = regex0.ReplaceAll("123abc456xyz888tmt", "hahaha");
        Assert.AreEqual(ret, "123hahaha456hahaha888hahaha");
    }
    [TestMethod]
    public void TestMethod21()
    {
        var reg = @"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$";
        var g = new RegExGenerator(reg);
        var t = g.Generate();
        var m = g.Regex.IsMatch(t);
        Debug.WriteLine(t);
    }
    [TestMethod]
    public void TestMethod22()
    {
        var r = new Regex(@"\.");
        var m = r.Match(".");
    }
    [TestMethod]
    public void TestMethod23()
    {
        string greedyPattern = @".+(\d+)\.";
        string lazyPattern = @".+?(\d+)\.";

        var regex1 = new Regex(greedyPattern);
        ExportAsDot(regex1);

        var regex2 = new Regex(lazyPattern);
        string input = "This sentence ends with the number 107325.";
        try
        {
            var r1 = regex1.Match(input);
            var r2 = regex2.Match(input);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
    [TestMethod]
    public void TestMethod24()
    {
        string input = "area bare arena mare";
        string pattern = @"\bare\w*\b";
        var regex1 = new Regex(pattern);

        var ms = regex1.Matches(input);


    }
    [TestMethod]
    public void TestMethod25()
    {
        string input = "a";
        string pattern = @"^a$";
        var regex1 = new Regex(pattern);

        var ms = regex1.Matches(input);

    }
    [TestMethod]
    public void TestMethod26()
    {
        string input = "hello world abc";
        string pattern = @"(\w+)\s";

        var regex1 = new Regex(pattern);
        ExportAsDot(regex1);

        var ms = regex1.Matches(input);

    }
    [TestMethod]
    public void TestMethod27()
    {
        //not captives
        var pattern = @"(?:abc)";
        var regex1 = new Regex(pattern);

        //lookaround
        pattern = @"(?=abc)";
        regex1 = new Regex(pattern);

        pattern = @"(?!abc)";
        regex1 = new Regex(pattern);

        pattern = @"(?<=abc)";
        regex1 = new Regex(pattern);

        pattern = @"(?<!abc)";
        regex1 = new Regex(pattern);


        //conditions with back reference
        pattern = @"(?(name)abc)";
        regex1 = new Regex(pattern);

        pattern = @"(?(name)abc|def)";
        regex1 = new Regex(pattern);

        pattern = @"(?(321)abc)";
        regex1 = new Regex(pattern);


        //conditions
        pattern = @"(?(?=xyz)abc)";
        regex1 = new Regex(pattern);

        pattern = @"(?(?!xyz)abc)";
        regex1 = new Regex(pattern);
        pattern = @"(?(?<=xyz)abc)";
        regex1 = new Regex(pattern);

        pattern = @"(?(?<!xyz)abc)";
        regex1 = new Regex(pattern);

    }
    [TestMethod]
    public void TestMethod28()
    {
        var good_ones = new string[]
        {
            "(ax+)+y",//NOT CBT
            "(x+b)+y",//NOT CBT
            "(abc|cat)*", //NOT CBT
            "foo|(x+bx+)+y",//NOT CBT
        };

        //<a\s*href=(.*?)[\s|>]: "<a href=" * 10000
        var bad_ones = new string[]
        {
            "<a\\s*href=(.*?)[\\s>]", //OK
            "(abc|adx|azz)*", //OK
            "foo|(x+x+)+y",//OK
            "^(a+)+$", //OK
            "^(a|a?)+$", //OK
            "(x+x+)+y", //OK
            "foo|(x+x+)+y",//OK
            "([a-zA-Z]+)*", //OK
            "(a|aa)+", //OK

            "(a+){2}y",//OK
            "(a+){10}y",//OK
            "(.*a){25}",//OK
            //"(a?){25}(a){25}",//OK,SLOW
            //"(.*){1,1000}[bc]",//OK,SLOW
        };
        foreach (var good_one in good_ones)
        {
            var p = RegExGraphVerifier.IsCatastrophicBacktrackingPossible(good_one);
            Assert.IsFalse(p);
        }
        foreach (var bad_one in bad_ones)
        {
            var p = RegExGraphVerifier.IsCatastrophicBacktrackingPossible(bad_one);
            Assert.IsTrue(p);
        }
    }
    public class ParseRecord
    {
        public readonly int Id;
        public readonly string Input;
        public readonly string Parse;
        public readonly int Size;
        public readonly bool Pumpable;
        public readonly double Time;
        public ParseRecord(int Id = 0, string Input = "", string Parse = "", int Size = 0, bool Pumpable = false, double Time = 0.0)
        {
            this.Id = Id;
            this.Input = Input;
            this.Parse = Parse;
            this.Size = Size;
            this.Pumpable = Pumpable;
            this.Time = Time;
        }
    }

    public List<ParseRecord> ParseFiles(string[] files)
    {
        var records = new List<ParseRecord>();
        foreach (var file in files)
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(Environment.CurrentDirectory, file));
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("= [") && line.EndsWith("] ="))
                {
                    if (!int.TryParse(line[3..^3], out var Id)) continue;

                    string input = "";
                    string parse = "";
                    int size = 0;
                    bool pumpable = false;
                    double time = 0.0;
                    if (++i >= lines.Length || !(line = lines[i]).StartsWith("INPUT:"))
                    {
                        --i;
                        records.Add(new(Id));
                        continue;
                    }
                    else
                    {
                        input = line[6..].Trim();
                        if (++i >= lines.Length || !(line = lines[i]).StartsWith("PARSE:"))
                        {
                            --i;
                            records.Add(new(Id, input));
                            continue;
                        }
                        else
                        {
                            parse = line[6..].Trim();
                            if (++i >= lines.Length || !(line = lines[i]).StartsWith("SIZE:"))
                            {
                                --i;
                                records.Add(new(Id, input, parse));
                                continue;
                            }
                            else
                            {
                                if (!int.TryParse(line[5..].Trim(), out size)) continue;
                                if (++i >= lines.Length || !(line = lines[i]).StartsWith("PUMPABLE:"))
                                {
                                    --i;
                                    records.Add(new(Id, input, parse, size));
                                    continue;
                                }
                                else
                                {
                                    pumpable = line[8..].Trim() == "YES";
                                    if (++i >= lines.Length || !(line = lines[i]).StartsWith("TIME:") && line.EndsWith(" (s)"))
                                    {
                                        --i;
                                        records.Add(new(Id, input, parse, size, pumpable));
                                        continue;
                                    }
                                    else
                                    {
                                        if (!double.TryParse(line[5..^5], out time))
                                        {
                                            --i;
                                            records.Add(new(Id, input, parse, size, pumpable));
                                            continue;
                                        }
                                        records.Add(new(Id, input, parse, size, pumpable, time));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return records;
    }
    [TestMethod]
    public void TestMethod29()
    {
        var ecd = Environment.CurrentDirectory;

        Environment.CurrentDirectory =
           System.IO.Path.Combine(Environment.CurrentDirectory, "..\\data\\validate");

        var files = new string[]
        {
            "rxxr-regexlib-vulns.txt",
            "rxxr-snort-vulns.txt",
        };
        var records = ParseFiles(files);
        int rc = records.Count;
        int count = 0;
        Debug.WriteLine($"Total:{records.Count}");

        using var output = new StreamWriter("Output.txt");
        foreach (var record in records.Where(r => r.Parse == "OK"))
        {
            var possible = RegExGraphVerifier.IsCatastrophicBacktrackingPossible(record.Input);
            if (!possible)
            {
                output.WriteLine($"Record: {record.Id}-({count}/{rc}):{record.Input}");
                output.WriteLine($"  Possible CBT:{possible}");
            }
            count++;
        }

        Environment.CurrentDirectory = ecd;
    }
}
