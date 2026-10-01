using System;
using System.Collections.Generic;
using System.Text;

namespace MiMFa.Engine.Core
{
    public class DependencyEventArgs : EventArgs
    {
        public string Source { get; }
        public object Value { get; }

        public DependencyEventArgs(string path, object value = null)
        {
            Source = path;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return (Value??string.Empty).ToString();
        }
    }

    public delegate void DependencyEventHandler(Engine compiler, DependencyEventArgs e);
}
