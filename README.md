# DEAL.JS (Declarative and Extensible Abstract Language for JavaScript) Compiler

DEAL.JS is a deterministic, reusable, human-readable, extensible command language for acquiring, transforming, analyzing, and automating data workflows. A structured intermediate representation (IR) between natural language and executable JavaScript scraping logic. In other words, DEAL is a procedural, English-like domain-specific language that compiles deterministically into JavaScript browser automation code.
* DEAL instructions will parse directly into complex and optimized pure JavaScript code to use everywhere.
* You can write pure JavaScript between your instructions freely.

Start DEAL directly using the tutorial sample [here](SAMPLE.md) quickly...

Also you can find more examples and their parsed JS versions [here](Samples/).


## Main Principles
DEAL has **five** main principles:
1. Every syntax will be the same as english grammar.
    * For example:
    ```sql
    For each url of the list, load the url, then append all h3#title, to the destination;
    otherwise log the error `Could not load the ${url}!`;
    ```
    * The written DEAL code can be read as a simple document.
2. Every instruction will be compiled deterministically into JavaScript.
    * The previous instructions will convert to the below pure JavaScript code directly:
    ```js
    for(const url of list) load(url).then((data) => {
        handle(data);
        append(all("h3#title"), destination);
    }).catch((data) => {
        handle(data);
        log.error(`Could not load the ${url}!`);
    });
    ```
    * There is no hidden behavior.
    * Never runtime guessing.
3. Ability to write pure JavaScript code between DEAL instructions freely.
    * For example:
    ```sql
    for(url of list) load(url).then(() => {
        append(all("h3#title"), destination);
    }) otherwise log the error `Could not load the ${url}!`;
    ```
    * Without any conflict.
4. Everything is extensible except the grammar.
    * Can even change the language of the written code to other local languages (Spanish, Persian, ...), using attach its special libraries, for example:
    ```sql
    use Spanish; // Contains all Spanish alternative reserved words.
    // ...
    Para cada url de la lista, carga la url, luego añade todos h3#title, al destino; 
    de lo contrario, registra el error `¡No se pudo cargar ${url}!`;
    ```
    * Then it will be exactly equal to the below DEAL instructions:
    ```dig
    use Spanish; // Contains all Spanish alternative reserved words.
    // ...
    For each url of the lista, load the url, then append all h3#title, to the destino;
    otherwise log the error `¡No se pudo cargar ${url}!`;
    ```
5. The generated JavaScript is the executable truth.


## Main Syntax
In the DEAL:
* Everything is is built upon five fundamental concepts: DEAL **Structures**, **Commands**, **Reserves**, **Selectors**, and **Handlers**.
    1. **Structures** define the grammar.
    2. **Commands** perform actions.
    3. **Reserves** represent all reserved-words or noise-words defined for DEAL.
    4. **Selectors** locate and identify resources.
    5. **Handlers** of the workspace represent the current execution context.
* Using predefined structures and commands, you will be able to write your procedure more clearly, more human-readable, and more flexibly.
* Case statements of DEAL are very flexible (case-relieve), instead of being somewhere:
    * Case-insensitive:
        * All JS structures, such as IF, FUNCTION, LET, etc.
        * All DEAL or third-party structures, such as BEGIN, USE, RESERVE, etc.
        * All DEAL or third-party commands, such as APPEND, COLLECT, POST, etc.
        * All DEAL or third-party reserveds, such as FROM, AND, WITH etc.
        * All DEAL or third-party selectors, such as ALL, ONE, ANY, etc.
        * All DEAL or third-party handlers, such as BROWSER, WINDOW, LOAD, etc.
            * Multiple of the most important interface variables, such as DOCUMENT, CONSOLE, SCREEN, etc.
            * Multiple of the most important interface functions, such as ALERT, CONFIRM, PROMPT, etc.
    * Case-sensitive:
        * All user-defined functions/variables/constants.
        * All objects' functions/variables/constants (almost everything is written after a dot('.')).

*Noice*: To keep your code more readable and avoid conflicts with DEAL, you should not use `"use strict";` phrase anywhere.
* Because in the strict mode, using a variable without declaring it will not be allowed, then it makes it difficult for the DEAL code writer to use some instructions.

---

### Structures:
Instead of supporting all JS structures, DEAL engine contains some special structures.
* Built into the compiler.
* They define the grammar and cannot be overridden (IF, FOR, COMMAND, USE, RESERVE, WILL, BEGIN, DOING, etc.)
* Multiple of the most important DEAL Structures are:

#### Conditions
* To work in a special conditions
    ```js
    IF condition, procedure; ELSE procedure;
    ```
    It will compile to:
    ```js
    if(js-condition)
        js-procedure;
    else 
        js-procedure;
    ```
* All other of JS conditonal structures (such as switch/case, ternary) are available as the same.

#### Iterations
* Working with a collection based iterations
    ```js
    FOR item OF/IN collection/object, procedure;
    ```
    ```js
    EACH item OF/IN collection/object, procedure;
    ```
    ```js
    FOR EACH item OF/IN collection/object, procedure;
    ```
    All of them will compile to:
    ```js
    for(js-item of js-collection)
        js-procedure;
    ```
    or
    ```js
    for(js-item in js-object)
        js-procedure;
    ```
* Using a conditional iterations
    ```js
    WHILE condition, procedure;
    ```
    It will compile to:
    ```js
    while(js-condition)
        js-procedure;
    ```
* Using a post conditional iterations
    ```js
    DO { procedures } WHILE condition;
    ```
    It will compile to:
    ```js
    do {
        js-procedures
    } while(js-condition);
    ```
    Also you can use a modern approach:
    ```js
    #doingTask procedure; WHILE condition, doingTask;
    ```
    It will compile to:
    ```js
    (doingTask = (() => js-procedure))();
    while(js-condition) doingTask();
    ```

#### Blockings
* To make a DEAL procedures-block
    ```js
    BEGIN procedures END;
    ```
    ```js
    DO procedures END;
    ```
    They will compile to:
    ```js
    {
        js-procedures
    }
    ```
* To make a DEAL callable procedures-block
    ```js
    DOING procedures END;
    ```
    It will compile to:
    ```js
    () => {
        js-procedures
    };
    ```
* To make a tryable block
    ```js
    TRY procedures; CATCH problem, procedure; FINALLY procedure;
    ```
    It will compile to:
    ```js
    try {
        js-procedures
    } catch (problem) {
        js-procedures
    } finally {
        js-procedures
    }
    ```

#### Threadings
* To have a multi-thread program, you can make and manage promises
    ```js
    WILL procedure THEN procedure; OTHERWISE procedure; ANYWAY procedure;
    ```
    It will compile to:
    ```js
    new Promise(()=>js-procedure)
        .then((data)=>js-procedure)
        .catch((data)=>js-procedure)
        .finally((data)=>js-procedure)
    ```

#### Collections
* To filter collections
    ```js
    array WHERE condition
    ```
    It will compile to:
    ```js
    js-array.filter(data=>js-condition)
    ```
* To sort collections
    ```js
    array ORDER BY key1, key2
    ```
    It will compile to:
    ```js
    js-array
        .sort((a, b)=>(a,b)=>a.js-key1>b.js-key1?1:a.js-key1==b.js-key1?0:-1)
        .sort((a, b)=>(a,b)=>a.js-key2>b.js-key2?1:a.js-key2==b.js-key2?0:-1)
    ```
* To slice collections
    ```js
    array LIMIT number, count
    ```
    It will compile to:
    ```js
    js-array.slice(js-number, js-count)
    ```
    ...

---

### Commands:
All commands represent executable operations.
* That are case-insensitive.
* Can be overridden in code.
* A very small set that the compiler itself needs for parsing or semantic analysis defined built in.
* They will include the following cases:
    * All simple DEAL built-in commands.
    * All third-party commands (you add them to your instructions using the USE statement).
    * All user-defined commands (You defined using the COMMAND statement).
* The user-defined command will be accessible exactly like other global commands, too.
* You can define commands in four ways:
    * Action
    * Function
    * Definition
    * Delegation

#### Action Command
* By action command there will make a simple callable, but with these features:
    * All action commands must be defined globally.
    * They will have a special case-insensitive commandName.
    * They will call immediately after definition once.
    * Will be able to call again, just using the name without any parentesis.
* The main syntax to define a special action commands:
    ```js
    #commandName procedure;
    ```
    or
    ```js
    #commandName: procedure;
    ```
    They will compile to:
    ```js
    (commandName = (() => js-procedure))();
    ```
* Then to call:
    ```js
    commandname
    ```
    It will compile to:
    ```js
    commandName()
    ```

#### Function Command
* Every function command behaves like a JavaScript function while following several conventions:
    * All function commands must be defined globally.
    * They will have a special case-insensitive commandName.
    * All inputs of each command should be optional. So ensure every parameter has a default value.
    * If the command has no returned output, it should return itself (specially when there is not inputted arguments). 
* The main syntax to define a special function commands:
    ```js
        COMMAND commandName(input1=value1, ...) {	// All inputs of each command must have a default value
            procedures
            /*
                // Should return one of the following
                return output;
                return commandName; // If you don't have any return value, you should return the command directly (usually when you do not have input arguments).
                // It will be useful when  you want to send a callable version of the command to another one as an input
            */
        }
    ```
    It will compile to:
    ```js
        function commandName(input1=value1, ...) {
            js-procedures
        }
    ```
* Four ways to call a command:
    * Grammatical:			
        ```js
        commandname                            		    // To call without any inputted arguments
        ```
    * Functional:			
        ```js
        commandname(val1_1, CMD2(val2_1, val2_2))		// Exact calling a function
        ```
    * Procedural:			
        ```js
        commandname val1_1, CMD2 val2_1, val2_2	        // Separated by comma.
        ```
    * Optional:			
        ```js
        commandname1 ...{								// Sending an object of named values
            inp1_1: val1_1,
            inp1_2: commandname2 { inp2_1: val2_1, inp2_2: val2_2 } 
        }
        ```
* You can combine the methods above, depending on your code situations.
* All of the ways will convert to a simple function call.

#### Definition Command
* Every definition command behaves like a JavaScript object while following several conventions:
    * All definition commands must be defined globally.
    * They will have a special case-insensitive commandName.
* The main syntax to define a special definition commands:
    ```js
        COMMAND commandName = {
            propertykey1 : propertyValue1,
            propertykey2 : propertyValue2,
            /* Exactly same to an object... */
        };
    ```
    It will compile to:
    ```js
        var commandName = {
            js-propertykey1 : js-propertyValue1,
            js-propertykey2 : js-propertyValue2,
            /* Exactly same to an object... */
        };
    ```
* Then to use:
    ```js
    commandname propertykey1
    ```
    It will compile to:
    ```js
    commandName.js-propertykey1
    ```

#### Delegation Command
* The simplest but valuable type of commands.
* Every delegation command behaves like a simple JavaScript variable while following several conventions:
    * All delegation commands must be defined globally.
    * They will have a special case-insensitive commandName.
    * All words, after being used only one time, a stuck acceptor sign `@` will convert to a delegation command.
    * They can convert everything (constant, variable, function, class, etc.), even after their definition.
    * They will not change the behavior of the converted thing. 
 * The main syntax to make then call a special delegation commands is:
    ```js
        var test1Var = "The test value #1";
        console.log(test1Var); // You should use case-sensitively
        console.log(@test1Var); // You should use case-sensitively (But made it case-insensitive after a sticked acceptor sign `@`)
        console.log(TEST1var); // Its a delegation command and is case-insensitive now
            
        var @test2Var = "The test value #2"; // You can define everything case-insensitive the first time using a sticked acceptor sign `@`
        console.log(Test2VAR); // Its a delegation command and is case-insensitive now
    ```
    It will compile to:
    ```js
        var test1Var = "The test value #1";
        console.log(test1Var);
        console.log(test1Var);
        console.log(test1Var);
            
        var @test2Var = "The test value #2";
        console.log(test2Var);
    ```

---

### Reserves:
DEAL allows optional readability words that improve the natural flow of the language. They will affect only the parser and contain no JavaScript code. These can be one of two groups below:
* Reserved words: All words will be replaced directly in the script while being preserved in the source code.
    * The user can call any one of the codes below freely.
    * The reserved name will be completely case-insensitive.
    * It can improve the readability of the code with a predefined replacement for them.
    * You can add a reserved-word to the parser using the following syntax:
        ```js
        RESERVE reservedWordName AS "replacement";       // This line affects only the parser, and will replace the used reservedWordName by replacement everywhere it used.
        ```
    * For example, you can write any one of the bellow codes, without any effects in meaning:
        ```js
        RESERVE into AS ",";
        // Other procedures
        SAVE "hello world!" INTO "index.html";
        // With the same meaning
        SAVE "hello world!" , "index.html";
        ```
        Or:
        ```js
        RESERVE restart AS "LOAD \"http://mimfa.net\";";
        // Other procedures
        RESTART
        // With the same meaning
        LOAD "http://mimfa.net";
        ```
    * Parser will replace those specified words before parsing by default.
* Noise-words: All words are ignored semantically while being preserved in the source code.
    * The user can call any one of the codes below freely.
    * The reserved name will be completely case-insensitive.
    * It can improve the readability of the code without carrying any semantic meaning on them.
    * You can add a noise word to the parser using the following syntax:
        ```js
        RESERVE noiseWordName;       // This line affects only the parser, not JavaScript.
        ```
    * For example, you can write any one of the bellow codes, without any effects in meaning:
        ```js
        RESERVE the;
        // Other procedures
        CLICK ALL THE #nextbutton
        // With the same meaning
        CLICK ALL #nextbutton
        ```
        Or:
        ```js
        RESERVE a;
        // Other procedures
        LOAD A "www.example.com"
        // With the same meaning
        LOAD "www.example.com"
        ```
    * Parser will skip those specified words before parsing by default.

---

### Selectors:
Selectors describe what should be selected, but not only DOM elements, potentially anything.
* Built into the compiler.
* They define how resources are identified (CSS, XPath, RegEx, Id, Class, Index, Location, ...)
* You have multiple special predefined DEAL commands in core, to extract elements from the resource too:
    ```js
    ALL selector						// To select all elements using the specified selector from the DOCUMENT
    ```
    ```js
    ONE selector						// To select one element using the specified selector from the DOCUMENT
    ```
    ```js
    ANY selector				    	// To select all/one elements using the specified selector from the DOCUMENT
    ```
    ```js
    ALL/ONE/ANY selector FROM parent 	// To select all/one from the parent element/object/array
    ```
    ```js
    resource[selector]				    // To select one item using the specified selector on the resource, the resource can be an element/object/array
    ```
* Selectors effectively makes DEAL a universal extraction language, not just a scripting language, Because you can use one of the DEAL Selectors with the syntax below, everywhere you need:
    * CSS:
        ```js
        table>tbody>tr			    // Write CSS selector directly, But if it has spaces or {} should wrapped by single/double quotes
        ```
    * XPath:
        ```js
        \table\tbody\tr\	        // Using backslash wrapped path to use a XPath route
        ```
    * RegEx:
        ```js
        /(?<=<tr>)(?=<\/tr>)/gi		// You can use a Regular Expression Pattern to filter elements based on their outerHTML
        ```
    * Tag:
        ```js
        tagName					    // Select element/s using type directly the elementName
        ```
    * Id:
        ```js
        #tagId				    // Select an element using type element Id after a #
        ```
    * Class:
        ```js
        .tagClass					// Select element/s using type element class after a dot('.')
        ```
    * Location:
        ```js
        500, 700, 999				// The location of element on the screen(x,y,z) or a dimensional array or object
        ```
    * Index:
        ```js
        12							// The index of the child
        ```

---

### Handlers:
There are multiple defined variables accessible globally, which users can interact with.
* A very small set of them defined bult-in (APPLICATION, BROWSER, WINDOW, TAB, DOCUMENT, RESPONSE, ITS, ...)
* Some of the global variables that will update based on the current status are named Handler Identifiers, including:
    ```js
    BROWSER 			    // The current browser used in this thread
    ```
    ```js
    APPLICATION 		    // The current application (MiMFa Scraper) used in this thread
    ```
    ```js
    WINDOW/TAB 		        // The current window/tab used in this thread
    ```
    ```js
    DOCUMENT 		        // The current loaded document in the window used in this thread
    ```
    ```js
    RESPONSE			    // The latest response received in this thread
    ```
    ```js
    LOADED				    // The current document used in this thread was loaded successfully or not
    ```
    ```js
    FETCHED			        // The current response used in this thread was received successfully or not
    ```
    ```js
    DATA				    // The root of iterated objects
    ```
    ```js
    ITS				        // The current object or class will accessible through this one
    ```
* Multiple structures will update in the current thread, based on the two commands FETCH/LOAD status.
    * FETCH: To send a request to a server and receive its response, and update the handler identifiers based on that, using the syntax below:
        ```js
        FETCH url, data, method		// To fetch data from a specified URL, then update the handler identifiers
        ```
    * LOAD: To open a website in the current window, and update the handler identifiers based on that, using the syntax below:
        ```js
        LOAD url		// To load a website from a specified URL in the current window/tab, and then update the handler
        ```
        ```js
        LOAD NEXT		// To load a website from a specified URL in the current window/tab, and then update the handler
        ```
        ```js
        LOAD BACK		// To load a website from a specified URL in the current window/tab, and then update the handler
        ```
        ```js
        LOAD			// To update the Workspace handlers based on the current window/tab statements
        ```
* If you want to work in your browser, window, or so on, without changing the current handler structures, use the following commands:
    * GET: To send a GET request to a server and receive its response, using the syntax below:
        ```js
        GET url, data		// To fetch data from a specified URL
        ```
    * POST: To send a POST request to a server and receive its response, using the syntax below:
        ```js
        POST url, data		// To fetch data from a specified URL
        ```
    * GO: To send a request to a server and receive its response, without changing the current handler identifiers, using the syntax below:
        ```js
        GO url			// To load a website from a specified URL in a new window/tab
        ```
        ```js
        GO NEXT		    // To open the next window/tab, if it exists
        ```
        ```js
        GO BACK		    // To open the previous window/tab, if it exists 
        ```