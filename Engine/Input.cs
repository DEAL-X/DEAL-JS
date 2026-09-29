using System;

namespace MiMFa.Engine
{
    public class Input
    {
        public string Content { get; set; }

        public string Source { get; set; }

        public Input(string content = "", string source = null)
        {
            Content = content;
            Source = source;
        }
    }
}
