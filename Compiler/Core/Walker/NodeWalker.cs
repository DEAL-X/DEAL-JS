// Converted from src/engine/DaRQ/Compiler/Assembler/NodeWalker.ts
using System;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;

namespace MiMFa.Compiler.Walker
{
    public class NodeWalker : WalkerBase<Node>
    {
        public NodeWalker(Node[] nodes = null, string source = null) : base(nodes?? new Node[0], source) { }

        public bool Is(params NodeType[] nodeTypes)
        {
            var current = Current;
            if (current != null) return current.Is(nodeTypes);
            return false;
        }

        public Node Peek(int offset = 0, params NodeType[] ofTypes)
        {
            Node p;
            int o = offset;
            while ((p = Peek(o)) != null)
            {
                if (ofTypes.Length == 0 || Array.Exists(ofTypes, v => (v & (p.Type)) != 0))
                    return p;
                o++;
            }
            return null;
        }

        public Node PeekProcedure(int offset = 0)
        {
            Node p;
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

        public NodeWalker Move(int count = 1, params NodeType[] ofTypes)
        {
            Node p;
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

        public NodeWalker MoveToProcedure(int count = 1)
        {
            return (NodeWalker)MoveTo(t => t.IsProcedure(), count);
        }

        public Node Walk(params NodeType[] ofTypes)
        {
            Node p;
            while (!IsEnded && (p = base.Walk()) != null)
            {
                if (ofTypes.Length == 0) return p;
                foreach (var v in ofTypes)
                    if ((v & p.Type) == v) return p;
            }
            return new Node();
        }

        public Node WalkProcedure()
        {
            return WalkTo(t => t.IsProcedure());
        }
    }
}
