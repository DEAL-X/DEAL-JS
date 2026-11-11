// CLR collection interoperability smoke test.
// Run after the host exposes ExtendedHostFunctions as "host" and loads deal/make.
// Verifies the actual .NET collection types and the ResourceProvider overloads.

function clrObjectInteropSmokeTest(application) {
    const resource = application.Engine.Resource;
    const values = ["first", "second", "third"];

    const clrArray = Make.arrayObject(values, "System.String");
    if (clrArray.GetType().FullName !== "System.String[]")
        throw new Error("Make.arrayObject did not create System.String[].");

    const clrList = Make.listObject(values, "System.String");
    if (clrList.GetType().GetGenericTypeDefinition().FullName !== "System.Collections.Generic.List`1" ||
        clrList.GetType().GetGenericArguments()[0].FullName !== "System.String")
        throw new Error("Make.listObject did not create List<string>.");

    const clrEnumerable = Make.iteratorObject(values, "System.String");
    if (clrEnumerable.GetType().GetGenericTypeDefinition().FullName !== "System.Collections.Generic.List`1" ||
        clrEnumerable.GetType().GetGenericArguments()[0].FullName !== "System.String")
        throw new Error("Make.iteratorObject must return a concrete List<string> implementing IEnumerable<string>.");

    const dictionary = Make.dictionaryObject(
        { one: 1, two: 2 },
        "System.String",
        "System.Int32"
    );
    if (!dictionary.ContainsKey("one") || dictionary["two"] !== 2)
        throw new Error("Make.dictionaryObject did not preserve typed key/value pairs.");

    const hashSet = Make.hashSetObject(values, "System.String");
    if (hashSet.Count !== 3)
        throw new Error("Make.hashSetObject returned an unexpected item count.");

    const path = resource.Resolve("clr-object-interop-smoke.txt");
    try {
        // This call exercises the exact string[] overload that previously failed.
        resource.SetFileLines(path, clrArray, null);
        const first = Make.array(resource.GetFileLines(path));
        if (first.length !== 3 || first[0] !== "first" || first[2] !== "third")
            throw new Error("The string[] ResourceProvider round-trip failed.");

        // List<string> and the concrete IEnumerable<string> implementation.
        resource.SetFileLines(path, clrList, null);
        resource.SetFileLines(path, clrEnumerable, null);
        const second = Make.array(resource.GetFileLines(path));
        if (second.length !== 3 || second[1] !== "second")
            throw new Error("The IEnumerable<string> ResourceProvider round-trip failed.");

        return {
            passed: true,
            arrayType: clrArray.GetType().FullName,
            listType: clrList.GetType().FullName,
            dictionaryCount: dictionary.Count,
            hashSetCount: hashSet.Count,
            lineCount: second.length
        };
    } finally {
        resource.Delete(path);
    }
}
