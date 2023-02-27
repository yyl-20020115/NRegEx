namespace NRegEx;

public class ResizableBitSet : BitSet
{
    public ResizableBitSet(IEnumerable<int> e) : base(e) { }

    public ResizableBitSet(int count, bool defValue = false) : base(count, defValue) { }

    public override int Count
    {
        get => base.count;
        protected set
        {
            if (value != base.count)
            {
                var buffer = CreateBuffer(value);
                Array.Copy(this.buffer, buffer, Math.Min(value, base.count));
                this.buffer = buffer;
                base.count = value;
            }
        }
    }
}
