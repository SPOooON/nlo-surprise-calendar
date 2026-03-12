window.nloIdentityStorage = {
    get: function () {
        return window.localStorage.getItem("nlo-surprise-calendar.participant-id");
    },
    set: function (value) {
        window.localStorage.setItem("nlo-surprise-calendar.participant-id", value);
    },
    clear: function () {
        window.localStorage.removeItem("nlo-surprise-calendar.participant-id");
    }
};
