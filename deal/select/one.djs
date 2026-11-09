// Select the first matching element or value.
// Return null when no match exists.
command #ONE:start|function:string() {
    let [query, source] = detect_Selector_Arguments(...arguments);

    if (query == null) return source;

    let result = ALL(...arguments);
    return result.length > 0 ? result[0] : null;
}
