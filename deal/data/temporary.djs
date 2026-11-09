command #TEMPORARY:identifier = {};

#TEMPORARY.directory:function = (name = null) => application.Engine.Resource.TempDirectory + name + application.Engine.Resource.DirectorySeparator;
#TEMPORARY.path:function = (name = "temp.tmp") => TEMPORARY.directory(`${name}`);

#TEMPORARY.delete:function = () => application.Engine.Resource.Delete(TEMPORARY.directory());
#TEMPORARY.extract:function = (directory) => application.Engine.Resource.Copy(TEMPORARY.directory(), directory);