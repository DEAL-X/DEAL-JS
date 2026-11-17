using MiMFa.Engine.Put;
using DealEngine = MiMFa.Engine.DEAL.JS.Engine;

var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
Environment.CurrentDirectory = repositoryRoot;

var inputPath = Path.Combine(repositoryRoot, "Tests", "inputs", "Hierarchical-Keyword-Spelling.djs");
var expectedPath = Path.Combine(repositoryRoot, "Tests", "expected", "Hierarchical-Keyword-Spelling.expected.js");
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
var failures = new List<string>();
for (var index = 0; index < variants.Length; index++)
{
    if (!string.Equals(expected[index], canonicalOutput, StringComparison.Ordinal))
        throw new InvalidOperationException($"Expected-output assertion {index + 1} is not canonical: '{expected[index]}'.");

    using var engine = new DealEngine();
    var source = string.Join(Environment.NewLine, useLines.Append(variants[index]));
    var output = engine.Compile(new Input(source, inputPath));

    if (output.Errors.Count > 0)
        throw new InvalidOperationException($"Compilation failed for '{variants[index]}': {string.Join(Environment.NewLine, output.Errors)}");

    var statements = (output.Content ?? string.Empty)
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith("// Used ", StringComparison.Ordinal))
        .ToArray();

    if (statements.Length != 1)
    {
        failures.Add($"'{variants[index]}' produced {statements.Length} statements: {output.Content}");
        continue;
    }

    Console.WriteLine($"ACTUAL: {variants[index]} => {statements[0]}");
    if (!string.Equals(statements[0], expected[index], StringComparison.Ordinal))
        failures.Add($"'{variants[index]}' compiled to '{statements[0]}'; expected '{expected[index]}'.");
}

if (failures.Count > 0)
    throw new InvalidOperationException(string.Join(Environment.NewLine, failures));

Console.WriteLine("All hierarchical keyword spelling regression tests passed.");
