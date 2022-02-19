using NRegEx;

var regexString0 = "(ab|c)*abb";
var regex0 = new Regex(regexString0);
Console.WriteLine(regex0);
Console.WriteLine(regex0.RegexText);
Console.WriteLine(regex0.NFA);
regex0.Reset();

var regexString1 = "a*";
var regex1 = new Regex(regexString1);
Console.WriteLine(regex1.RegexText);
Console.WriteLine(regex1.NFA);
regex1.Reset();

var regexString2 = "ab";
Regex regex2 = new Regex(regexString2);
Console.WriteLine(regex2.RegexText);
Console.WriteLine(regex2.NFA);
regex2.Reset();

var regexString3 = "a|b";
var regex3 = new Regex(regexString3);
Console.WriteLine(regex3.RegexText);
Console.WriteLine(regex3.NFA);
regex3.Reset();

var regexString4 = "(a|b)*";
var regex4 = new Regex(regexString4);
Console.WriteLine(regex4.RegexText);
Console.WriteLine(regex4.NFA);
regex4.Reset();

var regexString5 = "1(0|1)*101";
var regex5 = new Regex(regexString5);
Console.WriteLine(regex5.RegexText);
Console.WriteLine(regex5.NFA);
regex5.Reset();

var regexString6 = "0*10*10*10*";
var regex6 = new Regex(regexString6);
Console.WriteLine(regex6.RegexText);
Console.WriteLine(regex6.NFA);
regex6.Reset();

var regexString7 = "1(1010*|1(010)*1)*0";
var regex7 = new Regex(regexString7);
Console.WriteLine(regex7.RegexText);
Console.WriteLine(regex7.NFA);
regex6.Reset();

