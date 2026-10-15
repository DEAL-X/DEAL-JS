"use strict";
const { MathMatrixDocument } = require("../MatrixDocument");
class ForceMathMatrixDocument extends MathMatrixDocument {
  toParameter(value) { const n = Number(value); return Number.isFinite(n) ? n : 0; }
  fromParameter(value) { const n = Number(value); if (!Number.isFinite(n)) throw new TypeError("A finite numeric value is required."); return String(n); }
}
module.exports = { ForceMathMatrixDocument };
