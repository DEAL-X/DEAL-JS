// Tutorial 04: Convert JavaScript values into real .NET collections.
// Host prerequisite: expose ClearScript ExtendedHostFunctions as global "host":
// engine.AddHostObject("host", new ExtendedHostFunctions());
//
// Load deal/make (which includes deal/make/object) before running this tutorial.
// Type names are CLR names; values must be assignable to the requested element type.

Use deal/core;
Use deal/make;

Consider values be ["alpha", "beta", "gamma"];

// A real System.String[]: suitable for methods that require string[].
Consider clrArray be Make.arrayObject(values, "System.String");

// A real List<string>: suitable for List<string> and IEnumerable<string> parameters.
Consider clrList be Make.listObject(values, "System.String");

// A concrete List<string> implementing IEnumerable<string>.
Consider clrEnumerable be Make.iteratorObject(values, "System.String");

// A typed Dictionary<string, int>.
Consider counts be Make.dictionaryObject(
    { first: 1, second: 2, third: 3 },
    "System.String",
    "System.Int32"
);

// Convenience converters for string[] and byte[].
Consider clrStrings be Make.stringArrayObject(values);
Consider clrBytes be Make.byteArrayObject("DEAL.JS");

// Convert a primitive using a CLR type reference.
Consider integerType be Make.typeObject("System.Int32");
Consider convertedNumber be Make.convertObject("42", integerType);

// Plain JavaScript records can also become dynamic .NET objects.
Consider dynamicRecord be Make.objectObject({ name: "DEAL.JS", revision: 1 });

// Other common collection types.
Consider uniqueValues be Make.hashSetObject(values, "System.String");
Consider pendingValues be Make.queueObject(values, "System.String");
Consider recentValues be Make.stackObject(values, "System.String");
Consider legacyValues be Make.arrayListObject(values);

// The original overload-resolution issue: clrArray is a genuine .NET string[].
Consider outputPath be application.Engine.Resource.Resolve("clr-interop-array.txt");
application.Engine.Resource.SetFileLines(outputPath, clrArray, null);

// List<string> is passed through the IEnumerable<string> overload.
Consider listPath be application.Engine.Resource.Resolve("clr-interop-list.txt");
application.Engine.Resource.SetFileLines(listPath, clrList, null);

// iteratorObject returns a concrete List<T>, not an uninstantiable interface.
Consider enumerablePath be application.Engine.Resource.Resolve("clr-interop-enumerable.txt");
application.Engine.Resource.SetFileLines(enumerablePath, clrEnumerable, null);

Print {
    arrayType: clrArray.GetType().FullName,
    stringArrayType: clrStrings.GetType().FullName,
    byteArrayType: clrBytes.GetType().FullName,
    listType: clrList.GetType().FullName,
    dictionaryType: counts.GetType().FullName,
    convertedNumber: convertedNumber,
    dynamicObjectType: dynamicRecord.GetType().FullName,
    dynamicObjectName: dynamicRecord.name,
    dictionaryCount: counts.Count,
    hashSetCount: uniqueValues.Count,
    queueCount: pendingValues.Count,
    stackCount: recentValues.Count,
    arrayListCount: legacyValues.Count
};
