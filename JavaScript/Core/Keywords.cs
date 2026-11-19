using MiMFa.Engine.Model;
using System;
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

        public virtual Keywords FirstLeaf() => Count > 0 ? this.First().Value?? this : this;
        public virtual Keywords LastLeaf() => Count > 0? Values.Last()?.LastLeaf()?? this : this;
        public virtual int Depth() => Count > 0 ? Values.Select(v=>v?.Depth()??0).Max() + 1: 0;
        public virtual string Namespace(int index = 0, string separator = ".") => index < Count? string.Join(separator, Name, Values.ElementAt(index)?.Namespace(index, separator)): Name;

        public virtual bool Has(params string[] keysHierarchy) => Get(keysHierarchy) != null;
        public virtual bool Has(string name, TokenType type) => Get(name, type) != null;
     
        public virtual Keywords Get(params string[] keysHierarchy)
        {
            if (keysHierarchy.Length == 0) return new Keywords(Name, Type, Locality);
            if (TryGetValue(keysHierarchy[0], out var k)) {
                var n = k.Get(keysHierarchy.Skip(1).ToArray());
                if (n != null) return new Keywords(Name, Type, Locality) { { k.Name.ToLower(), n} };
            }
            return null;
        }
        public virtual Keywords Get(params int[] localitiesHierarchy)
        {
            if (localitiesHierarchy.Length == 0) return new Keywords(Name, Type, Locality);
            var l = localitiesHierarchy[0];
            var f = this.FirstOrDefault((k) => k.Value.Locality == l).Value?.Get(localitiesHierarchy.Skip(1).ToArray());
            if(f != null) return new Keywords(Name, Type, Locality) { { f.Name.ToLower(), f } };
            return null;
        }
        public virtual Keywords Get(params TokenType[] typeHierarchy)
        {
            if (typeHierarchy.Length == 0) return new Keywords(Name, Type, Locality);
            var type = typeHierarchy[0];
            var f = this.FirstOrDefault((k) => (k.Value.Type & type) == type).Value?.Get(typeHierarchy.Skip(1).ToArray());  
            if(f != null) return new Keywords(Name, Type, Locality) { { f.Name.ToLower(), f } };
            return null;
        }
        public virtual Keywords Get(string name, TokenType type)
        {
            if (ContainsKey(name) && ((this[name].Type & type) == type))
                return this[name];
            return null;
        }

        public virtual bool Set(Node node) => Set(node.Token.Value, node);
        public virtual bool Set(string key, Node node) => Set(key, node.Token.Value, node.Token.Type, node.Location);
        public virtual bool Set(string name, TokenType type, int location) => Set(name, name, type, location);
        public virtual bool Set(string key, string name, TokenType type, int location)
        {
            if (key == null) return false;
            if (this.ContainsKey(key))
            {
                this[key].Name = name;
                this[key].Type = type;
                this[key].Locality = location;
            }
            else this[key] = new Keywords(name, type, location);
            return true;
        }
        public virtual bool Set(TokenType type, int location, params KeyValuePair<string,string>[] keyNames)
        {
            if (keyNames.Length == 0) return false;
            if (this.ContainsKey(keyNames[0].Key))
            {
                this[keyNames[0].Key].Name = keyNames[0].Value;
                this[keyNames[0].Key].Type = type;
                this[keyNames[0].Key].Locality = location;
            }
            else this[keyNames[0].Key] = new Keywords(keyNames[0].Value, type, location);
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

        public override string ToString() => Namespace();
    }
}
