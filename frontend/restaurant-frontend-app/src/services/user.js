import { api } from "./api";

export const getUserProfile = () => api.get("/user/me");

export const updateUsername = (data) => api.put("/user/username", data);

export const uploadAvatar = (file) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/user/avatar", formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
};

export const sendEmailVerificationCode = (newEmail, accessToken) => {
    return api.put("/user/email", { newEmail, accessToken });
};

export const verifyEmailUpdate = (code, newEmail, accessToken) => {
    return api.post("/user/email/verify", { code, newEmail, accessToken });
};

export const changePassword = (currentPassword, newPassword, confirmNewPassword, accessToken) => {
    return api.put("/user/password", {
        currentPassword,
        newPassword,
        confirmNewPassword,
        accessToken
    });
};