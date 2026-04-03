import { useEffect, useMemo, useState, useRef } from "react";
import {
    Button,
    Input,
    PasswordInput,
    ProfileLayout,
    Toast,
} from "../../components/index.js";
import { tokenStorage } from "../../services/tokenStorage";
import { useNavigate } from "react-router-dom";
import {
    getUserProfile,
    sendEmailVerificationCode,
    updateUsername,
    uploadAvatar,
    verifyEmailUpdate,
    changePassword
} from "../../services/user";
import { useAuth } from "../../auth/AuthContext";
import styles from "./ProfilePage.module.css";
import userIcon from "../../assets/icons/user.svg";

const SECTIONS = [
    { value: "general", label: "General information" },
    { value: "password", label: "Change Password" },
    { value: "email", label: "Change email" },
];

const ROLE_LABELS = {
    CUSTOMER: "Customer",
    WAITER: "Waiter",
    ADMIN: "Administrator",
};

const NAME_RE = /^[A-Za-z'-]+$/;

function validateGeneral(form) {
    const errors = {};
    const firstNameTrimmed = form.firstName.trim();
    if (!firstNameTrimmed) {
        errors.firstName = "First name is required.";
    } else if (firstNameTrimmed.length > 50 || !NAME_RE.test(firstNameTrimmed)) {
        errors.firstName = "Only Latin letters, hyphens, and apostrophes are allowed.";
    }

    const lastNameTrimmed = form.lastName.trim();
    if (!lastNameTrimmed) {
        errors.lastName = "Last name is required.";
    } else if (lastNameTrimmed.length > 50 || !NAME_RE.test(lastNameTrimmed)) {
        errors.lastName = "Only Latin letters, hyphens, and apostrophes are allowed.";
    }
    return errors;
}

export default function ProfilePage() {
    const navigate = useNavigate();  // ← Додайте це
    const [activeSection, setActiveSection] = useState("general");
    const fileInputRef = useRef(null);
    const { auth, updateUserProfile } = useAuth()
    const [userProfile, setUserProfile] = useState({
        firstName: "",
        lastName: "",
        email: "",
        role: "",
        imageUrl: ""
    });
    const [passwordForm, setPasswordForm] = useState({
        oldPassword: "",
        newPassword: "",
        confirmPassword: ""
    });
    const [form, setForm] = useState({ firstName: "", lastName: "" });
    const [touched, setTouched] = useState({ firstName: false, lastName: false });
    const [isLoading, setIsLoading] = useState(false);

    const [toastOpen, setToastOpen] = useState(false);
    const [toastData, setToastData] = useState(null);

    const [emailStep, setEmailStep] = useState("input");
    const [emailForm, setEmailForm] = useState({ newEmail: "", code: "" });
    const [resendTimer, setResendTimer] = useState(59);

    useEffect(() => {
        const fetchProfile = async () => {
            try {
                const res = await getUserProfile();
                if (res.data.isSuccess) {
                    const data = res.data.data;
                    setUserProfile(data);
                    setForm({
                        firstName: data.firstName || "",
                        lastName: data.lastName || ""
                    });
                }
            } catch (err) {
                console.error("Failed to fetch profile", err);
            }
        };
        fetchProfile();
    }, []);

    useEffect(() => {
        let timer;
        if (emailStep === "verify" && resendTimer > 0) {
            timer = setInterval(() => {
                setResendTimer((prev) => prev - 1);
            }, 1000);
        }
        return () => clearInterval(timer);
    }, [emailStep, resendTimer]);

    const generalErrors = useMemo(() => validateGeneral(form), [form]);
    const isGeneralValid = Object.keys(generalErrors).length === 0;
    const isGeneralChanged =
        form.firstName.trim() !== (userProfile.firstName || "") ||
        form.lastName.trim() !== (userProfile.lastName || "");

    const profileName = useMemo(() => {
        const roleLabel = ROLE_LABELS[userProfile.role] || userProfile.role;
        return `${userProfile.firstName} ${userProfile.lastName} (${roleLabel})`;
    }, [userProfile]);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setForm(prev => ({ ...prev, [name]: value }));
    };

    const handleSave = async () => {
        setTouched({ firstName: true, lastName: true });
        if (!isGeneralValid) return;

        setIsLoading(true);
        try {
            const res = await updateUsername({
                firstName: form.firstName.trim(),
                lastName: form.lastName.trim()
            });

            if (res.data.isSuccess) {
                setUserProfile(prev => ({ ...prev, ...form }));

                updateUserProfile({
                    firstName: form.firstName.trim(),
                    lastName: form.lastName.trim()
                });

                setToastData({
                    type: "success",
                    title: "Success",
                    message: "Your profile has been updated successfully.",
                });
                setToastOpen(true);
            }
        } catch (err) {
            setToastData({
                type: "error",
                title: "Error",
                message: err.response?.data?.message || "Failed to update profile."
            });
            setToastOpen(true);
        } finally {
            setIsLoading(false);
        }
    };

    const handleFileChange = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const allowedTypes = ["image/jpeg", "image/png", "image/webp"];
        if (!allowedTypes.includes(file.type)) {
            setToastData({
                type: "error",
                title: "Invalid format",
                message: "Only JPG, PNG and WebP are allowed."
            });
            setToastOpen(true);
            e.target.value = "";
            return;
        }

        setIsLoading(true);
        try {
            const res = await uploadAvatar(file);
            if (res.data.isSuccess) {
                const newImageUrl = `${res.data.data}?t=${new Date().getTime()}`;

                setUserProfile(prev => ({ ...prev, imageUrl: newImageUrl }));
                updateUserProfile({
                    ...auth,
                    imageUrl: newImageUrl
                });

                setToastData({
                    type: "success",
                    title: "Success",
                    message: "Photo uploaded successfully.",
                });
                setToastOpen(true);
            }
        } catch (err) {
            setToastData({
                type: "error",
                title: "Error",
                message: err.response?.data?.message || "Failed to upload photo."
            });
            setToastOpen(true);
        } finally {
            setIsLoading(false);
            e.target.value = "";
        }
    };

    const handlePasswordChange = (e) => {
        const { name, value } = e.target;

        setPasswordForm((prev) => ({
            ...prev,
            [name]: value,
        }));
    };

    const handlePasswordSave = async () => {
        if (!passwordForm.oldPassword) {
            setToastData({
                type: "error",
                title: "Error",
                message: "Current password is required."
            });
            setToastOpen(true);
            return;
        }

        if (!passwordForm.newPassword) {
            setToastData({
                type: "error",
                title: "Error",
                message: "New password is required."
            });
            setToastOpen(true);
            return;
        }

        if (passwordForm.newPassword !== passwordForm.confirmPassword) {
            setToastData({
                type: "error",
                title: "Error",
                message: "New password and confirmation do not match."
            });
            setToastOpen(true);
            return;
        }

        if (passwordForm.newPassword.length < 8) {
            setToastData({
                type: "error",
                title: "Error",
                message: "Password must be at least 8 characters long."
            });
            setToastOpen(true);
            return;
        }

        setIsLoading(true);
        try {
            const accessToken = tokenStorage.getSession().accessToken;

            if (!accessToken) {
                throw new Error("No access token found. Please login again.");
            }

            console.log("Changing password with access token:", accessToken.substring(0, 50) + "...");

            const res = await changePassword(
                passwordForm.oldPassword,
                passwordForm.newPassword,
                passwordForm.confirmPassword,
                accessToken
            );

            if (res.data.isSuccess) {
                setPasswordForm({
                    oldPassword: "",
                    newPassword: "",
                    confirmPassword: ""
                });

                setToastData({
                    type: "success",
                    title: "Success",
                    message: "Your password has been changed successfully."
                });
                setToastOpen(true);

                setTimeout(() => {
                    tokenStorage.clear();
                    navigate("/login", {
                        replace: true,
                        state: {
                            toast: {
                                type: "success",
                                title: "Password Changed",
                                message: "Your password has been changed. Please sign in with your new password."
                            }
                        }
                    });
                }, 2000);
            }
        } catch (err) {
            console.error("Password change error:", err.response?.data);
            setToastData({
                type: "error",
                title: "Error",
                message: err.response?.data?.message || "Failed to change password. Please check your current password."
            });
            setToastOpen(true);
        } finally {
            setIsLoading(false);
        }
    };

    const handleSendCode = async () => {
        if (!emailForm.newEmail) return;

        setIsLoading(true);
        try {
            const accessToken = tokenStorage.getSession().accessToken;

            if (!accessToken) {
                throw new Error("No access token found");
            }

            console.log("Using access token:", accessToken.substring(0, 50) + "...");

            await sendEmailVerificationCode(
                emailForm.newEmail,
                accessToken
            );

            setEmailStep("verify");
            setResendTimer(59);

            setToastData({
                type: "success",
                title: "Success",
                message: "Verification code sent to your new email"
            });
            setToastOpen(true);

        } catch (err) {
            console.error("Error details:", err.response?.data);
            setToastData({
                type: "error",
                title: "Error",
                message: err.response?.data?.message || "Failed to send verification code"
            });
            setToastOpen(true);
        } finally {
            setIsLoading(false);
        }
    };

    const handleVerifyEmail = async () => {
        if (!emailForm.code) return;

        setIsLoading(true);
        try {
            const accessToken = tokenStorage.getSession().accessToken;

            if (!accessToken) {
                throw new Error("No access token found");
            }

            const res = await verifyEmailUpdate(
                emailForm.code,
                emailForm.newEmail,
                accessToken
            );

            if (res.data.isSuccess) {
                setToastData({
                    type: "success",
                    title: "Email Changed",
                    message: "Your email has been changed successfully. Please sign in again with your new email."
                });
                setToastOpen(true);

                setTimeout(() => {
                    tokenStorage.clear();
                    navigate("/login", {
                        replace: true,
                        state: {
                            toast: {
                                type: "success",
                                title: "Email Updated",
                                message: "Your email has been changed. Please sign in with your new email address."
                            }
                        }
                    });
                }, 2000);
            }
        } catch (err) {
            console.error("Verify error:", err.response?.data);
            setToastData({
                type: "error",
                title: "Error",
                message: err.response?.data?.message || "Invalid verification code"
            });
            setToastOpen(true);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <>
            <ProfileLayout
                title="My Profile"
                sections={SECTIONS}
                activeSection={activeSection}
                onSectionChange={setActiveSection}
            >
                {activeSection === "general" && (
                    <div className={styles.card}>
                        <div className={styles.cardInner}>
                            <div className={styles.avatarColumn}>
                                <div className={styles.avatarWrap}>
                                    <img
                                        src={userProfile.imageUrl || userIcon}
                                        alt="Profile"
                                        className={userProfile.imageUrl ? styles.avatarImage : styles.avatarIcon}
                                    />
                                </div>

                                <input
                                    type="file"
                                    ref={fileInputRef}
                                    onChange={handleFileChange}
                                    style={{ display: "none" }}
                                    accept="image/*"
                                />

                                <Button
                                    type="button"
                                    variant="tertiary"
                                    size="sm"
                                    className={styles.uploadBtn}
                                    onClick={() => fileInputRef.current.click()}
                                    disabled={isLoading}
                                >
                                    Upload Photo
                                </Button>
                            </div>

                            <div className={styles.formColumn}>
                                <div className={styles.profileMeta}>
                                    <div className={styles.profileName}>{profileName}</div>
                                    <div className={styles.profileEmail}>{userProfile.email}</div>
                                </div>

                                <div className={styles.formGrid}>
                                    <Input
                                        label="First Name"
                                        name="firstName"
                                        value={form.firstName}
                                        onChange={handleChange}
                                        onBlur={() => setTouched(p => ({...p, firstName: true}))}
                                        placeholder="Enter first name"
                                        error={touched.firstName ? generalErrors.firstName : ""}
                                    />

                                    <Input
                                        label="Last Name"
                                        name="lastName"
                                        value={form.lastName}
                                        onChange={handleChange}
                                        onBlur={() => setTouched(p => ({...p, lastName: true}))}
                                        placeholder="Enter last name"
                                        error={touched.lastName ? generalErrors.lastName : ""}
                                    />
                                </div>

                                <div className={styles.actions}>
                                    <Button
                                        type="button"
                                        variant="primary"
                                        size="lg"
                                        onClick={handleSave}
                                        disabled={!isGeneralValid || !isGeneralChanged || isLoading}
                                    >
                                        {isLoading ? "Saving..." : "Save Changes"}
                                    </Button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {activeSection === "password" && (
                    <div className={styles.card}>
                        <div className={styles.passwordForm}>
                            <PasswordInput
                                label="Old Password"
                                name="oldPassword"
                                value={passwordForm.oldPassword}
                                onChange={handlePasswordChange}
                                showChecklist={false}
                                showStrength={false}
                            />

                            <PasswordInput
                                label="New Password"
                                name="newPassword"
                                value={passwordForm.newPassword}
                                onChange={handlePasswordChange}
                            />

                            <PasswordInput
                                label="Confirm New Password"
                                name="confirmPassword"
                                value={passwordForm.confirmPassword}
                                matchValue={passwordForm.newPassword}
                                onChange={handlePasswordChange}
                                showChecklist={false}
                                showStrength={false}
                                hint="Confirm password must match new password"
                            />

                            <div className={styles.actions}>
                                <Button
                                    type="button"
                                    variant="primary"
                                    size="lg"
                                    onClick={handlePasswordSave}
                                    disabled={isLoading}
                                >
                                    {isLoading ? "Changing..." : "Save Changes"}
                                </Button>
                            </div>
                        </div>
                    </div>
                )}

                {activeSection === "email" && (
                    <div className={styles.card}>
                        <div className={styles.emailContainer}>
                            {emailStep === "input" && (
                                <div className={styles.emailFormBox}>
                                    <div className={styles.emailCurrent}>
                                        <strong>Current email:</strong> {userProfile.email}
                                    </div>
                                    <div className={styles.emailInputGroup}>
                                        <Input
                                            label="New Email"
                                            name="newEmail"
                                            value={emailForm.newEmail}
                                            onChange={(e) => setEmailForm({ ...emailForm, newEmail: e.target.value })}
                                            placeholder="Enter your New Email"
                                            hint="e.g. username@domain.com"
                                        />
                                        <div className={styles.emailActions}>
                                            <Button
                                                variant="primary"
                                                onClick={handleSendCode}
                                                disabled={!emailForm.newEmail || isLoading}
                                            >
                                                {isLoading ? "Sending..." : "Send Code"}
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            )}

                            {emailStep === "verify" && (
                                <div className={styles.emailFormBox}>
                                    <p className={styles.emailNote}>
                                        The verification code has been sent to your email to
                                        {" "}{emailForm.newEmail}. Please enter the code from the email.
                                    </p>
                                    <Input
                                        label="Verification Code"
                                        name="code"
                                        value={emailForm.code}
                                        onChange={(e) => setEmailForm({ ...emailForm, code: e.target.value })}
                                        placeholder="Enter verification code"
                                    />
                                    <div className={styles.dualActions}>
                                        <Button variant="secondary" onClick={() => setEmailStep("input")}>
                                            Back
                                        </Button>
                                        <Button
                                            variant="primary"
                                            onClick={handleVerifyEmail}
                                            disabled={!emailForm.code || isLoading}
                                        >
                                            Save Changes
                                        </Button>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </ProfileLayout>

            <Toast
                open={toastOpen}
                type={toastData?.type}
                title={toastData?.title}
                message={toastData?.message}
                onClose={() => setToastOpen(false)}
            />
        </>
    );
}