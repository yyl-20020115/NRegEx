public static class RegexHelpers
{
    public static int FixDirection(int direction) => direction >= 0 ? 1 : -1;
    public static int Abs(int value) => value >= 0 ? value : -value;
}