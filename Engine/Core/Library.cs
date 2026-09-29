using System;
using System.Collections.Generic;
using System.Text;

namespace MiMFa.Engine.Core
{
    public class LibraryEventArgs : EventArgs
    {
        public string Path { get; }
        public string Content { get; }

        public LibraryEventArgs(string path, string content = null)
        {
            Path = path;
            Content = content;
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
            return Content;
        }
    }

    public delegate void LibraryEventHandler(Engine compiler, LibraryEventArgs e);
}
