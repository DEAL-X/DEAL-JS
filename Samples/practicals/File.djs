// HOW TO USE:
// Use Samples/practicals/File.djs;

Use deal/core;
Use deal/data/file;
Use environment;

Consider myFile be file temporary path "file-sample.txt";
myFile append {A:1, B:2};
myFile append {A:10, B:20, C:"The letter number multiple 10"};
myFile append {A:100, B:200, C:"The letter number multiple 100"};

print "The myFile variable is a file contain:";
print myFile read it;