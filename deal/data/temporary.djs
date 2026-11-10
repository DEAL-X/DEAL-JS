command #Temporary:identifier = {};

#Temporary.directory:function = (name = null) => application.Engine.Resource.TempDirectory + name + application.Engine.Resource.DirectorySeparator;
#Temporary.path:function = (name = "temp.tmp") => Temporary.directory(`${name}`);

#Temporary.delete:function = () => application.Engine.Resource.Delete(Temporary.directory());
#Temporary.extract:function = (directory) => application.Engine.Resource.Copy(Temporary.directory(), directory);