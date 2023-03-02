namespace NRegEx;

public enum ReplacementType : uint
{
    PlainText = 0,
    GroupIndex = 1,//$number
    GroupName = 2,//$name
    Dollar = 3, //$$
    WholeMatch = 4,//$&
    PreMatch = 5, //$`
    PostMatch= 6, //$'
    LastGroup = 7, //$+
    Input = 8, //$_

}
