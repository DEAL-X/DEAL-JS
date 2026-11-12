// Practical: collect structured information from multiple pages.
// Browser navigation is synchronous; network Fetch calls are explicitly awaited.
// HOW TO USE:
// Use Samples/practicals/Website-Collecting.djs;

Use deal/core;
Use deal/web;

Consider sources be [
    "https://example.com/data.json",
    "https://example.org/data.json"
];
Consider collection be [];

For each address of sources,
    Try,
        Consider response be await fetch(address, { headers: { "Accept": "application/json" } });
        If response.ok,
            Consider record be await response.json();
            Append collection, { source: address, data: record };
        Otherwise,
            Log warning "HTTP " + response.status + " from " + address;
    Catch problem,
        Log error "Could not collect " + address + ": " + problem;

Print collection;
