using MiMFa.Engine.Model;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Engine.JavaScript
{
    public class Keywords : Dictionary<string, Keywords>
    {
        public string Name { get; set; }
        public int Locality { get; set; }
        public TokenType Type { get; set; }

        public Keywords(string name, TokenType type, int locality)
        {
            Name = name;
            Type = type;
            Locality = locality;
        }

        public virtual Keywords Find(params string[] keysHierarchy)
        {
            if (keysHierarchy.Length == 0) return this;
            if (TryGetValue(keysHierarchy[0], out var k)) return k.Find(keysHierarchy.Skip(1).ToArray());
            return null;
        }
        public virtual Keywords Find(params int[] localitiesHierarchy)
        {
            if (localitiesHierarchy.Length == 0) return this;
            var l = localitiesHierarchy[0];
            return this.FirstOrDefault((k) => k.Value.Locality == l).Value?.Find(localitiesHierarchy.Skip(1).ToArray());
        }
        public virtual Keywords Find(params TokenType[] typeHierarchy)
        {
            if (typeHierarchy.Length == 0) return this;
            var type = typeHierarchy[0];
            return this.FirstOrDefault((k) => (k.Value.Type & type) > 0).Value?.Find(typeHierarchy.Skip(1).ToArray());
        }

        public virtual bool TryAdd(Node node) => TryAdd(node.Token.Value, node);
        public virtual bool TryAdd(string key, Node node) => TryAdd(key, node.Token.Value, node.Token.Type, node.Location);
        public virtual bool TryAdd(string name, TokenType type, int location) => TryAdd(name, name, type, location);
        public virtual bool TryAdd(string key, string name, TokenType type, int location)
        {
            if (key == null) return false;
            this[key] = new Keywords(name, type, location);
            return true;
        }
        public virtual bool TryAdd(Keywords keyword)
        {
            if (keyword == null) return false;
            this[keyword.Name] = keyword;
            return true;
        }
        public virtual int Remove(TokenType type)
        {
            var keys = Keys;
            int num = 0;
            foreach (var key in keys)
                if ((this[key].Type & type) > 0 && Remove(key))
                    num++;
            return num;
        }
        public virtual int Remove(int fromLocality = 1)
        {
            var keys = Keys;
            int num = 0;
            foreach (var key in keys)
                if (this[key].Locality >= fromLocality && Remove(key))
                    num++;
            return num;
        }
        public virtual bool Remove(Keywords keyword)
        {
            if (keyword == null) return false;
            return Remove(keyword.Name);
        }
    }
}
