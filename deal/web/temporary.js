class Temporary {
    static keep(key, value, expires = 0, path = "/", $secure = false) {
        time = "";
        if (expires) {
            var date = new Date();
            date.setTime(date.getTime() + expires);
            time = "; expires=" + date.toUTCString();
        }
        if ($secure) $secure = "; Secure";
        else $secure = "";
        document.cookie = `${encodeURIComponent(key)}=${encodeURIComponent(value || "")}${time}${$secure}; path=${path}`;
    };
    static give(key) {
        let nameEQ = encodeURIComponent(key) + "=";
        let ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return decodeURIComponent(c.substring(nameEQ.length, c.length));
        }
        return null;
    };
    static take(key, path = "/") {
        document.cookie = `${encodeURIComponent(key)}=; Max-Age=-99999999; Secure; path=${path}`;
    };
    static remove() {
        let cookies = document.cookie.split(";");
        for (let i = 0; i < cookies.length; i++) {
            let cookie = cookies[i];
            let eqPos = cookie.indexOf("=");
            let key = eqPos > -1 ? cookie.substring(0, eqPos) : cookie;
            document.cookie = key + "=; Max-Age=-99999999; Secure; path=/";
        }
    };
}