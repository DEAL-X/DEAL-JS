reserve it as							"()";
reserve #its:identifier as				"(typeof argument == 'undefined' ? data??this : argument??data??this)";

reserve empty as						"''";

command time() => (new Date()).getTime();