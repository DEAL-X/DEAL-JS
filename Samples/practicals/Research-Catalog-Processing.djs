// Practical: normalize, validate, deduplicate, and persist a research catalog.
// This self-contained fixture demonstrates the workflow boundaries described in
// the DEAL research article. It performs no live acquisition.
//
// The output is split into accepted JSONL records and an explicit rejection log.

Use deal/core;
Use deal/make;
Use deal/data/file;

const retrievedAt = "2026-11-11T00:00:00Z";
const schemaVersion = "1.0";

const sourceRecords = [
    { id: "R-001", title: "  Open Data Methods ", price: "19.50", discount: 10, source_url: "https://example.org/a" },
    { id: "R-002", title: "Reproducible Research", price: "0", discount: 0, source_url: "https://example.org/b" },
    { id: "R-001", title: "Duplicate Identifier", price: "12.00", discount: 5, source_url: "https://example.org/c" },
    { id: "R-004", title: "Missing Price", price: "", discount: 10, source_url: "https://example.org/d" },
    { id: "R-005", title: "Invalid Discount", price: "8.00", discount: 125, source_url: "https://example.org/e" }
];

const accepted = [];
const rejected = [];
const seen = new Set();

function normalizeText(value) {
    return value == null ? "" : String(value).normalize("NFKC").trim().replace(/\s+/g, " ");
}

function reject(record, reason) {
    rejected.push({
        source_url: record.source_url,
        identifier: normalizeText(record.id),
        reason: reason
    });
}

for each raw of sourceRecords,
    Consider id is normalizeText(raw.id);
    Consider title is normalizeText(raw.title);
    Consider priceText is normalizeText(raw.price);
    Consider discount is Number(raw.discount);

    If !id,
        reject(raw, "missing_identifier");
    Else if seen.has(id),
        reject(raw, "duplicate_identifier");
    Else if priceText is empty,
        seen.add(id);
        reject(raw, "missing_price");
    Else if !Number.isFinite(Number(priceText)) || Number(priceText) < 0,
        seen.add(id);
        reject(raw, "invalid_price");
    Else if !Number.isFinite(discount) || discount < 0 || discount > 100,
        seen.add(id);
        reject(raw, "out_of_range_discount");
    Else,
        seen.add(id);
        Append accepted, {
            id: id,
            title: title,
            price: Number(priceText),
            discount: discount,
            final_price: Number((Number(priceText) * (1 - discount / 100)).toFixed(2)),
            source_url: raw.source_url,
            retrieved_at: retrievedAt,
            schema_version: schemaVersion
        };

// Guard the fixture contract: two accepted records and three explicit rejections.
if (accepted.length !== 2 || rejected.length !== 3)
    throw new Error("The catalog fixture must produce exactly two accepted and three rejected records.");

if (accepted[0].final_price !== 17.55 || accepted[1].final_price !== 0)
    throw new Error("Derived prices or valid zero-value handling changed.");

if (rejected.filter(item => item.reason === "duplicate_identifier").length !== 1 ||
    rejected.filter(item => item.reason === "missing_price").length !== 1 ||
    rejected.filter(item => item.reason === "out_of_range_discount").length !== 1)
    throw new Error("The fixture rejection categories do not match the data contract.");

Consider acceptedFile is File(application.Engine.Resource.Resolve("catalog-accepted.jsonl"), null, false);
Consider rejectedFile is File(application.Engine.Resource.Resolve("catalog-rejected.jsonl"), null, false);

acceptedFile.write(accepted.map(record => JSON.stringify(record)).join("\n"));
rejectedFile.write(rejected.map(record => JSON.stringify(record)).join("\n"));

Print {
    source_count: sourceRecords.length,
    accepted_count: accepted.length,
    rejected_count: rejected.length,
    accepted: accepted,
    rejected: rejected
};
