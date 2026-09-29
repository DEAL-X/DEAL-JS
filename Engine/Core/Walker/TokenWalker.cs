using System.Collections.Generic;
using MiMFa.Engine.Model;

namespace MiMFa.Engine.Walker
{
    public class TokenWalker : Walker<Token>
    {
        public TokenWalker(IEnumerable<Token> tokens, string source = null) : base(tokens, source)
        {
        }

        public bool Is(params TokenType[] tokenTypes)
        {
            return Current != null && Current.Is(tokenTypes);
        }

        public bool IsMatch(params string[] values)
        {
            return Current != null && Current.IsMatch(values);
        }

        public Token Peek(int offset = 0, params TokenType[] ofTypes)
        {
            Token p;
            int o = offset;
            while ((p = base.Peek(o++)) != null)
            {
                if (ofTypes.Length == 0) return p;
                foreach (var v in ofTypes)
                    if ((v & p.Type) == v) return p;
            }
            return null;
        }

        public Token PeekProcedure(int offset = 0)
        {
            Token p;
            if (offset < 0)
            {
                int o = 0;
                while ((p = base.Peek(--o)) != null)
                {
                    if (p.IsProcedure())
                    {
                        if (offset >= o) return p;
                    }
                    else offset--;
                }
            }
            else
            {
                int o = 0;
                while ((p = base.Peek(o++)) != null)
                {
                    if (p.IsProcedure())
                    {
                        if (offset < o) return p;
                    }
                    else offset++;
                }
            }
            return null;
        }

        public TokenWalker Move(int count = 1, params TokenType[] ofTypes)
        {
            Token p;
            int number = 0;
            if (ofTypes.Length > 0)
            {
                if (count > 0)
                {
                    while (number < count && (p = base.Peek(number++)) != null)
                    {
                        bool ok = false;
                        foreach (var v in ofTypes) if ((v & p.Type) == v) { ok = true; break; }
                        if (!ok) count++;
                    }
                }
                else
                {
                    while (number > count && (p = base.Peek(number--)) != null)
                    {
                        bool ok = false;
                        foreach (var v in ofTypes) if ((v & p.Type) == v) { ok = true; break; }
                        if (!ok) count--;
                    }
                }
            }
            base.Move(count);
            return this;
        }

        public TokenWalker MoveToProcedure(int count = 1)
        {
            return (TokenWalker)MoveTo(t => t.IsProcedure(), count);
        }

        public Token Walk(params TokenType[] ofTypes)
        {
            Token p;
            while (!IsEnded && (p = base.Walk()) != null)
            {
                if (ofTypes.Length == 0) return p;
                foreach (var v in ofTypes)
                    if ((v & p.Type) == v) return p;
            }
            return new Token(TokenType.None);
        }

        public Token WalkToProcedure()
        {
            return WalkTo(t => t.IsProcedure());
        }
    }
}
