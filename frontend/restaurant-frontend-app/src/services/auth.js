import { api } from "./api";


export async function signUp(payload) {
    const res = await api.post("/auth/sign-up", payload);
    return res.data;
}

export async function signIn(payload) {
    const res = await api.post("/auth/sign-in", payload);
    return res.data;
}

export async function refreshTokens(refreshToken) {
    const res = await api.post("/auth/refresh-token", { refreshToken });
    return res.data;
}

export async function signOut(refreshToken) {
    const res = await api.post("/auth/sign-out", { refreshToken });
    return res.data;
}