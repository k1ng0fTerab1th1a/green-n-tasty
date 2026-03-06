const KEYS = {
    idToken: "idToken",
    refreshToken: "refreshToken",
    username: "username",
    role: "role",
};

export const tokenStorage = {
    setSession({ idToken, refreshToken, username, role }) {
        localStorage.setItem(KEYS.idToken, idToken);
        localStorage.setItem(KEYS.refreshToken, refreshToken);
        localStorage.setItem(KEYS.username, username || "");
        localStorage.setItem(KEYS.role, role || "");
        localStorage.setItem("token", idToken);
    },

    getSession() {
        return {
            idToken: localStorage.getItem(KEYS.idToken) || localStorage.getItem("token"),
            refreshToken: localStorage.getItem(KEYS.refreshToken),
            username: localStorage.getItem(KEYS.username),
            role: localStorage.getItem(KEYS.role),
        };
    },

    clear() {
        localStorage.removeItem(KEYS.idToken);
        localStorage.removeItem(KEYS.refreshToken);
        localStorage.removeItem(KEYS.username);
        localStorage.removeItem(KEYS.role);
        localStorage.removeItem("token");
    },
};