using MiMFa.Compiler.Core;
using System;

namespace MiMFa.Compiler.Model
{
    public class Token
    {
        public string Value { get; set; }
        public TokenType Type { get; set; }
        public Position Position { get; set; }

        public Token(string value = null, TokenType? type = null, Position location = null)
        {
            Value = value ?? string.Empty;
            Type = type ?? (string.IsNullOrEmpty(value) ? TokenType.None : TokenType.Unknown);
            Position = location ?? new Position();
        }
        public Token(TokenType? type, string value = null, Position position = null) : this(value, type, position) { }

        public Token Update(TokenType? Type = null, string Value = null, Position position = null)
        {
            this.Value = Value ?? this.Value;
            this.Type = Type ?? this.Type;
            this.Position = position ?? this.Position;
            return this;
        }

        public Token Clone(TokenType? Type = null, string Value = null, Position position = null)
        {
            return new Token(Type ?? this.Type, Value ?? this.Value, position ?? this.Position);
        }

        public bool Is(params TokenType[] tokenTypes)
        {
            foreach (var tokenType in tokenTypes)
                if ((int)(this.Type & tokenType) == (int)tokenType)
                    return true;
            return false;
        }

        public bool IsEmpty() => string.IsNullOrEmpty(Value);
        public bool IsProcedure() => !string.IsNullOrWhiteSpace(Value) && !Is(TokenType.None, TokenType.Comment);
        
        public bool IsMatch(params string[] values)
        {
            foreach (var v in values)
                if (string.Equals(this.Value, v, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public double Sameness(string value)
        {
            if (this.Value == value) return 1.0;
            if (string.Equals(this.Value, value, StringComparison.OrdinalIgnoreCase)) return 0.5;
            return 0.0;
        }
    }
}
