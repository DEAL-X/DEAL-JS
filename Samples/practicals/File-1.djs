// HOW TO USE:
// Use Samples/practicals/File-1.djs;

Use deal/core;
Use deal/data/file;
Use environment;

Consider myCart be file temporary path "file-sample.txt";
myCart append {Item:"Snakes", Count:2, Price:2, Brand:"Guau"};
myCart append {Item:"Chocolate", Count:5, Price:2.5, Type:"V", Unit:"Kilo", Desc:"90% coca"};
myCart append {Item:"Beverage", Count:1, Price:1.2, Brand:"CocaCola", Desc:"1.5L"};
myCart append {Item:"Milk", Count:6, Price:0.2, Type:"V", Unit:"Liter"};

print "The myCart variable is a file contain:";
print myCart read it;