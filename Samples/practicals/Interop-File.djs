// Practical: export structured DEAL data through .NET resource APIs.
// Requires ExtendedHostFunctions exposed as "host".
// Use Samples/practicals/Interop-File.djs;

Use deal/core;
Use deal/make/object;
Use deal/data/file;
Use deal/data/local;

Consider records be [
    { id: 1, title: "Declarative", tags: ["language", "intent"] },
    { id: 2, title: "Deterministic", tags: ["compiler", "reliability"] },
    { id: 3, title: "Interoperable", tags: ["JavaScript", ".NET"] }
];

Consider root be application.Engine.Resource.TempDirectory;
Consider jsonLinesPath be application.Engine.Resource.Combine(root, "deal-records.jsonl");
Consider binaryPath be application.Engine.Resource.Combine(root, "deal-summary.txt");

// File.fields converts each record to JSON text and passes a real CLR string[].
Consider output be File(jsonLinesPath);
output.fields(records);

// Read the JSON Lines file back into JavaScript objects.
Consider restored be [];
For each record of output.fields(),
    Append restored, record;

// Local.write converts the supplied text to UTF-8 byte[] before calling .NET.
Local.write(binaryPath, "Exported " + restored.length + " records from DEAL.JS.");

Print restored;
Print Local.read(binaryPath);
