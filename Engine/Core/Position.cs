namespace MiMFa.Engine.Core
{
    public class Position
    {
        public int Index { get; }
        public int Line { get; }
        public int Column { get; }

        public Position(int index = 0, int line = 1, int column = 1)
        {
            Index = index;
            Line = line;
            Column = column;
        }

        public Position Move(int length, string text = "")
        {
            var line = Line;
            var column = Column;
            foreach (var ch in text)
            {
                if (ch == '\n')
                {
                    line++;
                    column = 1;
                }
                else column++;
            }
            return new Position(Index + length, line, column);
        }

        public bool Equals(Position location)
        {
            return location != null && Index == location.Index && Line == location.Line && Column == location.Column;
        }

        public override string ToString() => $"{Line}:{Column}";
    }
}
