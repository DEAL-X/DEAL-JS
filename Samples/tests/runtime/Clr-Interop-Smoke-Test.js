// Runtime smoke test for DEAL JavaScript-to-.NET collection conversion.
// Run after loading deal/core, deal/make/object, and the host's "host" object.

(function () {
    function assert(condition, message) {
        if (!condition) throw new Error("CLR interop smoke test failed: " + message);
    }

    const stringArray = Make.arrayObject(["alpha", "beta"], "System.String");
    assert(stringArray.GetType().FullName === "System.String[]", "arrayObject must return System.String[]");
    assert(stringArray.Length === 2 && stringArray.GetValue(1) === "beta", "string[] contents");

    const integerList = Make.listObject([3, 5, 8], "System.Int32");
    assert(integerList.GetType().FullName.includes("System.Collections.Generic.List"), "listObject must return List<T>");
    assert(integerList.Count === 3 && integerList[2] === 8, "List<Int32> contents");

    const stringEnumerable = Make.iteratorObject(new Set(["read", "write"]), "System.String");
    assert(stringEnumerable.GetType().FullName.includes("System.Collections.Generic.List"), "iteratorObject must return an IEnumerable<T> implementation");
    assert(stringEnumerable.Count === 2, "IEnumerable<String> contents");

    const dictionary = Make.dictionaryObject({
        name: "DEAL.JS",
        metadata: { version: 1 },
        tags: ["declarative", "extensible"]
    });
    assert(dictionary.GetType().FullName.includes("System.Collections.Generic.Dictionary"), "dictionaryObject must return Dictionary<TKey,TValue>");
    assert(dictionary.Count === 3, "dictionary entry count");

    const hashSet = Make.hashSetObject(["a", "b", "a"], "System.String");
    assert(hashSet.Count === 2, "HashSet<T> removes duplicates");

    const bytes = Make.bytesObject("DEAL");
    assert(bytes.GetType().FullName === "System.Byte[]", "bytesObject must return byte[]");
    assert(bytes.Length === 4, "UTF-8 byte length");

    const path = application.Engine.Resource.Combine(
        application.Engine.Resource.TempDirectory,
        "deal-clr-interop-smoke.txt"
    );
    application.Engine.Resource.SetFileLines(path, stringArray, null);
    application.Engine.Resource.AppendFileLines(path, stringEnumerable, null);
    const saved = Make.array(application.Engine.Resource.GetFileLines(path, null));
    assert(saved.length === 4 && saved[0] === "alpha" && saved[3] === "write", "ResourceProvider array and IEnumerable overloads");

    application.Engine.Resource.Delete(path);
    return "CLR interop smoke test passed.";
})();
