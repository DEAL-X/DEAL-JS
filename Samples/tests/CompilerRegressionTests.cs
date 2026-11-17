using MiMFa.Engine.Put;
using DealEngine = MiMFa.Engine.DEAL.JS.Engine;

var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
Environment.CurrentDirectory = repositoryRoot;

var inputPath = Path.Combine(repositoryRoot, "Samples", "tests", "inputs", "Hierarchical-Keyword-Spelling.djs");
var expectedPath = Path.Combine(repositoryRoot, "Samples", "tests", "expected", "Hierarchical-Keyword-Spelling.expected.js");
var sourceLines = File.ReadAllLines(inputPath).Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
var useLines = sourceLines.Where(line => line.StartsWith("Use ", StringComparison.OrdinalIgnoreCase)).ToArray();
var variants = sourceLines.Where(line =>
    line.StartsWith("make ", StringComparison.OrdinalIgnoreCase) ||
    line.StartsWith("make.", StringComparison.OrdinalIgnoreCase)).ToArray();
var expected = File.ReadAllLines(expectedPath).Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();

if (variants.Length != 3)
    throw new InvalidOperationException($"Expected exactly three spelling variants, found {variants.Length}.");
if (expected.Length != variants.Length)
    throw new InvalidOperationException($"Expected {variants.Length} output assertions, found {expected.Length}.");

const string canonicalOutput = "Make.string;";
for (var index = 0; index < variants.Length; index++)
{
    if (!string.Equals(expected[index], canonicalOutput, StringComparison.Ordinal))
        throw new InvalidOperationException($"Expected-output assertion {index + 1} is not canonical: '{expected[index]}'.");

    using var engine = new DealEngine();
    var source = string.Join(Environment.NewLine, useLines.Append(variants[index]));
    var output = engine.Compile(new Input(source, inputPath));

    if (output.Errors.Count > 0)
        throw new InvalidOperationException($"Compilation failed for '{variants[index]}': {string.Join(Environment.NewLine, output.Errors)}");

    var loadedStringModule = engine.Dependencies.Any(path =>
        path.Replace('\\', '/').EndsWith("/deal/make/string.djs", StringComparison.OrdinalIgnoreCase));
    if (!loadedStringModule)
        throw new InvalidOperationException($"The deal/make/string dependency was not loaded for '{variants[index]}'.");

    var statements = (output.Content ?? string.Empty)
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith("// Used ", StringComparison.Ordinal))
        .ToArray();

    if (statements.Length != 1)
        throw new InvalidOperationException($"Expected one generated statement for '{variants[index]}', found {statements.Length}: {output.Content}");

    if (!string.Equals(statements[0], expected[index], StringComparison.Ordinal))
        throw new InvalidOperationException($"'{variants[index]}' compiled to '{statements[0]}'; expected '{expected[index]}'.");

    Console.WriteLine($"PASS: {variants[index]} => {statements[0]}");
}

Console.WriteLine("All hierarchical keyword spelling regression tests passed.");
