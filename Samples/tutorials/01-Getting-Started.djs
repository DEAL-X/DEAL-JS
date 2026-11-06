// Tutorial 01: Getting started with DEAL.JS.
// DEAL instructions are sentence-like and compile deterministically to JavaScript.

Use deal/core;

// Define a collection and a destination.
Consider values is [1, 2, 3, 4, 5];
Consider doubled is [];

// Iterate using a declarative DEAL instruction.
For each value of values,
    Append doubled, value * 2;

// Display the result.
Print doubled;
