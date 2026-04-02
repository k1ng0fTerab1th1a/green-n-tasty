import { api } from "./api";


export async function signUp(payload) {
    const res = await api.post("/auth/sign-up", payload);
    return res.data;
}

export async function signIn(payload) {
    const res = await api.post("/auth/sign-in", payload);
    // console.log("Sign in response:", res.data);
    return res.data;
}

export const refreshTokens = async (refreshToken) => {
    const response = await api.post("/auth/refresh", { refreshToken });
    // console.log("Refresh response:", response.data);
    return response;
};

export async function signOut(refreshToken) {
    const res = await api.post("/auth/sign-out", { refreshToken });
    return res.data;
}