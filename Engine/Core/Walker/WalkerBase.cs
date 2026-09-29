using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Core;

namespace MiMFa.Engine.Walker
{
    public abstract class WalkerBase<T> : IWalker<T> where T : class
    {
        public string Source { get; private set; }

        public T[] Content { get; private set; }
        public int Position { get; private set; }
        public int Length => Content.Length;

        public T Current => Peek(0);

        public bool IsRunning => Position >= 0 && Position < Content.Length;
        public bool IsEnded => Position >= Content.Length;

        protected WalkerBase(T[] Content, string source = null)
        {
            this.Content = Content ?? new T[0];
            this.Source = source;
        }

        public virtual T Peek(int offset = 0)
        {
            var index = Position + offset;
            if (index >= 0 && index < Content.Length) return Content[index];
            return null;
        }

        public T PeekThe(Func<T, bool> aggregator, int offset = 0)
        {
            T p = null;
            int c = 0;
            if (offset < 0)
            {
                while ((p = Peek(c)) != null)
                {
                    if (!aggregator(p)) offset--;
                    if (c-- <= offset) break;
                }
            }
            else
            {
                while ((p = Peek(c)) != null)
                {
                    if (!aggregator(p)) offset++;
                    if (c++ >= offset) break;
                }
            }
            return p;
        }

        public virtual T Walk()
        {
            if (IsEnded) return null;
            return Content[Position++];
        }

        public T WalkTo(Func<T, bool> aggregator)
        {
            T p = null;
            while (!aggregator(p = Walk()) && IsRunning) ;
            return p;
        }

        public virtual IWalker<T> Move(int count = 1)
        {
            Position = Math.Max(Math.Min(Position + count, Content.Length), 0);
            return this;
        }

        public IWalker<T> MoveTo(Func<T, bool> aggregator, int count = 1)
        {
            while (count > 0)
                if (!IsRunning || aggregator(Walk())) count--;
            while (count < 0)
                if (!IsRunning || --Position > 0) count++;
            return this;
        }

        public virtual IWalker<T> Reset(int index = 0)
        {
            this.Position = Math.Max(Math.Min(Content.Length, index), 0);
            return this;
        }

        public virtual IWalker<T> Add(params T[] items)
        {
            Content = Content.Concat(items).ToArray();
            return this;
        }
        public virtual IWalker<T> Remove(Position position, int count = 1)
        {
            return Remove(position.Index, count);
        }
        public virtual IWalker<T> Remove(int index = -1, int count = 1)
        {
            return Replace(index < 0 ? Position : index, count, new T[0]);
        }
        public virtual IWalker<T> Remove(T item)
        {
            for (int i = Position; i < Content.Length; i++)
                if (Content[i] == item)
                {
                    Content = Content.Take(i).Concat(Content.Skip(i + 1)).ToArray();
                    return this;
                }
            return this;
        }

        public virtual IWalker<T> Insert(int index, params T[] values)
        {
            Content = Content.Take(index).Concat(values).Concat(Content.Skip(index)).ToArray();
            return this;
        }

        public virtual IWalker<T> Replace(Position position, int count, params T[] replacement)
        {
            return Replace(position.Index, count, replacement);
        }
        public virtual IWalker<T> Replace(int index, int count, params T[] replacement)
        {
            Content = Content.Take(index).Concat(replacement).Concat(Content.Skip(index + count)).ToArray();
            return this;
        }
        public virtual IWalker<T> Replace(params T[] replacement)
        {
            Content = Content.Take(Position).Concat(replacement).Concat(Content.Skip(Position + replacement.Length)).ToArray();
            return this;
        }

        public IEnumerable<TOut> MapWhile<TOut>(Func<bool> predicate, Func<TOut> action)
        {
            while (predicate())
            {
                var m = action();
                if (m != null) yield return m;
            }
        }

        public IEnumerable<TOut> MapUntil<TOut>(Func<bool> predicate, Func<TOut> action)
        {
            while (!predicate())
            {
                var m = action();
                if (m != null) yield return m;
            }
        }

        public IEnumerable<T> PeekCount(int count = 1)
        {
            var length = Math.Max(0, Math.Min(Content.Length, Position + count));
            if (length > Position)
                for (int index = Position; index < length; index++) yield return Content[index];
            else if (length < Position)
                for (int index = length; index < Position; index++) yield return Content[index];
        }

        public IEnumerable<T> PeekWhile(Func<T, bool> predicate)
        {
            int offset = 0;
            T c;
            while ((c = Peek(offset++)) != null && predicate(c)) yield return c;
        }

        public IEnumerable<T> PeekUntil(Func<T, bool> predicate)
        {
            int offset = 0;
            T c;
            while ((c = Peek(offset++)) != null && !predicate(c)) yield return c;
        }

        public IEnumerable<T> WalkWhile(Func<T, bool> predicate)
        {
            while (!IsEnded)
            {
                var current = Current;
                if (current == null || !predicate(current)) break;
                yield return Walk();
            }
        }

        public IEnumerable<T> WalkUntil(Func<T, bool> predicate)
        {
            while (!IsEnded)
            {
                var current = Current;
                if (current == null || predicate(current)) break;
                yield return Walk();
            }
        }

        public IEnumerable<TOut> Cast<TOut>(Func<T, TOut> convertor)
        {
            T p = null;
            TOut o;
            while ((o = convertor(p = Walk())) != null) yield return o;
        }

    }
}
