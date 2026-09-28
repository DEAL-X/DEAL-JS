// Converted from src/engine/DaRQ/JavaScript-Compiler/Generator.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Generator;
using MiMFa.Compiler.Assembler;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.JavaScript
{
    public class Generator : MiMFa.Compiler.Generator.Generator
    {
        protected virtual IList<string> GenerateArray(params Node[] nodes)
        {
            var walker = new NodeWalker(nodes.ToArray());
            return Generate(walker).ToList();
        }
        protected virtual string GenerateArrayCode(params Node[] nodes) => GenerateArrayCode(nodes, "");
        protected virtual string GenerateArrayCode(IEnumerable<Node> nodes, string separator = null)
        {
            return string.Join(separator ?? "", GenerateArray(nodes.ToArray())).Trim();
        }
        protected virtual string GenerateCodeLine(Node node, NodeWalker walker)
        {
            if (node.Is(NodeType.BlockStructure) && node.Token.IsMatch("{")) return GenerateCode(node, walker) ?? "";
            var code = GenerateCode(node, walker) ?? "";
            return string.IsNullOrEmpty(code) ? "" : System.Text.RegularExpressions.Regex.IsMatch(code, "[;\\}]\\s*$") ? code : $"{code};";
        }

        protected override string GenerateProgramCode(Node node, NodeWalker walker)
        {
            if (Compiler != null) return
                    Transform(node, Compiler) as string +
                    Compiler.Options.MakeNewLine(Indention) +
                    Compiler.Options.MakeNewLine(Indention);
            return null;
        }
        protected override string GenerateStructureCode(Node node, NodeWalker walker)
        {
            string prefix = "", suffix = "";
            var next = walker.PeekProcedure();
            if (node.Is(NodeType.BlockStructure))
            {
                if (
                    next != null &&
                    (
                        next.IsMatch("{") ||
                        !next.IsMatch("=>")
                    ) &&
                    !next.Is(TokenType.Suffix, TokenType.Start, TokenType.Scope, TokenType.ConcatenatorSymbol, TokenType.DelimiterSymbol, TokenType.TerminatorSymbol)
                ) suffix = " ";
                if (node.Token.Is(TokenType.ObjectData))
                {
                    Indention++;
                    var inner = GenerateArrayCode(node.Children, " ").Trim();
                    Indention--;
                    return $"{prefix}{node.Token.Value}{inner}}}{suffix}";
                }
                //else if (node.IsMatch("{") == true) suffix = Compiler.Options.MakeNewLine(Indention);
                if (node.Token.Is(TokenType.ArrayData))
                {
                    Indention++;
                    var parameters = GenerateArrayCode(node.Children, " ").Trim();
                    Indention--;
                    return $"{prefix}{node.Token.Value}{parameters}]{suffix}";
                }
                if (node.Token.Is(TokenType.Scope))
                    if (node.Token.IsMatch("{"))
                    {
                        Indention++;
                        var body = GenerateArrayCode(node.Children, Compiler.Options.MakeNewLine(Indention) ?? "").Trim();
                        Indention--;
                        return $"{prefix}{node.Token.Value}{Compiler.Options.MakeNewLine(++Indention)}{body}{Compiler.Options.MakeNewLine(--Indention)}}}{suffix}";
                    }
                    else if (node.Token.IsMatch("["))
                    {
                        Indention++;
                        var parameters = GenerateArrayCode(node.Children, " ").Trim();
                        Indention--;
                        return $"{prefix}{node.Token.Value}{parameters}]{suffix}";
                    }
                    else if (node.Token.IsMatch("("))
                    {
                        Indention++;
                        var parameters = GenerateArrayCode(node.Children, " ").Trim();
                        Indention--;
                        return $"{prefix}{node.Token.Value}{parameters}){suffix}";
                    }
                Indention++;
                var lines = GenerateArrayCode(node.Children, Compiler.Options.MakeNewLine(Indention) ?? "").Trim();
                Indention--;
                return $"{prefix}{node.Token.Value}{Compiler.Options.MakeNewLine(Indention + 1)}{lines}{suffix}";
            }

            if (node.Is(NodeType.Region))
                prefix = suffix = Compiler.Options.MakeNewLine(Indention);
            //else if (node.Is(NodeType.Line))
            //    prefix = Compiler.Options.MakeNewLine(Indention);

            if (node.Is(NodeType.DefineStructure))
            {
                if (node.Token.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children, " ")}{suffix}";
                if (string.IsNullOrEmpty(node.Token.Value))
                    return $"{prefix}{GenerateArrayCode(node.Children, " ")}{suffix}";
                else return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children, " ")}{suffix}";
            }

            if (node.Is(NodeType.CallStructure))
            {
                if (node.Token.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                if (node.Token.Is(TokenType.NamespaceKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                if (node.ForceFirst.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol, TokenType.End, TokenType.Suffix, TokenType.ConcatenatorSymbol) || node.ForceFirst.IsMatch("[", "("))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}".Trim();
                else return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children)}{suffix}".Trim();
            }

            //if (node.Is(NodeType.SelectorStructure))
            //{
            //    if (node.Is(NodeType.NormalSelectorStructure))
            //        return node.Count < 3?
            //            $"{node.Token.Value} {GenerateArrayCode(node.Children)}" :
            //            $"{node.Token.Value}{GenerateArrayCode(node.ForceFirst.Children)} {GenerateCodeLine(node.ForceChild(1), walker)}";
            //}

            //if (node.Is(NodeType.IteratorStructure))
            //{
            //    if (node.Is(NodeType.ConditionIteratorStructure))
            //    {
            //        if (node.Is(NodeType.PostConditionIteratorStructure))
            //            return $"do {GenerateCodeLine(node.ForceFirst, walker)}" + Compiler.Options.MakeNewLine(Indention) + $"while({GenerateArrayCode(node.ForceLast.Children)})";
            //        return $"while({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler.Options.MakeNewLine(Indention);
            //    }
            //    return $"for({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler.Options.MakeNewLine(Indention);
            //}

            return GenerateCode(node.Update((NodeType)(node.Type - NodeType.Structure) | NodeType.Unknown), walker);
        }
        protected override string GenerateRegionCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0)
                return $"{Compiler.Options.MakeNewLine(Indention)}{node.Token.Value}{Compiler.Options.MakeNewLine(Indention)}";
            else return $"{Compiler.Options.MakeNewLine(Indention)}{(node.Token.Value + " " + GenerateArrayCode(node.Children)).Trim()}{Compiler.Options.MakeNewLine(Indention)}";
        }
        protected override string GenerateLineCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0)
                return $"{node.Token.Value}{Compiler.Options.MakeNewLine(Indention)}";
            else return $"{(node.Token.Value + " " + GenerateArrayCode(node.Children)).Trim()}{Compiler.Options.MakeNewLine(Indention)}";
        }
        protected override string GenerateChunkCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0)
                return node.Token.Value;
            else return $"{node.Token.Value}{GenerateArrayCode(node.Children)}";
        }
        protected override string GenerateIndependCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0)
                return $"{node.Token.Value}{Compiler.Options.MakeNewLine(Indention)}";
            else return $"{node.Token.Value}{Compiler.Options.MakeNewLine(Indention)}{GenerateArrayCode(node.Children).TrimEnd()}";
        }
        protected override string GenerateDependCode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.ConcatenatorSymbol)) return node.Token.Value + GenerateArrayCode(node.Children);
            if (node.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol)) return node.Token.Value + " " + GenerateArrayCode(node.Children);
            else if (node.Count <= 0) return " " + node.Token.Value;
            else return " " + node.Token.Value + " " + GenerateArrayCode(node.Children);
        }
        protected override string GenerateAppendCode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.Suffix, TokenType.TerminatorSymbol, TokenType.ConcatenatorSymbol))
                return node.Token.Value + GenerateArrayCode(node.Children);
            else if (node.Count <= 0) return node.Token.Value;
            else return " " + node.Token.Value + GenerateArrayCode(node.Children);
        }
        protected override string GeneratePrependCode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.Prefix, TokenType.TerminatorSymbol, TokenType.ConcatenatorSymbol)) return node.Token.Value + GenerateArrayCode(node.Children);
            else if (node.Count <= 0) return node.Token.Value;
            else return node.Token.Value + " " + GenerateArrayCode(node.Children);
        }
        protected override string GenerateUnknownCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0) return node.Token.Value;
            else return $"{node.Token.Value} {GenerateArrayCode(node.Children)}".Trim();
        }
    }
}
