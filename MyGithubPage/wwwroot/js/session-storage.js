window.loginSession = {
    get: key => sessionStorage.getItem(key),
    set: (key, value) => sessionStorage.setItem(key, value),
    remove: key => sessionStorage.removeItem(key)
};
