# DEAL.JS tutorials

Follow the tutorials in order. Each `.djs` file is DEAL source with comments explaining the concepts being introduced. Run the files through the DEAL.JS compiler and inspect generated JavaScript when you want to study the compilation result.

1. [Getting started](01-Getting-Started.djs) — values, collections, iteration, and output.
2. [Browser and Fetch](02-Browser-and-Fetch.djs) — synchronous browser operations and asynchronous Fetch. The .NET host selects Chrome, Edge, or Firefox.
3. [Commands](03-Commands.djs) — reusable command syntax and iteration.
4. [CLR object interoperability](04-CLR-Object-Interop.djs) — converting JavaScript values into real .NET string/byte arrays, generic collections, dictionaries, dynamic .NET objects, and enumerable-compatible objects.

Tutorial 04 requires the ClearScript host to expose `Microsoft.ClearScript.ExtendedHostFunctions` as global `host`. It demonstrates the converters provided by `deal/make/object.djs`, including the `string[]` conversion needed by .NET methods such as `ResourceProvider.SetFileLines`. Load `deal/make` to access the complete `Make` utility module.
