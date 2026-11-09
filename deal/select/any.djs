// Select a single match or multiple matches.
// Return the single value when exactly one match exists;
// otherwise return the result array.
command #ANY:start|function:string() {
    let [query, source] = detect_Selector_Arguments(...arguments);

    if (query == null) return source;

    let result = ALL(...arguments);

    if (result.length == 1) return result[0];
    return result;
}