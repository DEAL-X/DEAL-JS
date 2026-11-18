command #Temporary:identifier = {};

command #Temporary.directory:function = (name = null) => name? application.Engine.Resource.TempDirectory + name + application.Engine.Resource.DirectorySeparator : application.Engine.Resource.TempDirectory;
command #Temporary.path:function = (name = "temp.tmp") => application.Engine.Resource.TempDirectory + name;

command #Temporary.clear:function = () => application.Engine.Resource.Delete(application.Engine.Resource.TempDirectory);
command #Temporary.extract:function = (directory) => application.Engine.Resource.Copy(application.Engine.Resource.TempDirectory, directory);