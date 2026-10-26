using System;
using System.Linq;
using System.Text.RegularExpressions;
using MiMFa.Engine;
using MiMFa.Engine.Tokenizer;
using MiMFa.Engine.Core;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;
using System.Collections.Generic;
using MiMFa.Engine.Put;

namespace MiMFa.Engine.DEAL.JS
{
    public class Tokenizer : MiMFa.Engine.JavaScript.Tokenizer
    {
        public new Engine Engine { get; set; }
        public Dictionary<string, TokenType> Acceptors { get; set; } = new Dictionary<string, TokenType>();
        public Dictionary<string, TokenType> DataAcceptors { get; set; } = new Dictionary<string, TokenType>();
        public Dictionary<string, string> CaseAcceptors { get; set; } = new Dictionary<string, string>();

        protected TokenType? SwitchToken { get; set; }
        protected bool SwitchNamespace { get; set; } = false;
        protected bool SwitchParent { get; set; } = false;

        public Token LastToken = null;

        public override bool Initialize(MiMFa.Engine.Engine engine)
        {
            if (engine != null) base.Initialize(Engine = engine as Engine);
            return true;
        }

        public override object Transform(object input, MiMFa.Engine.Engine compiler)
        {
            return base.Transform(Regex.Replace((string)input, @"(\n[\t ]*)[\*\+\-]([\t ])(?!\s)", "$1 $2"), compiler);
        }
        public override IEnumerable<Token> Tokenize(CodeWalker walker, MiMFa.Engine.Engine engine = null)
        {
            foreach (var item in base.Tokenize(walker, engine))
                yield return LastToken = item;
        }

        protected override Token TokenizeCode(CodeWalker walker)
        {
            if (SwitchParent)
            {
                SwitchParent = false;
                return base.TokenizeCode(walker);
            }
            walker.MoveToProcedure();
            if (this.SwitchToken != null)
                if (Regex.IsMatch(walker.Current, @"[{}\[\]()""'`;]", RegexOptions.CultureInvariant))
                    this.SwitchToken = null;
                else if (walker.Current != ",")
                {
                    var type = this.SwitchToken.Value;
                    this.SwitchToken = null;
                    return GetAcceptedToken(type, string.Join("", walker.WalkWhile(c => !Regex.IsMatch(walker.Current, @"[,;\s]", RegexOptions.CultureInvariant))), walker.Position);
                }
            var switchNameSpace = this.SwitchNamespace;
            if (this.SwitchNamespace) this.SwitchNamespace = false;
            if (walker.Current == "." && !walker.PeekProcedure().StartsWith("...")) this.SwitchNamespace = true;
            else if (!switchNameSpace && walker.Current != null)
            {
                var location = walker.Position;
                var position = walker.Index;

                if(walker.Current == "#")
                {
                    var w = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^\\w0-9_$#\\s:|]", RegexOptions.CultureInvariant)).ToArray());
                    w = Regex.Match(w, @"^#\w+(:\s*[\w|]*)*", RegexOptions.CultureInvariant).Value;
                    if (!string.IsNullOrWhiteSpace(w))
                    {
                        var ws = w.Split(new string[] { "#", ":" }, StringSplitOptions.None);
                        var cmd = ws[1];
                        TokenType? acceptor = ws.Length > 2 ? TokenTypeFromName(ws[2].Trim()) : null;
                        TokenType? dataAcceptor = ws.Length > 3 ? TokenTypeFromName(ws[3].Trim()) : null;
                        SetAcceptedType(cmd, acceptor ?? TokenType.Unknown, dataAcceptor);
                        walker.Remove(location.Index, w.Length);
                        return new Token(TokenType.Keyword, cmd, walker.Position);
                    }
                }

                location = walker.Position;
                position = walker.Index;
                var word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^\\w0-9_$\\*]", RegexOptions.CultureInvariant)).ToArray());
                var lword = word.ToLower();

                switch (lword)
                {
                    case "use":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        SwitchToken = TokenType.StringData;
                        var src = TokenizeCode(walker)?.Value;
                        if (!string.IsNullOrEmpty(src))
                        {
                            src = src.TrimEnd(';', ',');
                            int libs = UsePath(src);
                            if (walker.PeekProcedure() == ";" ) walker.WalkProcedure();
                            //walker.Replace(location.Index, p == ";"? walker.MoveToProcedure().Location.Index - location.Index : walker.Location.Index - location.Index, res.ToCharArray().Select(c => c.ToString()).ToArray());
                            return GetAcceptedToken(TokenType.Comment, libs<=0? $"// Could not find the {Newtonsoft.Json.JsonConvert.ToString(src)} library" : (libs > 1 ? $"// Used {libs} libraries from {Newtonsoft.Json.JsonConvert.ToString(src)}" : $"// Used {Newtonsoft.Json.JsonConvert.ToString(src)} library"), location);
                        }
                        else walker.Reset(position);
                        break;
                    case "reserve":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        var key = walker.Current == "#"? TokenizeCode(walker)?.Value : string.Join("", walker.WalkUntil(c=> Regex.IsMatch(c,"[\\s=;,]", RegexOptions.CultureInvariant)));
                        if (!string.IsNullOrEmpty(key))
                        {
                            walker.MoveToProcedure();
                            this.SwitchToken = TokenType.IdentifierKeyword;
                            var m = Regex.IsMatch(walker.Current, "[,;\n]", RegexOptions.CultureInvariant) ? walker.Walk() : TokenizeCode(walker)?.Value;
                            this.SwitchToken = null;
                            if (!string.IsNullOrEmpty(m))
                            {
                                switch (m.ToLower())
                                {
                                    case "\n":
                                    case ",":
                                    case ";":
                                        if (Engine != null) try { Engine.Reserves[key] = ""; } catch { }
                                        return GetAcceptedToken(TokenType.Comment, $"// Reserved {Newtonsoft.Json.JsonConvert.ToString(key)} as a noise", location);
                                    case "be":
                                    case "as":
                                    case "=":
                                        walker.MoveToProcedure();
                                        var val = TokenizeCode(walker)?.Value ?? "";
                                        try { Engine?.Reserves.Add(key, val); } catch { }
                                        walker.MoveToProcedure();
                                        if (walker.Walk() != ";") walker.Move(-1);
                                        return GetAcceptedToken(TokenType.Comment, $"/* Reserved {Newtonsoft.Json.JsonConvert.ToString(key)} as the {Newtonsoft.Json.JsonConvert.ToString(val.Replace("*/", "*\\/"))} */", location);
                                    default:
                                        walker.Reset(position);
                                        break;
                                }
                            }
                            else walker.Reset(position);
                        }
                        else walker.Reset(position);
                        break;

                    case "new":
                    case "instanceof":
                    case "typeof":
                        SwitchParent = true;
                        break;

                    default:
                        if (Regex.IsMatch(word, @"[\w0-9_$]", RegexOptions.CultureInvariant) && word.StartsWith("*") && word.EndsWith("*"))
                        {
                            var cmd = word.Trim('*');
                            SetAcceptedType(cmd);
                            walker.Replace(location, word.Length, cmd.ToCharArray().Select(v => v.ToString()).ToArray());
                            //return new Token(TokenType.Keyword, cmd, walker.Position);
                        }
                        break;
                }

                var current = walker.Current;
                if (current != null)
                {
                    location = walker.Position;
                    word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^\\w0-9_$]", RegexOptions.CultureInvariant)).ToArray());
                    lword = word.ToLower();

                    switch (lword)
                    {
                        case "command":
                        case "begin":
                        case "doing":
                        case "end":
                        case "consider":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Statement, word, location);
                        
                        case "as":
                            //ReservesScrubber(word);
                            //SwitchToken = TokenType.StringData;
                            //walker.Replace(location, word.Length, ",");
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Middle, word, location);
                        
                        case "where":
                        case "order":
                        case "distinct":
                        case "limit":
                        case "keys":
                        case "values":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Suffix, word, location);

                        case "will":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Start, word, location);
                        case "then":
                        case "otherwise":
                        case "anyway":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Suffix, word, location);

                        case "argument":
                        case "arguments":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.IdentifierKeyword, word, location);

                        case "resolve":
                        case "reject":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.FunctionKeyword, word, location);

                        case "is":
                        case "be":
                        case "not":
                        case "equal":
                        case "equals":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Middle | TokenType.Symbol, word, location);
                    }

                    if (ReservesApplier(walker, word, location))
                        return TokenizeCode(walker);

                    if (current == "\\")
                        return TokenizeXPath(walker, location);

                    if (current == "#")
                    {
                        word = string.Join("", walker.WalkWhile(w => w == "#"));
                        if(word.Length <= 6) return GetAcceptedToken(TokenType.Statement, word, location);
                    }

                    //if (current == ":")
                    //    return GetAcceptedToken(TokenType.End | TokenType.Symbol, current, location);

                    var p = string.Concat(walker.PeekUntil(ch => string.IsNullOrWhiteSpace(ch)).ToArray());
                    if (Regex.IsMatch(p, "^\\w+:\\/([\\/][^\\s\\[\\]\\{\\}\\/\\\\]+)+$", RegexOptions.CultureInvariant))
                        return TokenizePath(walker, location);
                }
            }

            var wrd = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^\\w0-9_$]", RegexOptions.CultureInvariant)).ToArray());
            var tk = GetAcceptedToken(TokenType.Unknown, wrd, walker.Position);
            if (tk.Value != wrd)
                walker.Replace(walker.Position, wrd.Length, tk.Value.ToCharArray().Select(v => v.ToString()).ToArray());
            if (tk.Type != TokenType.Unknown) {
                var ntk = base.TokenizeCode(walker);
                return ntk.Update(ntk.Type | tk.Type);
            }
            else return base.TokenizeCode(walker);
        }

        protected virtual Token TokenizeXPath(CodeWalker walker, Position location)
        {
            walker.Walk();
            var value = string.Concat(walker.WalkUntil(ch => ch == "\\" && walker.Peek(-1) != Engine?.Options?.Escape).ToArray());
            walker.Walk();
            return GetAcceptedToken(TokenType.StringData, value, location);
        }
        protected virtual Token TokenizePath(CodeWalker walker, Position location)
        {
            var value = string.Concat(walker.WalkUntil(ch => string.IsNullOrWhiteSpace(ch)).ToArray());
            return GetAcceptedToken(TokenType.StringData, value, location);
        }

        protected virtual int UsePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return 0;
            if (Regex.IsMatch(path, "^\\w+:\\/\\/.+$", RegexOptions.CultureInvariant)) return UseUrl(path);
            var compiler = this.Engine as MiMFa.Engine.Engine;
            if (compiler == null) return 0;
            var baseDirectory = Regex.IsMatch(path, "^\\.[\\/\\\\]", RegexOptions.CultureInvariant) ?
                compiler?.ResourceProvider?.DirectoryName(compiler.Input?.Source ?? compiler.SourceDirectory+"index") :
                compiler?.ResourceProvider?.DirectoryName(compiler.SourceDirectory + "index");
            var val = compiler?.ResourceProvider?.Resolve(path, baseDirectory);
            if (!string.IsNullOrEmpty(val) && val.EndsWith("\\")) return UseFolder(val);
            int num;
            return (num = UseFile(val)) > 0 ? num : UseFolder(val);
        }
        protected virtual int UseUrl(string url)
        {
            return 0;
        }
        protected virtual int UseFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || folder.Split(System.IO.Path.DirectorySeparatorChar).Last().StartsWith("~")) return 0;
            folder = Regex.Replace(folder, "[\\/\\\\]$", "");
            int results = 0;
            try
            {
                var rp = Engine.ResourceProvider;
                if (rp != null && rp.Exists(folder))
                {
                    var res = UseFile(folder + "\\index");
                    if (res > 0) return res;
                    else
                    {
                        foreach (var adrs in rp.GetFolderContents(folder))
                        {
                            if (adrs.IsFile) results += UseFile(adrs.Path);
                            else if (adrs.IsFolder) results += UseFolder(adrs.Path);
                        }
                    }
                }
            }
            catch { }
            return results;
        }
        protected virtual int UseFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Split(System.IO.Path.DirectorySeparatorChar).Last().StartsWith("~")) return 0;
            int num = 0;
            if (!Regex.IsMatch(file, "\\.(deal|djs|js)$", RegexOptions.IgnoreCase))
                return (num = UseFile(file + ".deal")) > 0 ? num : (num = UseFile(file + ".djs")) > 0 ? num : UseFile(file + ".js");
            file = System.IO.Path.GetFullPath(file);
            try
            {
                var compiler = this.Engine as MiMFa.Engine.Engine;
                var rp = compiler?.ResourceProvider;
                if ((compiler as Engine).HasDependency(file)) return 1;
                if (rp != null && rp.Exists(file))
                {
                    int c = (compiler as Engine).Dependencies.Count;
                    (compiler as Engine).UseDependency(
                        new Input(rp.GetFileContents(file, System.Text.Encoding.UTF8), file)
                    );
                    return (compiler as Engine).Dependencies.Count - c;
                }
            }
            catch { }
            return num;
        }


        protected virtual TokenType? TokenTypeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (name.Contains("|"))
            {
                TokenType? combined = null;
                foreach (var item in name.Split('|'))
                    if (combined.HasValue) combined = ((TokenTypeFromName(item) ?? combined) | combined);
                    else combined = TokenTypeFromName(item);
                return combined;
            }
            switch (name.ToLower())
            {
                case "u":
                    return TokenType.Unknown;

                case "n":
                    return TokenType.None;

                case "s":
                    return TokenType.Statement;

                case "y":
                    return TokenType.Symbol;

                case "p":
                    return TokenType.Scope;

                case "o":
                    return TokenType.Start;

                case "c":
                    return TokenType.Suffix;

                case "d":
                    return TokenType.Data;
                case "path":
                case "xpath":
                    return TokenType.StringData;
                case "template":
                    return TokenType.TemplateStringData;
                case "short":
                case "int":
                case "long":
                case "float":
                case "single":
                case "double":
                    return TokenType.NumberData;
                case "regex":
                    return TokenType.PatternData;
                case "bool":
                    return TokenType.BooleanData;

                case "k":
                    return TokenType.Keyword;

                case "m":
                    return TokenType.Middle;

                case "z":
                    return TokenType.Comment;

                default:
                    TokenType result = TokenType.None;
                    if (
                        Enum.TryParse(name, true, out result) ||
                        Enum.TryParse(name + "Symbol", true, out result) ||
                        Enum.TryParse(name + "Scope", true, out result) ||
                        Enum.TryParse(name + "Data", true, out result) ||
                        Enum.TryParse(name + "Keyword", true, out result)
                    )
                        return result;
                    else return null;
            }
        }

        protected virtual void SetAcceptedType(string name, TokenType? acceptor = null, TokenType? dataAcceptor = null)
        {
            string lname = name.ToLower();
            if (acceptor.HasValue) Acceptors[lname] = acceptor.Value;
            if (dataAcceptor.HasValue) DataAcceptors[lname] = dataAcceptor.Value;
            CaseAcceptors[lname] = name;
        }
        protected virtual Token GetAcceptedToken(TokenType type, string name, Position location = null)
        {
            string lname = name.ToLower();
            bool iscommand = LastToken == null || (
                !LastToken.Is(".") &&
                (type & TokenType.StringData) == 0 &&
                (type & TokenType.Comment) == 0
            );
            if (iscommand && DataAcceptors.ContainsKey(lname))
                SwitchToken = DataAcceptors[lname];
            return new Token(
                iscommand && Acceptors.ContainsKey(lname) ? Acceptors[lname] | type : type,
                iscommand && CaseAcceptors.ContainsKey(lname) ? CaseAcceptors[lname] : name,
                location
            );
        }


        protected virtual bool ReservesApplier(CodeWalker walker, string word, Position location)
        {
            try
            {
                var t = GetAcceptedToken(TokenType.Unknown, word);
                bool setAcc = t.Type != TokenType.Unknown;
                foreach (var reserve in Engine.Reserves)
                {
                    if (word == reserve.Key)
                    {
                        if (setAcc) SetAcceptedType(reserve.Value, t.Type);
                        if (word == reserve.Value) SwitchNamespace = true;
                        else walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray().Select(v=>v.ToString()).ToArray());
                        return true;
                    }
                }
                var lw = word.ToLower();
                foreach (var reserve in Engine.Reserves)
                {
                    if (lw == reserve.Key.ToLower())
                    {
                        if (setAcc) SetAcceptedType(reserve.Value, t.Type);
                        if (word == reserve.Value) SwitchNamespace = true;
                        else walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray().Select(v => v.ToString()).ToArray());
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
        protected virtual int ReservesScrubber(string word)
        {
            word = word.ToLower();
            int d = 0;
            try
            {
                var reserves = Engine?.Reserves;
                if (reserves != null)
                {
                    var keys = reserves.Keys.ToArray();
                    for (int index = 0; index < keys.Length; index++)
                        if (keys[index].ToLower() == word)
                            if (reserves.Remove(keys[index])) d++;
                }
            }
            catch { }
            return d;
        }
    }
}
