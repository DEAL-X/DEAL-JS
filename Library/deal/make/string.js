Make.string = function(object = null) {
	const t = typeof object;
	switch(t)
	{
		case "string":
			return object;
			
		case "array":
		case "object":
			return JSON.stringify(object);
		
		default:
			if(object instanceof HTMLInputElement) 
				return object.value;
			if(object instanceof HTMLImageElement) 
				return object.src;
			if(object instanceof HTMLSourceElement) 
				return object.src;
			if(object instanceof HTMLIFrameElement) 
				return object.src;
			if(object instanceof HTMLScriptElement) 
				return object.src ?? object.textContent;
			if(object instanceof HTMLStyleElement) 
				return object.textContent;
			if(object instanceof HTMLProgressElement) 
				return object.value;
			if(object instanceof HTMLElement) 
				return object.innerText;
			return String(object);
	}
};