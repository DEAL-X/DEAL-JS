using System.Linq;
using System.Text.RegularExpressions;
using MiMFa.Engine.Core;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.JavaScript
{
    public class Tokenizer : MiMFa.Engine.Tokenizer.Tokenizer
    {
        public TokenType? SpaceSwitch = null;
        protected override Token TokenizeCode(CodeWalker walker)
        {
            if(SpaceSwitch != null)
            {
                var sw = SpaceSwitch;
                SpaceSwitch = null;
                return new Token(sw, walker.WalkToProcedure());
            }
            walker.MoveToProcedure();
            var location = walker.Location;
            var current = walker.Current;
            if (current == null) return new Token();
            var next = walker.Peek(1);
            var word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());

            switch (word)
            {
                case "true":
                case "false":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.BooleanData, word, location);
                case "null":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.NullData, word, location);
                case "undefined":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.UndefinedData, word, location);

                case "if":
                case "else":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.Statement, word, location);
                case "switch":
                case "case":
                case "default":
                case "for":
                case "while":
                case "do":
                case "break":
                case "continue":
                case "try":
                case "catch":
                case "finally":
                case "throw":
                case "return":
                case "yield":
                case "import":
                case "export":
                case "void":
                case "const":
                case "let":
                case "var":
                case "with":
                case "debugger":
                case "delete":
                case "await":
                case "async":
                case "new":

                case "implements":
                case "extends":
                case "interface":
                case "class":
                case "enum":
                case "package":
                case "get":
                case "set":
                case "function":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.Statement, word, location);

                case "of":
                case "in":
                case "typeof":
                case "instanceof":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.Middle, word, location);

                case "super":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.FunctionKeyword, word, location);

                case "this":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.IdentifierKeyword, word, location);

                case "private":
                case "internal":
                case "protected":
                case "public":
                case "static":
                    walker.Move(word.Length);
                    walker.MoveToProcedure();
                    return new Token(TokenType.Statement, word, location);
            }
            if (current == "/")
            {
                if (next == "*") return TokenizeCommentBlock(walker, location);
                else if (next == "/") return TokenizeComment(walker, location);
                else if (!string.IsNullOrWhiteSpace(next)) return TokenizeRegExPath(walker, location);
            }

            if (Regex.IsMatch(current.ToString(), "[A-Z_$#]", RegexOptions.IgnoreCase))
                return TokenizeKeyword(walker, location);

            if (Regex.IsMatch(current.ToString(), @"[0-9]") || (Regex.IsMatch(current.ToString(), @"[\-\+]") && Regex.IsMatch(next.ToString(), @"[0-9\.]")))
                return TokenizeNumber(walker, location);

            if (current == "\"" || current == "'" || current == "`")
                return TokenizeString(walker, location);

            if (Regex.IsMatch(current.ToString(), "[^\\w\\d\\s_$]", RegexOptions.IgnoreCase))
                return TokenizeOperator(walker, location);

            return new Token(TokenType.Unknown, walker.Walk().ToString(), location);
        }

        protected virtual Token TokenizeKeyword(CodeWalker walker, Position location)
        {
            var chars = (new string[] { walker.Walk() }).Concat(walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), "[A-Za-z0-9_$]"))).ToArray();
            var value = string.Concat(chars);
            var type = TokenType.Keyword;
            walker.MoveToProcedure();
            var sign = string.Concat(walker.PeekCount(2).ToArray());
            if (sign == "++" || sign == "--")
            {
                value += sign;
                type = TokenType.IdentifierKeyword;
                walker.Move(2);
            }
            return new Token(type, value, location);
        }

        protected virtual Token TokenizeNumber(CodeWalker walker, Position location)
        {
            var value = string.Concat(walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), @"[\-\+_0-9.ebxo]", RegexOptions.IgnoreCase)).ToArray());
            value += string.Concat(walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), "[0-9.a-f]", RegexOptions.IgnoreCase)).ToArray());
            return new Token(TokenType.NumberData, value, location);
        }

        protected virtual Token TokenizeRegExPath(CodeWalker walker, Position location)
        {
            walker.Walk(); // consume '/'
            var value = string.Concat(walker.WalkUntil(ch => ch == "/" && walker.Peek(-1) != (this.Engine?.Options?.Escape ?? "\\")[0].ToString()).ToArray());
            walker.Walk(); // consume closing '/'
            value = "/" + value + "/" + string.Concat(walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), "[gimsuy]", RegexOptions.IgnoreCase)).ToArray());
            return new Token(TokenType.PatternData, value, location);
        }

        protected virtual Token TokenizeString(CodeWalker walker, Position location)
        {
            var quote = walker.Walk();
            var value = "";
            var escaped = false;
            while (!walker.IsEnded)
            {
                var ch = walker.Walk();
                if (!escaped && ch == quote) break;
                if (ch == (this.Engine?.Options?.Escape ?? "\\")[0].ToString() && !escaped)
                {
                    escaped = true;
                    continue;
                }
                if (ch != null) value += ch;
                escaped = false;
            }
            return new Token(quote == "`" && value.Contains("${") ? TokenType.TemplateStringData : TokenType.StringData, value, location);
        }

        protected virtual Token TokenizeOperator(CodeWalker walker, Position location)
        {
            var sign = string.Concat(walker.WalkUntil(ch => Regex.IsMatch(ch.ToString(), "[\\w\\d\\s_$]", RegexOptions.IgnoreCase)).ToArray());
            while (true)
            {
                switch (sign)
                {
                    case "*": case "/": case "**": case "%": case "+": case "-": case "~": case "^": case "=": case "<": case ">": case "<<": case ">>": case ">>>":
                    case "==": case "!=": case "===": case "!==": case "<=": case ">=": case "&&": case "||": case "&": case "|": case "??":
                    case "+=": case "-=": case "*=": case "/=": case "**=": case "^=": case "%=":
                    case "&&=": case "&=": case "||=": case "|=": case "??=":
                    case "=>":
                        return new Token(TokenType.Middle | TokenType.Symbol, sign, location);
                    case "!": case "...":
                        return new Token(TokenType.Prefix | TokenType.Symbol, sign, location);
                    case "++": case "--":
                        return new Token(TokenType.Prefix | TokenType.Suffix | TokenType.Symbol, sign, location);
                    case ".": case "?.": case "!.":
                        return new Token(TokenType.ConcatenatorSymbol, sign, location);
                    case ",":
                        return new Token(TokenType.DelimiterSymbol, sign, location);
                    case ";":
                        return new Token(TokenType.TerminatorSymbol, sign, location);
                    case "{": case "(":
                        return new Token(TokenType.Start | TokenType.Scope, sign, location);
                    case "[":
                        return new Token(TokenType.Start | TokenType.Scope, sign, location);
                    case "}": case ")":
                    case "]":
                        return new Token(TokenType.End | TokenType.Scope, sign, location);
                    case ":":
                    default:
                        if (sign.Length <= 1)
                            return new Token(TokenType.Symbol, sign, location);
                        break;
                }
                if (sign.Length > 1)
                {
                    walker.Move(-1);
                    sign = sign.Substring(0, sign.Length - 1);
                    continue;
                }
            }
        }

        protected virtual Token TokenizeComment(CodeWalker walker, Position location)
        {
            walker.Move(2);
            var value = string.Concat(walker.WalkUntil(ch => ch == "\r" || ch == "\n").ToArray());
            return new Token(TokenType.Comment, $"//{value}", location);
        }

        protected virtual Token TokenizeCommentBlock(CodeWalker walker, Position location)
        {
            walker.Move(2);
            var value = string.Concat(walker.WalkUntil(ch => ch == "*" && walker.Peek(1) == "/").ToArray());
            return new Token(TokenType.Comment, $"/*{value}" + walker.Walk() + walker.Walk(), location);
        }
    }
}
