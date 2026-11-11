command #Temporary:identifier = {};

#Temporary.directory:function = (name = null) => name? application.Engine.Resource.TempDirectory + name + application.Engine.Resource.DirectorySeparator : application.Engine.Resource.TempDirectory;
#Temporary.path:function = (name = "temp.tmp") => application.Engine.Resource.TempDirectory + name;

#Temporary.clear:function = () => application.Engine.Resource.Delete(application.Engine.Resource.TempDirectory);
#Temporary.extract:function = (directory) => application.Engine.Resource.Copy(application.Engine.Resource.TempDirectory, directory);