// Tutorial 04: Convert JavaScript values and collections for .NET APIs.
// The host must expose Microsoft.ClearScript.ExtendedHostFunctions as "host".

Use deal/core;
Use deal/make/object;

Consider names is ["Ada", "Grace", "Edsger"];

// 1. A JavaScript array is not automatically a CLR string[].
// Convert it before passing it to a .NET method with a string[] parameter.
Consider clrNames is Make.arrayObject(names, "System.String");
application.Engine.Resource.SetFileLines(
    application.Engine.Resource.Combine(application.Engine.Resource.TempDirectory, "deal-names.txt"),
    clrNames,
    null
);

// 2. Create generic .NET collections for strongly typed method parameters.
Consider clrList is Make.listObject([10, 20, 30], "System.Int32");
Consider clrEnumerable is Make.iteratorObject(new Set(["read", "write"]), "System.String");
Consider clrUniqueNames is Make.hashSetObject(names, "System.String");

// clrList is List<Int32>; clrEnumerable is List<String>, which implements IEnumerable<String>.
// clrUniqueNames is HashSet<String>.

// 3. Convert JavaScript objects and nested values to Dictionary<String, Object>.
Consider profile is Make.dictionaryObject({
    name: "DEAL.JS",
    version: 1,
    tags: ["declarative", "interoperable"],
    metadata: { language: "JavaScript", target: ".NET" }
});

// 4. Convert plain objects into a requested .NET class when it has a
// parameterless constructor and writable properties with matching names.
// Consider point is Make.objectObject({ X: 12, Y: 34 }, "MyApplication.Point");

// 5. Convert strings and numeric arrays to byte[] for APIs such as SetFileContents.
Consider textBytes is Make.bytesObject("Hello from DEAL.JS");
Consider numericBytes is Make.bytesObject([68, 69, 65, 76], null);

Print clrNames;
Print clrList;
Print clrEnumerable;
Print profile;
Print textBytes.Length;
Print numericBytes.Length;
