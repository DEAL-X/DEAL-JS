using System;
using System.Linq;
using System.Text.RegularExpressions;
using MiMFa.Compiler;
using MiMFa.Compiler.Tokenizer;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;
using System.Collections.Generic;

namespace MiMFa.Compiler.DaRQ
{
    public class Tokenizer : MiMFa.Compiler.JavaScript.Tokenizer
    {
        public new Compiler Compiler { get; set; }
        public Dictionary<string, TokenType> Acceptors { get; set; } = new Dictionary<string, TokenType>();
        public Dictionary<string, TokenType> DataAcceptors { get; set; } = new Dictionary<string, TokenType>();
        public Dictionary<string, string> CaseAcceptors { get; set; } = new Dictionary<string, string>();

        protected TokenType? SwitchToken { get; set; }
        protected bool SwitchNamespace { get; set; } = false;

        public Token LastToken = null;

        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler as Compiler);
            return true;
        }

        public override IEnumerable<Token> Tokenize(CodeWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            foreach (var item in base.Tokenize(walker, compiler))
                yield return LastToken = item;
        }

        protected override Token TokenizeCode(CodeWalker walker)
        {
            if (SpaceSwitch != null)
            {
                var type = SpaceSwitch.Value;
                SpaceSwitch = null;
                return GetAcceptedToken(type, walker.WalkToProcedure());
            }

            walker.MoveToProcedure();
            if (this.SwitchToken != null)
                if (!Regex.IsMatch(walker.Current, @"[{}\[\]()""'`,;]"))
                {
                    var type = this.SwitchToken.Value;
                    this.SwitchToken = null;
                    return GetAcceptedToken(type, string.Join("", walker.WalkWhile(c => !Regex.IsMatch(walker.Current, @"[,;\s]"))), walker.Location);
                }
                else this.SwitchToken = null;
            var switchNameSpace = this.SwitchNamespace;
            if (this.SwitchNamespace) this.SwitchNamespace = false;
            if (walker.Current == "." && !walker.PeekProcedure().StartsWith("...")) this.SwitchNamespace = true;
            else if (!switchNameSpace && walker.Current != null)
            {
                var location = walker.Location;
                var position = walker.Position;

                if(walker.Current == "@")
                {
                    var w = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$@|]", RegexOptions.IgnoreCase)).ToArray());
                    var ws = w.Split(new string[] { "@"}, StringSplitOptions.None);
                    var cmd = ws.Last();
                    TokenType? acceptor = ws.Length > 2 ? TokenTypeFromName(ws[1]) : null;
                    TokenType? dataAcceptor = ws.Length > 3? TokenTypeFromName(ws[2]):null;
                    SetAcceptedType(ws.Last(), acceptor ?? TokenType.Keyword, dataAcceptor);
                    walker.Remove(location.Index, w.Length);
                    return new Token(dataAcceptor.HasValue?TokenType.FunctionKeyword: TokenType.Keyword, cmd, walker.Location);
                }

                location = walker.Location;
                position = walker.Position;
                var word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());
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
                        var key = string.Join("", walker.WalkUntil(c=> Regex.IsMatch(c,"[\\s=;,]")));
                        if (!string.IsNullOrEmpty(key))
                        {
                            walker.MoveToProcedure();
                            this.SwitchToken = TokenType.IdentifierKeyword;
                            var m = Regex.IsMatch(walker.Current, "[,;\n]") ? walker.Walk() : TokenizeCode(walker)?.Value;
                            this.SwitchToken = null;
                            if (!string.IsNullOrEmpty(m))
                            {
                                switch (m.ToLower())
                                {
                                    case "\n":
                                    case ",":
                                    case ";":
                                        if (Compiler != null) try { Compiler.Reserves[key] = ""; } catch { }
                                        return GetAcceptedToken(TokenType.Comment, $"// Reserved {Newtonsoft.Json.JsonConvert.ToString(key)} as a noise", location);
                                    case "be":
                                    case "as":
                                    case "=":
                                        walker.MoveToProcedure();
                                        var val = TokenizeCode(walker)?.Value ?? "";
                                        try { Compiler?.Reserves.Add(key, val); } catch { }
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
                    default:
                        break;
                }

                var current = walker.Current;
                if (current != null)
                {
                    location = walker.Location;
                    word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());
                    lword = word.ToLower();

                    //string c = null;
                    //try { c = this.Compiler?.GetFunctionCommand(word); } catch { }
                    //if (c != null)
                    //{
                    //    walker.Move(word.Length);
                    //    return GetAcceptedToken(TokenType.FunctionKeyword, c, location);
                    //}

                    switch (lword)
                    {
                        case "command":
                        case "begin":
                        case "doing":
                        case "end":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.Statement, word, location);
                        
                        case "as":
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

                        case "data":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return GetAcceptedToken(TokenType.IdentifierKeyword, word, location);

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
                        walker.WalkWhile(w => w == "#").ToArray();
                        return GetAcceptedToken(TokenType.Statement, current, location);
                    }

                    //if (current == ":")
                    //    return GetAcceptedToken(TokenType.End | TokenType.Symbol, current, location);

                    var p = string.Concat(walker.PeekUntil(ch => string.IsNullOrWhiteSpace(ch)).ToArray());
                    if (Regex.IsMatch(p, "^\\w+:\\/([\\/][^\\s\\[\\]\\{\\}\\/\\\\]+)+$"))
                        return TokenizePath(walker, location);
                }
            }

            var wrd = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());
            var tk = GetAcceptedToken(TokenType.Unknown, wrd, walker.Location);
            if (tk.Value != wrd)
            {
                walker.Replace(walker.Location, wrd.Length, tk.Value.ToCharArray().Select(v => v.ToString()).ToArray());
                var ntk = base.TokenizeCode(walker);
                return ntk.Update(Type: ntk.Type | tk.Type);
            }
            else return base.TokenizeCode(walker);
        }

        protected virtual Token TokenizeXPath(CodeWalker walker, Position location)
        {
            walker.Walk();
            var value = string.Concat(walker.WalkUntil(ch => ch == "\\" && walker.Peek(-1) != Compiler?.Options?.Escape).ToArray());
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
            if (Regex.IsMatch(path, "^\\w+:\\/\\/.+$")) return UseUrl(path);
            var compiler = this.Compiler as MiMFa.Compiler.Compiler;
            if (compiler == null) return 0;
            var baseDirectory = Regex.IsMatch(path, "^\\.[\\/\\\\]") ?
                compiler?.ResourceProvider?.DirectoryName(compiler.Input?.Source ?? "DaRQ\\Library\\index") :
                compiler?.ResourceProvider?.DirectoryName("DaRQ\\Library\\index");
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
            if (string.IsNullOrWhiteSpace(folder)) return 0;
            folder = Regex.Replace(folder, "[\\/\\\\]$", "");
            int results = 0;
            try
            {
                var rp = Compiler.ResourceProvider;
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
            if (string.IsNullOrWhiteSpace(file)) return 0;
            int num = 0;
            if (!Regex.IsMatch(file, "\\.(darq|js)$", RegexOptions.IgnoreCase))
                return (num = UseFile(file + ".darq")) > 0 ? num : UseFile(file + ".js");
            file = System.IO.Path.GetFullPath(file);
            try
            {
                var compiler = this.Compiler as MiMFa.Compiler.Compiler;
                var rp = compiler?.ResourceProvider;
                var modules = (compiler as MiMFa.Compiler.DaRQ.Compiler)?.Libraries;
                if (modules != null && modules.ContainsKey(file)) return 1;
                if (rp != null && rp.Exists(file))
                {
                    Input input = compiler.Input;
                    Output output = compiler.Output;
                    int c = modules.Count;
                    (compiler as MiMFa.Compiler.DaRQ.Compiler)?.Libraries.Add(
                        file,
                        (compiler as MiMFa.Compiler.DaRQ.Compiler).Compile(new Input(rp.GetFileContents(file, System.Text.Encoding.UTF8), file)).Content);
                    compiler.Input = input;
                    compiler.Output = output;
                    return (compiler as MiMFa.Compiler.DaRQ.Compiler).Libraries.Count - c;
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
            if (dataAcceptor.HasValue) DataAcceptors[name] = dataAcceptor.Value;
            CaseAcceptors[lname] = name;
        }
        protected virtual Token GetAcceptedToken(TokenType type, string name, Position location = null)
        {
            string lname = name.ToLower();
            bool iscommand = LastToken == null || (
                !LastToken.IsMatch(".") &&
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
                var reserves = Compiler?.Reserves;
                if (reserves != null)
                {
                    foreach (var reserve in reserves)
                    {
                        if (word == reserve.Key)
                        {
                            if (word == reserve.Value) SwitchNamespace = true;
                            else walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray().Select(v=>v.ToString()).ToArray());
                            return true;
                        }
                    }
                    var lw = word.ToLower();
                    foreach (var reserve in reserves)
                    {
                        if (lw == reserve.Key.ToLower())
                        {
                            if (word == reserve.Value) SwitchNamespace = true;
                            else walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray().Select(v => v.ToString()).ToArray());
                            return true;
                        }
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
                var reserves = Compiler?.Reserves;
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
