namespace MiMFa.Engine.Core
{
    public class Range
    {
        public Position Start { get; }
        public Position End { get; }

        public Range(Position start, Position end)
        {
            Start = start;
            End = end;
        }

        public int Length => End.Index - Start.Index;

        public bool Contains(Position location) => location.Index >= Start.Index && location.Index <= End.Index;

        public bool Overlaps(Range range) => Start.Index <= range.End.Index && End.Index >= range.Start.Index;

        public override string ToString() => $"{Start}-{End}";
    }
}
