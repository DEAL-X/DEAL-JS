// Converted from src/engine/DaRQ/Compiler/Core/NodeType.ts
using System;

namespace MiMFa.Compiler.Model
{
    [Flags]
    public enum NodeType
    {
        Unknown = 0,
        None = 1 << 0,

        Program = 1 << 1,
        Region = 1 << 2,
        Line = 1 << 3,
        Chunk = 1 << 4,

        /// <summary>
        /// ` CURRENT ` // Like `,` in: `myins, 12` `
        /// </summary>
        Independ = 1 << 6,
        /// <summary>
        /// `CURRENT next` // Like `new` in: `new Instance`
        /// </summary>
        Prepend = 1 << 8,
        /// <summary>
        /// `previous CURRENT` // Like `}` in: `; }`
        /// </summary>
        Append = 1 << 9,
        /// <summary>
        /// `previous CURRENT next` // Like `instanceof` in: `myins instanceof Instance`
        /// </summary>
        Depend = 1 << 7 | Prepend | Append,

        Structure = 1 << 10,
            BlockStructure = 1 << 11 | Structure,
            DefineStructure = 1 << 12 | Structure,
            CallStructure = 1 << 13 | Structure,
            ConditionStructure = (1 << 14) | Structure,
            IterationStructure = (1 << 15) | Structure,
    }
}
