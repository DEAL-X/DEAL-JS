# Compiler and runtime tests

- `inputs/` contains the DEAL/JavaScript stress-test source files.
- `expected/` contains the expected JavaScript output for each stress test.
- `results/` contains the captured compiler result snapshots.
- `runtime/` contains executable smoke tests for the browser object model and native web integration.
- `fixtures/` contains local files used by runtime tests.

For each stress test, compare the generated JavaScript from `inputs/` with the corresponding file in `expected/`. The `results/` copy records the output captured from the implementation at the time it was saved. These files are kept separate so differences are easy to inspect.
