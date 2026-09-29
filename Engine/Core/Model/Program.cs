namespace MiMFa.Engine.Model
{
    public class Program : Node
    {
        public Program(string source = null, params Node[] children) : base(new Token(TokenType.StringData, source), NodeType.Program, children: children)
        {
        }
    }
}