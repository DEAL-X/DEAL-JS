//Html Tag Creator Function
command STRUCT = function (content = null, tagName = "div", attributes = {}) {
    let isSingle = typeof tagName === 'object';
    if (isSingle) {
        attributes = tagName;
        tagName = content;
    }
    tagName = tagName.toLowerCase();

    let attrs = "";
    for (let x in attributes) {
        let val = attributes[x] + "";
        let sp = val.includes("'") ? '"' : "'";
        attrs += [" ", x.toLowerCase(), "=", sp, val, sp].join("");
    }

    if (isSingle) return ["<", tagName, attrs, "/>"].join("");
    else return ["<", tagName, attrs, ">", Array.isArray(content) ? content.join("") : isArray(content) ? each(content).join("") : content, "</", tagName, ">"].join("");
}

STRUCT.stylePath = "";
STRUCT.stylePresetPath = "";
STRUCT.styleResetPath = "";
STRUCT.scriptPath = "";
STRUCT.maxFloatDecimals = 2;
STRUCT.maxValueLength = 10;
STRUCT.usedHeadItems = [];
STRUCT.charset = "UTF-8";
STRUCT.newLine = "<br/>";

STRUCT.toText = (obj) => textConvertor.Done(obj);
STRUCT.tryToText = (obj) => textConvertor.TryDone(obj);
STRUCT.subject = (content, attributes = {}) => Struct(content, "Title", attributes);
STRUCT.source = (source, attributes = {}) => {
    const srci = Struct("", "script", { ...{ SRC: source }, ...attributes });
    STRUCT.usedHeadItems.push(srci);
    return srci;
}

STRUCT.script = (content, source = null, attributes = {}) => {
    const srci = Struct(content, "script", source === null ? attributes : { ...{ SRC: source }, ...attributes });
    STRUCT.usedHeadItems.push(srci);
    return srci;
}
STRUCT.script.async = (content, source = null, attributes = {}, callback = null) => {
    var script = document.createElement('script');
    script.src = source;
    script.innerHTML = content;
    script.async = true;
    for (const item in attributes) script.setAttribute(item, attributes[item]);
    if (callback) {
        script.onload = (ev) => callback(ev, null);
        script.onerror = (ev, src, ln, n, e) => callback(ev, e, src, ln, n);
    }
    document.head.appendChild(script);
}
STRUCT.script.load = (content, source = null, attributes = {}) => {
    var script = document.createElement('script');
    script.src = source;
    script.innerHTML = content;
    for (const item in attributes) script.setAttribute(item, attributes[item]);
    document.head.appendChild(script);
}
STRUCT.script.code = (value = null, attributes = {}) => {
    STRUCT.script(value == null ? "" : value + ";", null, attributes);
}
STRUCT.script.var = (name, value = null, attributes = {}) => {
    STRUCT.script.code((name == null ? "" : "var " + name + (value == null ? "" : " = ")) + value, attributes);
}
STRUCT.script.let = (name, value = null, attributes = {}) => {
    STRUCT.script.code((name == null ? "" : "let " + name + (value == null ? "" : " = ")) + value, attributes);
}
STRUCT.script.const = (name, value = null, attributes = {}) => {
    STRUCT.script.code((name == null ? "" : "const " + name + (value == null ? "" : " = ")) + value, attributes);
}

STRUCT.style = (content, source = null, attributes = {}) => {
    if (source === null)
        return Struct(content, "style", attributes);
    const srci = Struct("Link", { ...{ REL: "stylesheet", HREF: source ?? content }, ...attributes });
    STRUCT.usedHeadItems.push(srci);
    return srci;
}
STRUCT.style.load = (content, source = null, attributes = {}) => {
    if (source) {
        var style = document.createElement('link');
        style.rel = 'stylesheet';
        style.href = source;
        for (const item in attributes) style.setAttribute(item, attributes[item]);
        document.head.appendChild(style);
    }
    if (content) {
        var style = document.createElement('style');
        style.innerHTML = content;
        for (const item in attributes) style.setAttribute(item, attributes[item]);
        document.head.appendChild(style);
    }
}
STRUCT.style.code = (value = null, attributes = {}) => {
    STRUCT.style(value == null ? "" : value + "", null, attributes);
}
STRUCT.style.on = (selector, value = null, attributes = {}) => {
    STRUCT.style.code((selector == null ? "*" : selector) + "{" + (value == null ? "" : value) + "}", attributes);
}

STRUCT.pages = (title, content = null, description = null, attributes = {}, head = "", styles = [], sources = []) => {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    if (!isArray(content)) return STRUCT.page(title, content, description, attributes, head, styles, sources);
    const c = count(content);
    if (c == 0) return STRUCT.page(title, null, description, attributes, head, styles, sources);
    if (c == 1) return STRUCT.page(title, content[0], description, attributes, head, styles, sources);

    const p = Math.slice(c, 100, 100, 20, 33);
    let ls = [];
    for (let item of content) {
        ls.push([`<iframe src="" srcdoc="`,
            (item + "").replace("\"", "&quot;"),
            `" class="embed" style="width:`, p.x, `vw;height:`, p.y,
            `vh;" marginwidth="0" marginheight="0" align="top" frameborder="0" hspace="0" vspace="0"></iframe>`].join(""));
    }
    return STRUCT.page(title, ls.join("\r\n\r\n"), description, attributes, head, styles, sources);
}
STRUCT.page = (title, content = null, description = null, attributes = {}, head = "", styles = [], sources = []) => {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    let body = Struct(
        Struct(
            [
                isNull(title) ? "" : STRUCT.superTitle(title),
                isArray(content) ? (each(content, (item, i) => STRUCT.part(null, item))).join("\r\n") : content,
                isNull(title) ? "" : STRUCT.caption(description)
            ].join("\r\n"),
            "div",
            { ...{ CLASS: "page" }, ...attributes }
        ),
        "body",
        { LANG: System.Lang, CHARSET: System.CharSet }
    );

    if (STRUCT.usedHeadItems.length > 0) {
        head = Array.distinct(STRUCT.usedHeadItems).join("\r\n") + "\r\n" + head + "\r\n";
        for (const x of STRUCT.usedHeadItems)
            body = body.replace(x, "");
        STRUCT.reset();
    }

    return "<!DOCTYPE HTML>" +
        Struct(
            [
                Struct(
                    [
                        STRUCT.subject(title),
                        Struct("meta", { CHARSET: STRUCT.charset }),
                        Struct("Link", { HREF: STRUCT.styleResetPath, REL: "stylesheet" }),
                        Struct("Link", { HREF: STRUCT.stylePresetPath, REL: "stylesheet" }),
                        Struct("Link", { HREF: STRUCT.stylePath, REL: "stylesheet" }),
                        Struct("", "script", { SRC: STRUCT.scriptPath }),
                        styles.length > 0 ?
                            each(styles, (item, i) => STRUCT.style("", item)).join("") :
                            "",
                        sources.length > 0 ?
                            each(sources, (item, i) => STRUCT.source(item)).join("") :
                            "",
                        head === null ? "" : "\r\n" + head
                    ].join("\r\n"),
                    "head"
                ),
                body
            ].join("\r\n"),
            "html"
        );
}
STRUCT.part = (title, content = null, description = null, attributes = {}) => {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    return Struct(
        [
            (isHollow(title) ? "" : STRUCT.title(title)),
            isArray(content) ? (each(content, (item, i) => STRUCT.panel(null, item))).join("\r\n") : content,
            (isHollow(description) ? "" : STRUCT.caption(description))
        ].join("\r\n"),
        "div",
        { ...{ CLASS: "part" }, ...attributes }
    );
}
STRUCT.panel = (title, content = null, description = null, attributes = {}) => {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    return Struct(
        [
            (isHollow(title) ? "" : STRUCT.title(title)),
            isArray(content) ? (each(content, (item, i) => STRUCT.box(null, item))).join("\r\n") : content,
            (isHollow(description) ? "" : STRUCT.caption(description))
        ].join("\r\n"),
        "div",
        { ...{ CLASS: "panel" }, ...attributes }
    );
}
STRUCT.box = (title, content = null, description = null, attributes = {}) => {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    return Struct(
        [
            (isHollow(title) ? "" : STRUCT.title(title)),
            isArray(content) ? (each(content, (item, i) => STRUCT.element(item))).join("\r\n") : content,
            (isHollow(description) ? "" : STRUCT.caption(description))
        ].join("\r\n"),
        "div",
        { ...{ CLASS: "box" }, ...attributes }
    );
}
STRUCT.element = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "element" }, ...attributes });

STRUCT.superTitle = (content, attributes = {}) => Struct(content, "h1", { ...{ CLASS: "supertitle" }, ...attributes });
STRUCT.title = (content, attributes = {}) => Struct(content, "h2", { ...{ CLASS: "Title" }, ...attributes });
STRUCT.subTitle = (content, attributes = {}) => Struct(content, "h3", { ...{ CLASS: "SubTitle" }, ...attributes });
STRUCT.caption = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "caption" }, ...attributes });
STRUCT.footNote = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "footnote" }, ...attributes });
STRUCT.context = (content, attributes = {}) => Struct(content, "p", { ...{ CLASS: "context" }, ...attributes });
STRUCT.label = (content, attributes = {}) => Struct(content, "span", { ...{ CLASS: "label" }, ...attributes });
STRUCT.bold = (content, attributes = {}) => Struct(content, "b", { ...{ CLASS: "bold" }, ...attributes });
STRUCT.italic = (content, attributes = {}) => Struct(content, "i", { ...{ CLASS: "italic" }, ...attributes });
STRUCT.big = (content, attributes = {}) => Struct(content, "span", { ...{ CLASS: "big" }, ...attributes });
STRUCT.small = (content, attributes = {}) => Struct(content, "span", { ...{ CLASS: "small" }, ...attributes });
STRUCT.result = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "result" }, ...attributes });
STRUCT.message = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "result message", ondblclick: "this.style.display = 'none'" }, ...attributes });
STRUCT.success = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "result success", ondblclick: "this.style.display = 'none'" }, ...attributes });
STRUCT.error = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "result error", ondblclick: "this.style.display = 'none'" }, ...attributes });
STRUCT.warning = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "result warning", ondblclick: "this.style.display = 'none'" }, ...attributes });

STRUCT.division = (content, attributes = {}) => Struct(content, "div", { ...{ CLASS: "division" }, ...attributes });

STRUCT.link = (link, anchor = null, attributes = {}) => Struct(anchor ?? link, "a", { ...{ HREF: link, CLASS: "Link" }, ...attributes });

STRUCT.image = (source, alt = null, attributes = {}) => Struct("img", { ...{ SRC: source, ALT: alt, CLASS: "Image" }, ...attributes });

STRUCT.center = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => STRUCT.center(item, attributes)) : content, "div", { ...{ CLASS: "center" }, ...attributes });

STRUCT.table = function (title, content = null, description = null, attributes = {}, colHeads = [0], rowHeads = []) {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    let isen = isArray(content);
    let arr = isen ? each(content) : content;
    if (!isen) {
        colHeads = [];
        rowHeads = [];
    }
    return STRUCT.box(
        title,
        Struct(
            STRUCT.rows(arr, {}, colHeads, rowHeads),
            "table",
            { ...{ BORDER: "1", CLASS: "table" }, ...attributes }
        ),
        description,
        {}
    );
}
STRUCT.tables = (content, attributes = {}) =>
    each(content, (item, i) => STRUCT.table(item, attributes)).join("");
STRUCT.col = (content, attributes = {}) =>
    Struct(isArray(content) ? STRUCT.headers(content) : content, "tr", { ...{ CLASS: "col" }, ...attributes });
STRUCT.cols = (content, attributes = {}) =>
    each(content, (item, i) => STRUCT.col(item, attributes)).join("\r\n");
STRUCT.row = (content, attributes = {}, cellHeads = []) =>
    Struct(isArray(content) ? STRUCT.cells(content, {}, cellHeads) : content, "tr", { ...{ CLASS: "row" }, ...attributes });
STRUCT.rows = function (content, attributes = {}, colHeads = [], cellHeads = []) {
    if (count(colHeads) > 0)
        return each(content, (item, i) => colHeads.includes(i) ? STRUCT.col(item, attributes) : STRUCT.row(item, attributes, cellHeads)).join("\r\n");
    else return each(content, (item, i) => STRUCT.row(item, attributes, cellHeads)).join("\r\n");
}
STRUCT.header = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => item).join(STRUCT.newLine) : content, "th", { ...{ CLASS: "header" }, ...attributes });
STRUCT.headers = (content, attributes = {}) =>
    each(content, (item, i) => STRUCT.header(item, attributes)).join("");
STRUCT.cell = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => item).join(STRUCT.newLine) : content, "td", { ...{ CLASS: "cell" }, ...attributes });
STRUCT.cells = function (content, attributes = {}, cellHeads = []) {
    if (count(cellHeads) > 0)
        return each(content, (item, i) => cellHeads.includes(i) ? STRUCT.header(item, attributes) : STRUCT.cell(item, attributes)).join("");
    else return each(content, (item, i) => STRUCT.cell(item, attributes)).join("");
}

STRUCT.value = (content, attributes = {}) => {
    if (isFloat(content)) return STRUCT.abbr(content, Struct(+parseFloat(content).toFixed(STRUCT.maxFloatDecimals), "span", { ...{ CLASS: "Value" }, ...attributes }));
    if (isText(content) && content.length > STRUCT.maxValueLength) return STRUCT.abbr(content.substring(STRUCT.maxValueLength), Struct(content.substring(0, STRUCT.maxValueLength) + "⋯", "span", { ...{ CLASS: "Value" }, ...attributes }));
    return Struct(content, "span", { ...{ CLASS: "Value" }, ...attributes });
}
STRUCT.values = (content, attributes = {}) =>
    isArray(content) ? each(content, (item, i) => STRUCT.value(item, attributes)).join(", ") : STRUCT.value(content, attributes);

STRUCT.color = (content, color = "blue", attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => item).join(", ") : content, "span", { ...{ CLASS: "color", STYLE: "color:" + color }, ...attributes });
STRUCT.abbr = (title, content = "", attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => item).join(", ") : content, "abbr", { ...{ CLASS: "abbr", TITLE: title }, ...attributes });
STRUCT.between = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => item).join("-") : content, "span", { ...{ CLASS: "between" }, ...attributes });
STRUCT.pair = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => item).join(":") : content, "span", { ...{ CLASS: "pair" }, ...attributes });
STRUCT.list = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => STRUCT.items(item)).join("\r\n") : content, "ol", { ...{ CLASS: "list" }, ...attributes });
STRUCT.collection = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => STRUCT.items(item)).join("\r\n") : content, "ul", { ...{ CLASS: "collection" }, ...attributes });
STRUCT.item = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => STRUCT.values(item)).join(STRUCT.newLine) : content, "li", { ...{ CLASS: "Item" }, ...attributes });
STRUCT.items = (content, attributes = {}) =>
    Struct(isArray(content) ? each(content, (item, i) => STRUCT.item(item)).join("\r\n") : content, "li", { ...{ CLASS: "Item" }, ...attributes });
STRUCT.usedChartNumber = 0;
STRUCT.chart = function (title, content = null, description = null, attributes = {}, type = "column", axisXTitle = "X", axisYTitle = "Y", options = null) {
    if (content === null) {
        content = title;
        title = null;
    }
    if (content === null) return null;
    let isen = isArray(content);
    let isobj = isObject(content);
    let datachart = null;
    const id = "Chart" + STRUCT.usedChartNumber++;
    if (!isen && !isobj) datachart = content;
    else {
        if (!isen && isobj) {
            let rows = between(content["matrix"], content["table"], content["rows"], content["columns"]);
            if (isHollow(rows))
                rows = between(content["Source"], content["document"], document).Rows.Skip(between(content["Source"], document).RowsLabelsIndex);
            let arr = [];
            let l = between(content["labels"], content["label"], -1);
            let xs = between(content["axisX"], content["xs"], content["x"], []);
            let ys = between(content["axisY"], content["ys"], content["y"], []);
            let ct = 0;
            if (l > -1)
                if (xs.length > 0)
                    if (ys.length > 0)
                        for (const row of rows)
                            arr.push([
                                row.ElementAt(l),
                                xs.length == 1 ? forceParseFloat(row.ElementAt(xs[0])) : each(xs, i => forceParseFloat(row.ElementAt(i))),
                                ys.length == 1 ? forceParseFloat(row.ElementAt(ys[0])) : each(ys, i => forceParseFloat(row.ElementAt(i)))
                            ]);
                    else
                        for (const row of rows)
                            arr.push([
                                row.ElementAt(l),
                                xs.length == 1 ? forceParseFloat(row.ElementAt(xs[0])) : each(xs, i => forceParseFloat(row.ElementAt(i))),
                                ct++
                            ]);
                else if (ys.length > 0)
                    for (const row of rows)
                        arr.push([
                            row.ElementAt(l),
                            ct++,
                            ys.length == 1 ? forceParseFloat(row.ElementAt(ys[0])) : each(ys, i => forceParseFloat(row.ElementAt(i)))
                        ]);
                else
                    for (const row of rows)
                        arr.push([
                            forceParseFloat(row.ElementAt(l))
                        ]);
            else if (xs.length > 0)
                if (ys.length > 0)
                    for (const row of rows)
                        arr.push([
                            xs.length == 1 ? forceParseFloat(row.ElementAt(xs[0])) : each(xs, i => forceParseFloat(row.ElementAt(i))),
                            ys.length == 1 ? forceParseFloat(row.ElementAt(ys[0])) : each(ys, i => forceParseFloat(row.ElementAt(i)))
                        ]);
                else
                    for (const row of rows)
                        arr.push([
                            xs.length == 1 ? forceParseFloat(row.ElementAt(xs[0])) : each(xs, i => forceParseFloat(row.ElementAt(i))),
                            ct++
                        ]);
            else if (ys.length > 0)
                for (const row of rows)
                    arr.push([
                        ct++,
                        ys.length == 1 ? forceParseFloat(row.ElementAt(ys[0])) : each(ys, i => forceParseFloat(row.ElementAt(i)))
                    ]);
            content = arr;
        }
        let lci = -1, xci = -1, yci = -1;
        let arr = new Array();
        const isten = isArray(type);
        if (isen && isArray(content, 3))
            arr = each(content, (cnt, i) => ["{", `Type: "`, isten ? type[i] : type, `"`, `, dataPoints: [`, STRUCT.points(cnt), `]}`].join(""));
        else if (isten)
            arr = each(type, (ty, i) => ["{", ty.includes(":") ? ty : `Type: "` + ty + `"`, `, dataPoints: [`, STRUCT.points(isen ? content[i] : content), `]}`].join(""));
        else arr.push([`{type: "`, type, `", dataPoints: [`, STRUCT.points(content), `]}`].join(""));

        datachart = "[" + arr.join(",") + "]";
    }

    return STRUCT.box(
        title,
        [
            STRUCT.source("https://cdnjs.cloudflare.com/ajax/libs/canvasjs/1.7.0/CanvasJS.min.js"),
            STRUCT.script([
                `
                window.addEventListener("load", function() 
                    {
                        var chart = new CanvasJS.Chart("`, id, `", {
							zoomEnabled: true,
							legend: {
								horizontalAlign: "center",
								verticalAlign: "top",
		                        fontFamily: "defaultFont"
							},
                            axisX:{
                                title: "`, axisXTitle, `",
                                crosshair: { 
                                    enabled: true
                                },
		                        labelTextAlign: "`, System.HorizontalAlign, `",
		                        labelFontFamily: "defaultFont",
		                        titleFontFamily: "defaultFont"
                            },
                            axisY:{
                                title: "`, axisYTitle, `",
                                crosshair: { 
                                    enabled: true
                                },
		                        labelTextAlign: "`, System.HorizontalAlign, `",
		                        labelFontFamily: "defaultFont",
		                        titleFontFamily: "defaultFont"
                            },
                            toolTip: {
                                shared: true,
		                        fontFamily: "defaultFont"
                            },
                            /*
                            title:{
                                text: "`, title, `",
                                verticalAlign: "top",
                                horizontalAlign: "center"
                            },
                            subtitles:{
                                text: "`, description, `",
                                verticalAlign: "bottom",
                                horizontalAlign: "center"
                            },
                            */
                            data: `, datachart, `
                            `, (options == null ? "" : "," + options), `
                        });
                        chart.render();
                    });`
            ].join(""), null, { TYPE: "text/javascript" }),
            STRUCT.center(Struct("", "div", { Id: id, CLASS: "chart" }))
        ].join("\r\n"),
        description,
        { ...{}, ...attributes }
    );
}

STRUCT.point = (row, index = 0) => {
    const cc = count(row);
    if (cc > 3) {
        var res = [];
        res.push(`{ `);
        res.push(`label: `, STRUCT.parameters(row[0]));
        res.push(`,x:`, STRUCT.numbers(row[1]));
        res.push(`,y:`, STRUCT.numbers(row[2]));
        let len = count(row);
        for (let i = 3; i < len; i++) res.push("," + row[i]);
        res.push(` }`);
        return res.join("");
    }
    else if (cc > 2)
        return [
            `{ label: `, STRUCT.parameters(row[0]),
            `,x:`, STRUCT.numbers(row[1]),
            `,y:`, STRUCT.numbers(row[2]),
            `}`
        ].join("");
    else if (cc > 1)
        return [
            `{x:`, STRUCT.numbers(row[0]),
            `,y:`, STRUCT.numbers(row[1]),
            `}`
        ].join("");
    else if (cc > 0) {
        return [
            `{x:`,
            index,
            `,y:`, STRUCT.numbers(row),
            `}`
        ].join("");
    }
    else return null;
}

STRUCT.points = (content) => {
    return each(content, (row, i) => STRUCT.point(row, i)).join(",");
}

STRUCT.numbers = (arr) => {
    const isarr = isArray(arr);
    const val = isarr ? (Array.isArray(arr) ? arr : each(arr)).join(", ").replace(/^\s*,\s+/gm, "0, ").replace(/,\s*$/gm, ", 0").replace(/,\s+,/gm, ", 0,") : arr;
    return isHollow(val) ? "0" : isarr ? "[" + val + "]" : val;
}

STRUCT.parameters = (arr) => {
    return isHollow(arr) ? "" :
        isArray(arr) ? ["[", each(arr, o => STRUCT.parameters(o)).join(", "), "]"].join("") :
            isString(arr) ? ['"', arr, '"'].join("") :
                isObject(arr) ? JSON.stringify(arr) :
                    arr + "";
}

STRUCT.handle = (data, selector = "body :nth-child(1)", reload = false) => {
    if (data.includes('result')) {
        _(selector + ` .result`).remove();
        _(selector).prepend(data);
        if (reload) load();
    }
    else {
        _(selector).append(data);
    }
}

STRUCT.reset = () => {
    STRUCT.usedHeadItems = [];
    STRUCT.usedChartNumber = 0;
}
