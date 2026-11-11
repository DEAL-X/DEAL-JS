# DEAL.JS sample collection

The samples are organized by purpose:

- [tests/](tests/) — compiler stress inputs, expected JavaScript, captured result snapshots, and executable runtime smoke tests.
- [tutorials/](tutorials/) — progressive, commented DEAL.JS examples, from language fundamentals through browser and CLR interoperability.
- [practicals/](practicals/) — complete workflows that combine reusable modules, data processing, browser operations, and persistence.

Recommended learning path:

1. [Getting started](tutorials/01-Getting-Started.djs) — values, collections, iteration, and output.
2. [Browser and Fetch](tutorials/02-Browser-and-Fetch.djs) — browser control and asynchronous HTTP requests.
3. [Commands](tutorials/03-Commands.djs) — reusable command syntax.
4. [CLR object interoperability](tutorials/04-CLR-Object-Interop.djs) — JavaScript-to-.NET arrays and collections, including byte[] and string[].

The expected and result JavaScript files in `tests/` are compiler test artifacts. They are intentionally JavaScript because they are compared with the compiler's generated target output; they are not intended as the primary language examples.

## JavaScript and .NET interop

The deal/make/object.djs module contains explicit converters for ClearScript host boundaries. The host exposes ExtendedHostFunctions as host; see the root README and quick-start guide (../SAMPLE.md) for setup.

- Make.arrayObject — CLR arrays such as string[].
- Make.listObject — generic List<T>.
- Make.iteratorObject / Make.enumerableObject — materialized List<T> instances assignable to IEnumerable<T>.
- Make.dictionaryObject — Dictionary<TKey, TValue>, including nested JavaScript objects.
- Make.hashSetObject, Make.queueObject, and Make.stackObject — common generic collections.
- Make.bytesObject — CLR byte[] from strings, buffers, or numeric collections.

See the .NET interop tutorial (tutorials/04-DotNet-Interop.djs), the file-export practical (practicals/Clr-Interop-File-Export.djs), and the runtime smoke test (tests/runtime/Clr-Interop-Smoke-Test.js).
