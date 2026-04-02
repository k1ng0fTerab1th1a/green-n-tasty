const KEYS = {
    idToken: "idToken",
    accessToken: "accessToken",
    refreshToken: "refreshToken",
    username: "username",
    role: "role",
    email: "email",
};

export const tokenStorage = {
    setSession({ idToken, accessToken, refreshToken, username, role, email }) {
        localStorage.setItem(KEYS.idToken, idToken);
        localStorage.setItem(KEYS.accessToken, accessToken);
        localStorage.setItem(KEYS.refreshToken, refreshToken);
        localStorage.setItem(KEYS.username, username || "");
        localStorage.setItem(KEYS.role, role || "");
        localStorage.setItem(KEYS.email, email || "");
        localStorage.setItem("token", idToken);
    },

    getSession() {
        return {
            idToken: localStorage.getItem(KEYS.idToken) || localStorage.getItem("token"),
            accessToken: localStorage.getItem(KEYS.accessToken),
            refreshToken: localStorage.getItem(KEYS.refreshToken),
            username: localStorage.getItem(KEYS.username),
            role: localStorage.getItem(KEYS.role),
            email: localStorage.getItem(KEYS.email) || "",
        };
    },

    clear() {
        localStorage.removeItem(KEYS.idToken);
        localStorage.removeItem(KEYS.accessToken);
        localStorage.removeItem(KEYS.refreshToken);
        localStorage.removeItem(KEYS.username);
        localStorage.removeItem(KEYS.role);
        localStorage.removeItem(KEYS.email);
        localStorage.removeItem("token");
    },
};