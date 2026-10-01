using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine;
using MiMFa.Engine.Core;
using MiMFa.Engine.Generator;
using MiMFa.Engine.Assembler;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.JavaScript
{
    public class Generator : MiMFa.Engine.Generator.Generator
    {
        protected virtual string NewLine(int? indention = null) => Engine.Options.MakeNewLine(indention??Indention);
        protected virtual IList<string> GenerateArray(params Node[] nodes) => Generate(new NodeWalker(nodes.ToArray())).ToList();
        protected virtual string GenerateArrayCode(params Node[] nodes) => GenerateArrayCode(nodes, "");
        protected virtual string GenerateArrayCode(IEnumerable<Node> nodes, string separator = null) => string.Join(separator ?? "", GenerateArray(nodes.ToArray()));

        protected override string GenerateProgramCode(Node node, NodeWalker walker)
        {
            if (Engine != null) return
                    Transform(node, Engine) as string +
                    NewLine() +
                    NewLine();
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
                        !(
                            next.IsMatch("=>") ||
                            next.Is(NodeType.Append) ||
                            next.Is(NodeType.Depend) ||
                            next.Is(TokenType.Suffix, TokenType.Start, TokenType.Scope, TokenType.ConcatenatorSymbol, TokenType.DelimiterSymbol, TokenType.TerminatorSymbol)
                        )
                    )
                ) suffix = " ";
                if (node.Token.Is(TokenType.ObjectData))
                {
                    Indention++;
                    var inner = GenerateArrayCode(node.Children, NewLine()).Trim();
                    Indention--;
                    if(inner.Contains("\n")) return $"{prefix}{{{NewLine(Indention+1)}{inner}{NewLine()}}}{suffix}";
                    else if(string.IsNullOrWhiteSpace(inner)) return $"{prefix}{{}}{suffix}";
                    else return $"{prefix}{{ {inner} }}{suffix}";
                }
                if (node.Token.Is(TokenType.ArrayData))
                {
                    var inner = GenerateArrayCode(node.Children, " ").Trim();
                    if(string.IsNullOrWhiteSpace(inner)) return $"{prefix}[]{suffix}";
                    return $"{prefix}[{inner}]{suffix}";
                }
                if (node.Token.Is(TokenType.Scope))
                    if (node.Token.IsMatch("{"))
                    {
                        Indention++;
                        var inner = GenerateArrayCode(node.Children, NewLine()).Trim();
                        Indention--;
                        if(string.IsNullOrWhiteSpace(inner)) return $"{prefix}{{}}{suffix}";
                        return $"{prefix}{{{NewLine(Indention+1)}{inner}{NewLine()}}}{suffix}";
                    }
                    else if (node.Token.IsMatch("["))
                    {
                        var inner = GenerateArrayCode(node.Children, " ").Trim();
                        if(string.IsNullOrWhiteSpace(inner)) return $"{prefix}[]{suffix}";
                        return $"{prefix}[{inner}]{suffix}";
                    }
                    else if (node.Token.IsMatch("("))
                    {
                        var inner = GenerateArrayCode(node.Children, " ").Trim();
                        if(string.IsNullOrWhiteSpace(inner)) return $"{prefix}(){suffix}";
                        return $"{prefix}({inner}){suffix}";
                    }
                Indention++;
                var lines = GenerateArrayCode(node.Children, NewLine()).Trim();
                Indention--;
                return $"{prefix}{node.Token.Value}{NewLine(Indention + 1)}{lines}{suffix}";
            }


            if (node.Is(NodeType.DefineStructure))
            {
                if (node.Is(NodeType.Append))
                    prefix = " ";
                if (node.Is(NodeType.Prepend) && node.Count <= 0)
                    suffix = " ";
                if (node.Is(NodeType.Region))
                    prefix = NewLine();
                if (node.Is(NodeType.Line))
                    suffix = NewLine();

                if (node.Count <= 0)
                    return $"{prefix}{node.Token.Value}{suffix}";
                if (node.First.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children, " ").Trim()}{suffix}";
                if (node.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                if (node.IsMatch("function", "function*"))
                    return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children)}{suffix}";
                if (string.IsNullOrEmpty(node.Token.Value))
                    return $"{prefix}{GenerateArrayCode(node.Children)}{suffix}";
                else return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children, " ").Trim()}{suffix}";
            }

            if (node.Is(NodeType.CallStructure))
            {
                if (node.Is(NodeType.Region))
                    prefix = NewLine();
                if (node.Is(NodeType.Line))
                    suffix = NewLine();

                if (node.Count <= 0)
                    return $"{prefix}{node.Token.Value}{suffix}";
                if (node.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                if (node.Is(TokenType.NamespaceKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                else return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children).Trim()}{suffix}";
            }

            if (node.Is(NodeType.ConditionStructure, NodeType.IterationStructure) && node.Count > 1 && node.First.IsMatch("("))
                return $"{node.Token.Value} {GenerateArrayCode(node.Children)}";

            return GenerateCode(node.Update((node.Type & ~NodeType.Structure) | NodeType.Unknown), walker);
        }
        protected override string GenerateRegionCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0)
                return $"{(walker.PeekProcedure(-2)?.Is(NodeType.Line) != true?NewLine() : "")}{node.Token.Value}{(node.Is(NodeType.Line) ? NewLine() : "")}";
            else if (string.IsNullOrWhiteSpace(node.Token.Value))
                return $"{(walker.PeekProcedure(-2)?.Is(NodeType.Line) != true ? NewLine() : "")}{GenerateArrayCode(node.Children)}{(node.Is(NodeType.Line) ? NewLine() : "")}";
            else return $"{(walker.PeekProcedure(-2)?.Is(NodeType.Line) != true ? NewLine() : "")}{node.Token.Value} {GenerateArrayCode(node.Children)}{(node.Is(NodeType.Line) ? NewLine() : "")}";
        }
        protected override string GenerateLineCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0)
                return $"{node.Token.Value}{NewLine()}";
            else if (string.IsNullOrWhiteSpace(node.Token.Value))
                 return $"{GenerateArrayCode(node.Children)}{NewLine()}";
            else return $"{node.Token.Value} {GenerateArrayCode(node.Children)}{NewLine()}";
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
                return $"{node.Token.Value}{NewLine()}";
            else return $"{node.Token.Value}{NewLine()}{GenerateArrayCode(node.Children)}";
        }
        protected override string GenerateDependCode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.ConcatenatorSymbol))
                return node.Token.Value + GenerateArrayCode(node.Children).Trim();
            if (node.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol))
                if (string.IsNullOrWhiteSpace(node.Token.Value)) 
                    return " " + GenerateArrayCode(node.Children).Trim();
                else return node.Token.Value + " " + GenerateArrayCode(node.Children).Trim();
            else if (node.Count <= 0)
                if (string.IsNullOrWhiteSpace(node.Token.Value)) return "";
                else return " " + node.Token.Value;
            else if (string.IsNullOrWhiteSpace(node.Token.Value)) 
                return " " + GenerateArrayCode(node.Children).Trim();
            else return " " + node.Token.Value + " " + GenerateArrayCode(node.Children).Trim();
        }
        protected override string GenerateAppendCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0) return node.Token.Value;
            else if (node.Is(TokenType.TerminatorSymbol))
                return node.Token.Value + " " + GenerateArrayCode(node.Children);
            else if (node.Is(TokenType.DelimiterSymbol))
                return node.Token.Value + " " + GenerateArrayCode(node.Children);
            else if (node.Is(TokenType.Suffix, TokenType.ConcatenatorSymbol))
                return node.Token.Value + GenerateArrayCode(node.Children);
            else return " " + node.Token.Value + GenerateArrayCode(node.Children);
        }
        protected override string GeneratePrependCode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.Prefix, TokenType.ConcatenatorSymbol))
                return node.Token.Value + GenerateArrayCode(node.Children);
            else if (node.Count <= 0)
                if (string.IsNullOrWhiteSpace(node.Token.Value)) return "";
                else return node.Token.Value;
            else if (string.IsNullOrWhiteSpace(node.Token.Value))
                return GenerateArrayCode(node.Children);
            else return node.Token.Value + " " + GenerateArrayCode(node.Children).Trim();
        }
        protected override string GenerateUnknownCode(Node node, NodeWalker walker)
        {
            if (node.Count <= 0) return node.Token.Value;
            else if (string.IsNullOrWhiteSpace(node.Token.Value)) return GenerateArrayCode(node.Children);
            else return $"{node.Token.Value} {GenerateArrayCode(node.Children).Trim()}";
        }
    }
}
