import { api } from "./api";

export function signUp(payload) {
    return api.post("/auth/sign-up", payload);
}

export function signIn(payload) {
    return api.post("/auth/sign-in", payload);
}