import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { signOut as signOutRequest } from "../services/auth";
import { tokenStorage } from "../services/tokenStorage";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const [auth, setAuth] = useState({
        isAuth: false,
        username: "",
        role: "",
    });

    useEffect(() => {
        const session = tokenStorage.getSession();

        if (session.idToken) {
            setAuth({
                isAuth: true,
                username: session.username || "",
                role: session.role || "",
            });
        }
    }, []);

    const value = useMemo(() => ({
        auth,

        signInSuccess: ({ idToken, refreshToken, username, role }) => {
            tokenStorage.setSession({ idToken, refreshToken, username, role });

            setAuth({
                isAuth: true,
                username: username || "",
                role: role || "",
            });
        },

        signOut: async () => {
            const { refreshToken } = tokenStorage.getSession();

            try {
                if (refreshToken) {
                    await signOutRequest(refreshToken);
                }
            } catch (err) {
                console.error("Sign out request failed:", err);
            } finally {
                tokenStorage.clear();
                setAuth({
                    isAuth: false,
                    username: "",
                    role: "",
                });
            }
        },
    }), [auth]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
    const context = useContext(AuthContext);

    if (!context) {
        throw new Error("useAuth must be used within AuthProvider");
    }

    return context;
}