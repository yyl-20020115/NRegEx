namespace NRegEx;

public class RuneClass
{
    public static List<RuneClass> FromTable(int[][] table, bool inverted, List<RuneClass>? list = null, int Positon = -1)
    {
        list ??= new();
        foreach (var triple in table)
        {
            int lo = triple[0], hi = triple[1], stride = triple[2];
            if (stride == 1)
            {
                list.Add(new(lo, hi, inverted) { Position = Positon });
                continue;
            }
            else if (stride > 1)
            {
                var range = new List<int>();
                for (int c = lo; c <= hi; c += stride)
                {
                    range.Add(c);
                }
                if (range.Count > 0)
                {
                    list.Add(new(inverted, range.ToArray()) { Position = Positon });
                }
            }
        }
        return list;
    }

    public readonly int[] Runes;
    public readonly bool Inverted;
    public int Position { get; init; } = -1;
    public RuneClass(bool Inverted, params int[] Runes)
    {
        this.Runes = Runes;
        this.Inverted = Inverted;
    }
    public RuneClass(int lo, int hi, bool Inverted = false)
    {
        this.Runes = Enumerable.Range(lo, hi - lo + 1).ToArray();
        this.Inverted = Inverted;
    }
}
