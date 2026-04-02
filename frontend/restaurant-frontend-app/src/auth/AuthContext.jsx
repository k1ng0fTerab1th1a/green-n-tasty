import { createContext, useContext, useEffect, useMemo, useState, useCallback } from "react";
import { signOut as signOutRequest, refreshTokens } from "../services/auth";
import { tokenStorage } from "../services/tokenStorage";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const [auth, setAuth] = useState({
        isAuth: false,
        username: "",
        role: "",
        email: "",
    });

    const updateUserProfile = useCallback(({ firstName, lastName, email }) => {
        setAuth(prev => ({
            ...prev,
            username: `${firstName} ${lastName}`,
            email: email || prev.email
        }));

        const session = tokenStorage.getSession();
        tokenStorage.setSession({
            ...session,
            username: `${firstName} ${lastName}`,
            email: email || session.email
        });
    }, []);

    const refresh = useCallback(async () => {
        const { refreshToken } = tokenStorage.getSession();
        if (!refreshToken) return;

        try {
            const res = await refreshTokens(refreshToken);
            const { idToken, accessToken, refreshToken: newRefreshToken, username, role, email } = res.data.data;

            tokenStorage.setSession({
                idToken,
                accessToken,
                refreshToken: newRefreshToken,
                username,
                role,
                email
            });

            setAuth({
                isAuth: true,
                username: username || "",
                role: role || "",
                email: email || "",
            });
            return idToken;
        } catch (err) {
            console.error("Silent refresh failed:", err);
            tokenStorage.clear();
            setAuth({ isAuth: false, username: "", role: "", email: "" });
        }
    }, [])

    useEffect(() => {
        if (!auth.isAuth) return;

        const { idToken } = tokenStorage.getSession();
        const remainingTime = getTokenRemainingTime(idToken);
        const refreshDelay = Math.max(remainingTime - 60000, 0);

        const timeoutId = setTimeout(() => {
            refresh();
        }, refreshDelay);

        return () => clearTimeout(timeoutId);
    }, [auth, refresh]);

    useEffect(() => {
        const session = tokenStorage.getSession();

        if (session.idToken) {
            const remaining = getTokenRemainingTime(session.idToken);

            if (remaining <= 0) {
                refresh();
            } else {
                setAuth({
                    isAuth: true,
                    username: session.username || "",
                    role: session.role || "",
                    email: session.email || "",
                });
            }
        }
    }, [refresh]);

    const value = useMemo(() => ({
        auth,
        signInSuccess: ({ idToken, accessToken, refreshToken, username, role, email }) => {
            tokenStorage.setSession({ idToken, accessToken, refreshToken, username, role, email });
            setAuth({ isAuth: true, username: username || "", role: role || "", email: email || "" });
        },
        updateUserProfile,
        signOut: async () => {
            const { refreshToken } = tokenStorage.getSession();
            try {
                if (refreshToken) await signOutRequest(refreshToken);
            } catch (err) {
                console.error("Sign out request failed:", err);
            } finally {
                tokenStorage.clear();
                setAuth({ isAuth: false, username: "", role: "", email: "" });
            }
        },
    }), [auth]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (!context) throw new Error("useAuth must be used within AuthProvider");
    return context;
}

const getTokenRemainingTime = (token) => {
    if (!token) return 0;
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const exp = payload.exp * 1000;
        return exp - Date.now();
    } catch (e) {
        return 0;
    }
};