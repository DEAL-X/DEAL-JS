using MiMFa.Engine;
using MiMFa.Engine.Core;
using MiMFa.Engine.Resource;
using System;
using System.Collections.Generic;

namespace MiMFa.Engine
{
    public class Engine
    {
        public IList<StageBase> CompileStages { get; set; }
        public StageBase ExecuteStage { get; set; }

        public Core.Version Version { get; } = new Core.Version(1);

        public Options Options { get; }

        public Dictionary<string, string> Libraries { get; protected set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public event LogEventHandler Logged = null;
        public event LibraryEventHandler AttachedLibrary = null;
        public Input Input { get; set; }
        public Output Output { get; set; }
        public ResourceProvider ResourceProvider { get; set; }

        public Engine(StageBase[] compileStages, StageBase executeStage, Options options = null, ResourceProvider resourceProvider = null)
        {
            this.CompileStages = new List<StageBase>(compileStages ?? Array.Empty<StageBase>());
            this.ExecuteStage = executeStage ?? new Stage((i, c) => i);
            this.Options = options ?? new Options();
            this.ResourceProvider = resourceProvider ?? new ResourceProvider();
        }

        public Output Compile(Input input)
        {
            bool isfirst = Input == null;
            Input = input;
            object data = input.Content;
            Output = new Output(Input.Source);
            string sn = null;
            if (isfirst) OnLogged("Compiling is Started", LogStatus.Subject);
            try
            {
                string source = System.IO.Path.GetFullPath("DEAL.JS");
                if (!string.IsNullOrWhiteSpace(Input.Source)) OnLogged($"Compiling the {Input.Source.Replace(source, ".\\DEAL.JS")}", LogStatus.Message);
                foreach (var stage in CompileStages)
                {
                    sn = stage.GetType().Name;
                    if (isfirst) OnLogged($"{sn} stage is checking...", LogStatus.Info);
                    data = stage.Transform(data, this);
                    if (isfirst) OnLogged($"{sn} stage tasks completed. ✔️ ", LogStatus.Success);
                    else OnLogged(" .", null);
                }
                Output.Content = (data ?? string.Empty).ToString();
                if (!isfirst) OnLogged(" ✔️ ", null);
                return Output;
            }
            catch (Exception e)
            {
                if (isfirst) OnLogged($"{sn} stage is not completed! ❌ ", LogStatus.Error);
                else OnLogged($" ❌ ", null);
                OnLogged(e.Message, LogStatus.Error);
                return Output.Error(e);
            }
            finally
            {
                if (isfirst) OnLogged("Compiling is Ended", LogStatus.Subject);
            }
        }
      
        public object Execute(Input input)
        {
            return Execute(Compile(input));
        }
        public object Execute(Output output)
        {
            return Execute(output.Content, output.Source);
        }
        public IEnumerable<object> Execute(params KeyValuePair<string, string>[] pathScripts)
        {
            foreach (var item in pathScripts)
                yield return Execute(item.Key, item.Value);
        }
        public object Execute(string script, string path = null)
        {
            OnLogged("Executing is Started", LogStatus.Subject);
            if (!string.IsNullOrWhiteSpace(path)) OnLogged("Source: " + path, LogStatus.Info);
            try
            {
                return ExecuteStage.Transform(script, this);
            }
            catch (Exception e)
            {
                OnLogged($"Could not execute the {(string.IsNullOrWhiteSpace(path) ?"scripts":"\""+ path + "file \"")}! ❌ ", LogStatus.Error);
                OnLogged(e.Message, LogStatus.Error);
                Output.Error(e);
                return null;
            }
            finally
            {
                OnLogged("Executing is Ended", LogStatus.Subject);
            }
        }

        public virtual void AttachLibrary(string path, string code)
        {
            Libraries[path] = code;
            OnAttachedLibrary(path, code);
        }
        public virtual bool HasLibrary(string path)
        {
            return Libraries.ContainsKey(path);
        }

        public void OnAttachedLibrary(string path, string content = null)
        {
            if (AttachedLibrary != null) AttachedLibrary(this, new LibraryEventArgs(path, content));
        }
        public void OnLogged(string message = "", LogStatus? status = LogStatus.Info, DateTime? time = null)
        {
            if (Logged != null) Logged(this, new LogEventArgs(message, status, time));
        }
    }
}
