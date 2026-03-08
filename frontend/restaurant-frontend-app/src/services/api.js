import axios from "axios";
import { tokenStorage } from "./tokenStorage";
import { refreshTokens } from "./auth";

export const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
    headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
    const { idToken } = tokenStorage.getSession();

    if (idToken) {
        config.headers.Authorization = `Bearer ${idToken}`;
    }

    return config;
});

let refreshPromise = null;

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response?.status !== 401 || originalRequest._retry) {
            return Promise.reject(error);
        }

        originalRequest._retry = true;

        const { refreshToken } = tokenStorage.getSession();

        if (!refreshToken) {
            tokenStorage.clear();
            return Promise.reject(error);
        }

        try {

            if (!refreshPromise) {
                refreshPromise = refreshTokens(refreshToken)
                    .finally(() => refreshPromise = null);
            }

            const res = await refreshPromise;

            const newIdToken = res.data.data.idToken;
            const newRefreshToken = res.data.data.refreshToken;

            tokenStorage.setSession({
                idToken: newIdToken,
                refreshToken: newRefreshToken,
                username: tokenStorage.getSession().username,
                role: tokenStorage.getSession().role,
                email: tokenStorage.getSession().email,
            });

            originalRequest.headers.Authorization = `Bearer ${newIdToken}`;

            return api(originalRequest);

        } catch (err) {

            tokenStorage.clear();
            return Promise.reject(err);

        }
    }
);