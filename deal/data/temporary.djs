command #Temporary:identifier = {};

command Temporary.directory (name = null) => name ? application.Engine.Resource.TempDirectory + name + application.Engine.Resource.DirectorySeparator : application.Engine.Resource.TempDirectory;
command Temporary.path (name = "temp.tmp") => application.Engine.Resource.TempDirectory + name;

command Temporary.clear () => application.Engine.Resource.Delete(application.Engine.Resource.TempDirectory);
command Temporary.extract (directory) => application.Engine.Resource.Copy(application.Engine.Resource.TempDirectory, directory);