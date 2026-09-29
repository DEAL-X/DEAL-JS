using System;

namespace MiMFa.Engine.Model
{
    [Flags]
    public enum AccessType
    {
        Unknown = 0,
        Private = 1 << 0,
        Protected = (1 << 1) | Private,
        Internal = (1 << 2) | Protected,
        Public = (1 << 3) | Internal,
        Global = (1 << 4) | Public
    }
}
