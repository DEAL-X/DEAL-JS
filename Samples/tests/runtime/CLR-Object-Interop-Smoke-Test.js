// CLR collection interoperability smoke test.
// Run after the host exposes ExtendedHostFunctions as "host" and loads deal/make.
// Verifies the actual .NET collection types and the ResourceProvider overloads.

function clrObjectInteropSmokeTest(application) {
    const resource = application.Engine.Resource;
    const values = ["first", "second", "third"];

    const clrArray = Make.arrayObject(values, "System.String");
    if (clrArray.GetType().FullName !== "System.String[]")
        throw new Error("Make.arrayObject did not create System.String[].");

    const clrStrings = Make.stringArrayObject(values);
    if (clrStrings.GetType().FullName !== "System.String[]")
        throw new Error("Make.stringArrayObject did not create System.String[].");

    const clrBytes = Make.byteArrayObject([65, 66, 67]);
    if (clrBytes.GetType().FullName !== "System.Byte[]")
        throw new Error("Make.byteArrayObject did not create System.Byte[].");

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
    if (!dictionary.ContainsKey("one") || Number(dictionary["two"]) !== 2)
        throw new Error("Make.dictionaryObject did not preserve typed key/value pairs.");

    const intType = Make.typeObject("System.Int32");
    if (Number(Make.convertObject("42", intType)) !== 42)
        throw new Error("Make.convertObject did not convert through System.Convert.ChangeType.");

    const dynamicRecord = Make.objectObject({ name: "DEAL.JS", revision: 1 });
    if (dynamicRecord.name !== "DEAL.JS" || dynamicRecord.revision !== 1)
        throw new Error("Make.objectObject did not create a populated ExpandoObject.");

    const hashSet = Make.hashSetObject(values, "System.String");
    if (hashSet.Count !== 3)
        throw new Error("Make.hashSetObject returned an unexpected item count.");

    const path = resource.Resolve("clr-object-interop-smoke.txt");
    const binaryPath = resource.Resolve("clr-object-interop-bytes-smoke.bin");
    try {
        // This call exercises the exact string[] overload that previously failed.
        resource.SetFileLines(path, clrArray, null);
        if (resource.GetFileLine(path, 0, null) !== "first" ||
            resource.GetFileLine(path, 2, null) !== "third")
            throw new Error("The string[] ResourceProvider round-trip failed.");

        resource.SetFileContents(binaryPath, clrBytes);
        const storedBytes = resource.GetFileContents(binaryPath);
        if (Number(storedBytes[0]) !== 65 || Number(storedBytes[2]) !== 67)
            throw new Error("The CLR byte[] ResourceProvider round-trip failed.");

        // List<string> and the concrete IEnumerable<string> implementation.
        resource.SetFileLines(path, clrList, null);
        resource.SetFileLines(path, clrEnumerable, null);
        if (resource.GetFileLine(path, 0, null) !== "first" ||
            resource.GetFileLine(path, 1, null) !== "second" ||
            resource.GetFileLine(path, 2, null) !== "third")
            throw new Error("The IEnumerable<string> ResourceProvider round-trip failed.");

        return {
            passed: true,
            arrayType: clrArray.GetType().FullName,
            byteArrayType: clrBytes.GetType().FullName,
            listType: clrList.GetType().FullName,
            dictionaryCount: dictionary.Count,
            dynamicObjectType: dynamicRecord.GetType().FullName,
            hashSetCount: hashSet.Count,
            lineCount: 3
        };
    } finally {
        resource.Delete(path);
        resource.Delete(binaryPath);
    }
}
