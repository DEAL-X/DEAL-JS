use .\local;
use .\resource;

reserve reverse as			".reverse";
reserve sort as				".sort";
reserve join as				".join";
reserve concat as			".concat";
reserve flat as				".flat";
reserve fill as				".fill";
reserve at as				".at";

reserve desc as				",.reverse";
reserve asc as				",.sort";
reserve descending as		",.reverse";
reserve ascending as		",.sort";


// To select inputted graph properties from the source
command select(graph = null, source = null) {
    if (!graph) return select;
    if (graph === "*") return source.map();
}

command collect() {
    if (arguments.length <= 0) return collect;
    return arguments;
}

