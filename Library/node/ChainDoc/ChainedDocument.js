"use strict";

const { MemoryConnector } = require("./Connector/ConnectorBase");

function assertPath(path) {
  if (typeof path !== "string" || !path.trim()) throw new TypeError("A non-empty document path/key is required.");
}
function* asIterable(value) {
  if (value == null) return;
  if (typeof value === "string") { yield value; return; }
  yield* value;
}

/**
 * ChainedDocument is a lazy, piece-based text/table document.
 * It deliberately does not cache whole files: row and line iterators stream piece by piece.
 * The connector is injected, so the same core works with Node.js, ClearScript and memory storage.
 */
class ChainedDocument {
  constructor(path, options = {}) {
    assertPath(path);
    this.path = path;
    this.connector = options.connector || new MemoryConnector();
    this.delimiter = options.delimiter ?? "\t";
    this.lineEnding = options.lineEnding ?? "\n";
    this.quote = options.quote ?? '"';
    this.encoding = options.encoding ?? "utf8";
    this.freeze = false;
    this.autoSave = false;
    this.token = null;
    this.attachments = new Map();
    this._back = null;
    this._fore = null;
    this._events = new Map();
    this._cachedRowCount = null;
    this._cachedColumnCount = null;
    this._revision = 0;
  }

  get Path() { return this.path; }
  get HasForePiece() { return this._fore !== null; }
  get HasBackPiece() { return this._back !== null; }
  get ForePiece() { return this._fore; }
  get BackPiece() { return this._back; }
  get FirstPiece() { let p = this; while (p._back) p = p._back; return p; }
  get LastPiece() { let p = this; while (p._fore) p = p._fore; return p; }
  get Length() { let n = 0; for (const _ of this.getChain()) n++; return n; }
  get IsExist() { return !!this.connector.exists(this.path); }
  get IsValid() { return !!this.path && this.IsExist; }
  get Revision() { return this._revision; }

  on(name, handler) {
    if (typeof handler !== "function") throw new TypeError("Event handler must be a function.");
    if (!this._events.has(name)) this._events.set(name, new Set());
    this._events.get(name).add(handler);
    return () => this._events.get(name)?.delete(handler);
  }
  _emit(name, payload) {
    for (const fn of this._events.get(name) || []) fn(this, payload);
  }
  setAttached(key, value) {
    const existed = this.attachments.has(key);
    this.attachments.set(key, value);
    return !existed;
  }
  getAttached(key, fallback = null) { return this.attachments.has(key) ? this.attachments.get(key) : fallback; }

  *getChain() {
    for (let p = this.FirstPiece; p; p = p._fore) yield p;
  }
  *getSequences() {
    for (let p = this._fore; p; p = p._fore) yield p;
  }
  breakSequence() {
    if (this._back) this._back._fore = null;
    if (this._fore) this._fore._back = null;
    this._back = this._fore = null;
    this._changed("BreakSequence");
    return this;
  }
  appendForePiece(piece) {
    if (!(piece instanceof ChainedDocument)) piece = new ChainedDocument(String(piece), { connector: this.connector, delimiter: this.delimiter, lineEnding: this.lineEnding });
    const tail = this.LastPiece;
    const head = piece.FirstPiece;
    if (head === this.FirstPiece || head === tail) throw new Error("Cannot chain a document to itself or create a cycle.");
    if (tail._fore) throw new Error("Internal chain invariant violated.");
    tail._fore = head;
    head._back = tail;
    this._changed("AppendForePiece");
    return this;
  }
  prependBackPiece(piece) {
    if (!(piece instanceof ChainedDocument)) piece = new ChainedDocument(String(piece), { connector: this.connector, delimiter: this.delimiter, lineEnding: this.lineEnding });
    const head = this.FirstPiece, tail = piece.LastPiece;
    if (tail === head || tail === this.LastPiece) throw new Error("Cannot chain a document to itself or create a cycle.");
    head._back = tail;
    tail._fore = head;
    this._changed("PrependBackPiece");
    return this;
  }
  setForePiece(piece) {
    if (this._fore) this._fore._back = null;
    this._fore = null;
    return piece == null ? this : this.appendForePiece(piece);
  }
  setBackPiece(piece) {
    if (this._back) this._back._fore = null;
    this._back = null;
    return piece == null ? this : this.prependBackPiece(piece);
  }

  *_readPieceLines() { yield* this.connector.readLines(this.path); }
  *readLines() { for (const piece of this.getChain()) yield* piece._readPieceLines(); }
  *readRows(options = {}) {
    const delimiter = options.delimiter ?? this.delimiter;
    for (const line of this.readLines()) yield ChainedDocument.parseRow(line, delimiter, this.quote);
  }
  *readCells() { for (const row of this.readRows()) yield* row; }
  readText() { return [...this.readLines()].join(this.lineEnding); }

  writeLines(lines) {
    if (this.freeze) throw new Error("Document is frozen.");
    this.connector.writeLines(this.path, asIterable(lines));
    this._changed("WriteLines");
    return this;
  }
  writeRows(rows) {
    const self = this;
    function* lines() { for (const row of rows) yield ChainedDocument.formatRow(row, self.delimiter, self.quote); }
    return this.writeLines(lines());
  }
  writeText(text) {
    if (this.freeze) throw new Error("Document is frozen.");
    this.connector.writeText(this.path, String(text));
    this._changed("WriteText");
    return this;
  }
  appendLines(lines) {
    if (this.freeze) throw new Error("Document is frozen.");
    this.connector.appendLines(this.path, asIterable(lines));
    this._changed("AppendLines");
    return this;
  }
  appendRows(rows) {
    const self = this;
    function* lines() { for (const row of rows) yield ChainedDocument.formatRow(row, self.delimiter, self.quote); }
    return this.appendLines(lines());
  }

  get RowsCount() {
    if (this._cachedRowCount === null) { let n = 0; for (const _ of this.readLines()) n++; this._cachedRowCount = n; }
    return this._cachedRowCount;
  }
  get ColumnsCount() {
    if (this._cachedColumnCount === null) {
      let max = 0;
      for (const row of this.readRows()) max = Math.max(max, row.length);
      this._cachedColumnCount = max;
    }
    return this._cachedColumnCount;
  }
  readRow(index) {
    if (!Number.isSafeInteger(index) || index < 0) return null;
    let i = 0;
    for (const row of this.readRows()) { if (i++ === index) return row; }
    return null;
  }
  readCell(column, row) {
    const cells = this.readRow(row);
    return cells && column >= 0 ? (cells[column] ?? null) : null;
  }
  findRows(pattern, options = {}) {
    const re = pattern instanceof RegExp ? pattern : new RegExp(String(pattern), options.flags ?? "i");
    const start = Math.max(0, options.fromIndex ?? 0);
    const result = [];
    let i = 0;
    for (const row of this.readRows()) { if (i >= start && row.some(cell => re.test(cell))) result.push({ index: i, row }); i++; }
    return result;
  }
  save() { if (this.connector.flush) this.connector.flush(); this._emit("Saved", null); return this; }
  _changed(action) {
    this._revision++;
    this._cachedRowCount = this._cachedColumnCount = null;
    this._emit("Changed", { action, revision: this._revision });
    if (this.autoSave) this.save();
  }

  static parseRow(line, delimiter = "\t", quote = '"') {
    line = String(line ?? "");
    if (!delimiter) throw new TypeError("Delimiter cannot be empty.");
    const cells = [];
    let cell = "", quoted = false;
    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if (ch === quote) {
        if (quoted && line[i + 1] === quote) { cell += quote; i++; }
        else quoted = !quoted;
      } else if (!quoted && line.startsWith(delimiter, i)) {
        cells.push(cell); cell = ""; i += delimiter.length - 1;
      } else cell += ch;
    }
    if (quoted) throw new SyntaxError("Malformed row: unterminated quoted field.");
    cells.push(cell);
    return cells;
  }
  static formatRow(cells, delimiter = "\t", quote = '"') {
    return [...cells].map(value => {
      const s = value == null ? "" : String(value);
      if (s.includes(quote) || s.includes(delimiter) || /[\r\n]/.test(s)) return quote + s.split(quote).join(quote + quote) + quote;
      return s;
    }).join(delimiter);
  }
}
module.exports = { ChainedDocument };
