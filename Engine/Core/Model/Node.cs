using System;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Engine.Model
{
    public class Node
    {
        public Token Token { get; set; }
        public NodeType Type { get; set; }
        public Node Parent { get; protected set; }
        public string Value => ToString();

        protected List<Node> children = new List<Node>();
        public IList<Node> Children
        {
            get => children;
            set
            {
                children.Clear();
                foreach (var v in value) Add(v);
            }
        }

        public AccessType AccessType { get; set; }
        public int Location { get; set; } = -1;

        public int Count => children.Count;
        public Node ForceFirst
        {
            get => First??new Node();
            set
            {
                if (children.Count > 0) children[0] = value;
                else Add(value);
            }
        }
        public Node ForceLast
        {
            get => Last ?? new Node();
            set
            {
                if (children.Count > 0) children[children.Count - 1] = value;
                else Add(value);
            }
        }
        public Node First => children.Count > 0 ? children.FirstOrDefault(v => !v.IsEmpty()) : null;
        public Node Last => children.Count > 0 ? children.LastOrDefault(v=>!v.IsEmpty()) : null;

        public Node FirstLeaf => First != null ? First.FirstLeaf : this;
        public Node LastLeaf => Last != null ? Last.LastLeaf : this;

        public Node(params Node[] children) : this(null, null, null, null, children) { }
        public Node(Token token = null, NodeType? type = null, int? location = null, params Node[] children) : this(token, type, null, location, children) { }
        public Node(Token token = null, NodeType? type = null, params Node[] children) : this(token, type, null, null, children) { }
        public Node(Token token = null, NodeType? type = null, AccessType? accessType = null, int? location = null, IEnumerable<Node> children = null, Node parent = null)
        {
            Children = children == null? new List<Node>() : children.ToList() ?? new List<Node>();
            Token = token ?? new Token();
            Type = type ?? (token != null || Children.Count > 0 ? NodeType.Unknown : NodeType.None);
            Parent = parent;
            Location = location ?? -1;
            AccessType = accessType ?? AccessType.Unknown;
        }

        public Node Update(NodeType? type = null, Token token = null)
            => Update(token, type);
        public Node Update(Token token = null, NodeType? type = null, AccessType? accessType = null, int? location = null, IEnumerable<Node> children = null, Node parent = null)
        {
            Token = token ?? Token;
            Type = type ?? Type;
            Parent = parent ?? Parent;
            Children = (children ?? Children).ToList();
            Location = location ?? Location;
            AccessType = accessType ?? AccessType;
            return this;
        }

        public Node Clone(Token token = null, NodeType? type = null, AccessType? accessType = null, int? location = null, IEnumerable<Node> children = null, Node parent = null)
        {
            return new Node(token ?? Token, type?? Type, accessType??AccessType, location??Location, (children?? Children).Select(c => c.Clone()).ToList(), parent ?? Parent);
        }

        public bool Is(params NodeType[] nodeTypes)
        {
            foreach (var nt in nodeTypes)
                if (((int)Type & (int)nt) == (int)nt) return true;
            return false;
        }
        public bool Is(params TokenType[] tokenTypes)
        {
            foreach (var nt in tokenTypes)
                if (((int)Token.Type & (int)nt) == (int)nt) return true;
            return false;
        }
        public bool IsMatch(params string[] values)
        {
            return Token.IsMatch(values);
        }
        public bool Has(Func<Node, bool> condition)
        {
            return condition(this) || Children.Any(n=>n.Has(condition));
        }

        public bool IsEmpty() => Is(NodeType.None, NodeType.Unknown) && Token.Is(TokenType.None, TokenType.Unknown) && Count <= 0 && Token.IsMatch("");
        public bool IsProcedure() => !Is(NodeType.None);

        public Node Add(Node node)
        {
            if (node != null)
            {
                node.Parent = this;
                children.Add(node);
            }
            return this;
        }

        public bool Remove(Node node)
        {
            if (node == null) return true;
            var index = children.IndexOf(node);
            if (index < 0) return false;
            node.Parent = null;
            children.RemoveAt(index);
            return true;
        }

        public Node Insert(int index, Node node)
        {
            if (node != null)
            {
                node.Parent = this;
                children = children.Take(index).Concat(new[] { node }).Concat(children.Skip(index)).ToList();
            }
            return this;
        }

        public Node Trim(Func<Node, bool> selector)
        {
            return TrimStart(selector).TrimEnd(selector);
        }
        public Node TrimStart(Func<Node, bool> selector)
        {
            while (children.Count > 0)
            {
                if (selector(children[0]))
                {
                    children[0].Parent = null;
                    children.RemoveAt(0);
                }
                else
                {
                    children[0].TrimStart(selector);
                    if(Count == 1 && Token.IsEmpty())
                        return children[0];
                    return this;
                }
            }
            if (Count == 1 && Token.IsEmpty())
                return children[0];
            return this;
        }
        public Node TrimEnd(Func<Node, bool> selector)
        {
            while (children.Count > 0)
            {
                var l = children.Count - 1;
                if (selector(children[l]))
                {
                    children[l].Parent = null;
                    children.RemoveAt(l);
                }
                else
                {
                    children[l].TrimEnd(selector);
                    if (Count == 1 && Token.IsEmpty())
                        return children[0];
                    return this;
                }
            }
            if (Count == 1 && Token.IsEmpty())
                return children[0];
            return this;
        }

        public Node Ancestor(Func<Node, bool> aggregator) => Parent == null? null: (aggregator(Parent) ? Parent : Parent.Ancestor(aggregator));
        public Node Seek(Func<Node, bool> aggregator)
        {
            if (aggregator(this)) return this;
            foreach (var child in Children)
                if (aggregator(child)) return child;
                else
                {
                    var c = child.Child(aggregator);
                    if (c != null) return c;
                }
            return null;
        }
        public Node ForceChild(int index) => Child(index)??new Node();
        public Node Child(int index) => children.Count > index ? children[index] : null;
        public Node Child(Func<Node, bool> aggregator)
        {
            foreach (var child in Children)
                if (aggregator(child)) return child;
                else
                {
                    var c = child.Child(aggregator);
                    if (c != null) return c;
                }
            return null;
        }

        public IEnumerable<Node> Flat(Func<Node, bool> aggregator = null)
        {
            if (aggregator == null || aggregator(this)) yield return Clone(children: new Node[0]);
            foreach (var child in children)
                foreach (var cc in child.Flat(aggregator))
                    yield return cc;
        }

        public Node AddRange(params Node[] nodes)
        {
            foreach (var node in nodes) Add(node);
            return this;
        }

        public Node Clear()
        {
            children.Clear();
            return this;
        }

        public Node Revise(Func<Node, Node> visitor)
        {
            var n = visitor(this);
            Update(n.Token, n.Type, n.AccessType, n.Location, n.Children, n.Parent);
            var c = Count;
            for (int i = 0; i < c; i++)
                children[i].Revise(visitor);
            return this;
        }

        public override string ToString()
        {
            return string.Join(" ", new[] { Token.Value }.Concat(Children.Select(n => n.ToString())));
        }
    }
}
