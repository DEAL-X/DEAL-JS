using System;

namespace MiMFa.Engine.Model
{
    [Flags]
    public enum TokenType
    {
        Unknown = 0,
        None = 1 << 0,

        Statement = 1 << 1,

        Start = 1 << 2,
        Prefix = 1 << 3,
        Middle = 1 << 4,
        Suffix = 1 << 5,
        End = 1 << 6,

        Scope = 1 << 7,

        Symbol = 1 << 8,
            ConcatenatorSymbol = (1 << 15) | Symbol,
            DelimiterSymbol = (1 << 16) | Symbol,
            TerminatorSymbol = (1 << 17) | Symbol,

        Data = 1 << 9,
            UndefinedData = (1 << 18) | Data,
            NullData = (1 << 19) | Data,
            BinaryData = (1 << 20) | Data,
            NumberData = (1 << 21) | Data,
            BooleanData = (1 << 22) | Data,
            StringData = (1 << 23) | Data,
            TemplateStringData = (1 << 24) | StringData,
            ObjectData = (1 << 25) | Data,
            ArrayData = (1 << 26) | Data,
            PatternData = (1 << 27) | Data,

        Keyword = 1 << 10,
            NamespaceKeyword = (1 << 28) | Keyword,
            FunctionKeyword = (1 << 29) | Keyword,
            IdentifierKeyword = (1 << 30) | Keyword,

        Comment = 1 << 11
    }
}