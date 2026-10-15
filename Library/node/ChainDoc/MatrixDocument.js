"use strict";
const { ChainedDocument } = require("./ChainedDocument");

/** Lightweight lazy matrix facade. Coordinates are zero-based: (column, row). */
class MatrixDocument {
  constructor(document, options = {}) {
    if (!(document instanceof ChainedDocument)) throw new TypeError("document must be a ChainedDocument.");
    this.document = document;
    this.defaultParameter = options.defaultParameter ?? "";
    this.defaultString = options.defaultString ?? "";
  }
  toParameter(value) { return value == null ? this.defaultParameter : value; }
  fromParameter(value) { return value == null ? this.defaultString : String(value); }
  get width() { return this.document.ColumnsCount; }
  get height() { return this.document.RowsCount; }
  get(x, y) { return this.toParameter(this.document.readCell(x, y)); }
  set(x, y, value) {
    if (x < 0 || y < 0 || !Number.isSafeInteger(x) || !Number.isSafeInteger(y)) throw new RangeError("Matrix coordinates must be non-negative integers.");
    const document = this.document;
    const replacement = this.fromParameter(value);
    function* rewrittenRows() {
      let rowIndex = 0;
      for (const existing of document.readRows()) {
        if (rowIndex === y) {
          const row = existing.slice();
          while (row.length <= x) row.push("");
          row[x] = replacement;
          yield row;
        } else {
          yield existing;
        }
        rowIndex++;
      }
      while (rowIndex <= y) {
        const row = [];
        while (row.length <= x) row.push("");
        if (rowIndex === y) row[x] = replacement;
        yield row;
        rowIndex++;
      }
    }
    // writeRows delegates to a temp-file-backed connector when available, so the source
    // remains readable until the replacement stream has been fully consumed.
    document.writeRows(rewrittenRows());
    return value;
  }
  *rows() { for (const row of this.document.readRows()) yield row.map(v => this.toParameter(v)); }
}
class TextMatrixDocument extends MatrixDocument {}
class MathMatrixDocument extends MatrixDocument {
  toParameter(value) { if (value == null || String(value).trim() === "") return Number(this.defaultParameter) || 0; const n = Number(value); return Number.isFinite(n) ? n : (Number(this.defaultParameter) || 0); }
  fromParameter(value) { const n = Number(value); return Number.isFinite(n) ? String(n) : String(this.defaultString); }
}
module.exports = { MatrixDocument, TextMatrixDocument, MathMatrixDocument };
