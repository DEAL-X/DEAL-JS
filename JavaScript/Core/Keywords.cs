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

        public virtual Keywords Get(params string[] keysHierarchy)
        {
            if (keysHierarchy.Length == 0) return this;
            if (TryGetValue(keysHierarchy[0], out var k)) return k.Get(keysHierarchy.Skip(1).ToArray());
            return null;
        }
        public virtual Keywords Get(params int[] localitiesHierarchy)
        {
            if (localitiesHierarchy.Length == 0) return this;
            var l = localitiesHierarchy[0];
            return this.FirstOrDefault((k) => k.Value.Locality == l).Value?.Get(localitiesHierarchy.Skip(1).ToArray());
        }
        public virtual Keywords Get(params TokenType[] typeHierarchy)
        {
            if (typeHierarchy.Length == 0) return this;
            var type = typeHierarchy[0];
            return this.FirstOrDefault((k) => (k.Value.Type & type) == type).Value?.Get(typeHierarchy.Skip(1).ToArray());
        }

        public virtual bool Set(Node node) => Set(node.Token.Value, node);
        public virtual bool Set(string key, Node node) => Set(key, node.Token.Value, node.Token.Type, node.Location);
        public virtual bool Set(string name, TokenType type, int location) => Set(name, name, type, location);
        public virtual bool Set(string key, string name, TokenType type, int location)
        {
            if (key == null) return false;
            this[key] = new Keywords(name, type, location);
            return true;
        }
        public virtual bool Set(TokenType type, int location, params KeyValuePair<string,string>[] keyNames)
        {
            if (keyNames.Length == 0) return false;
            this[keyNames[0].Key] = new Keywords(keyNames[0].Value, type, location);
            if (keyNames.Length == 1) return true;
            return this[keyNames[0].Key].Set(type, location, keyNames.Skip(1).ToArray());
        }
        public virtual bool Set(Keywords keyword)
        {
            if (keyword == null) return false;
            this[keyword.Name] = keyword;
            return true;
        }
        public virtual bool Remove(Keywords keyword)
        {
            if (keyword == null) return false;
            return Remove(keyword.Name);
        }
        public virtual int RemoveAll(TokenType type)
        {
            var keys = Keys;
            int num = 0;
            foreach (var key in keys)
                if (((this[key].Type & type) == type) && Remove(key))
                    num++;
            return num;
        }
        public virtual int RemoveAll(int fromLocality = 1)
        {
            var keys = Keys;
            int num = 0;
            foreach (var key in keys)
                if (this[key].Locality >= fromLocality && Remove(key))
                    num++;
            return num;
        }
    }
}
