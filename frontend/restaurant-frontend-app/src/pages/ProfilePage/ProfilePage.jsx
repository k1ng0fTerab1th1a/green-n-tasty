import { useEffect, useMemo, useState } from "react";
import {
    Button,
    Input,
    PasswordInput,
    ProfileLayout,
    Toast,
} from "../../components/index.js";
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
        errors.firstName = "First name must be up to 50 characters. Only Latin letters, hyphens, and apostrophes are allowed.";
    }

    const lastNameTrimmed = form.lastName.trim();
    if (!lastNameTrimmed) {
        errors.lastName = "Last name is required.";
    } else if (lastNameTrimmed.length > 50 || !NAME_RE.test(lastNameTrimmed)) {
        errors.lastName = "Last name must be up to 50 characters. Only Latin letters, hyphens, and apostrophes are allowed.";
    }
    return errors;
}

export default function ProfilePage() {
    const [activeSection, setActiveSection] = useState("general");
    const { auth } = useAuth();

    const profile = {
        firstName: auth.username?.split(" ")[0] || "Jonson",
        lastName: auth.username?.split(" ")[1] || "Doe",
        email: auth.email || "johnsondoe@nomail.com",
        role: auth.role || "CUSTOMER",
    };

    const [form, setForm] = useState({
        firstName: profile.firstName,
        lastName: profile.lastName,
    });

    const [touched, setTouched] = useState({
        firstName: false,
        lastName: false,
    });

    const [passwordForm, setPasswordForm] = useState({
        oldPassword: "",
        newPassword: "",
        confirmPassword: "",
    });

    const [toastOpen, setToastOpen] = useState(false);
    const [toastData, setToastData] = useState(null);

    useEffect(() => {
        setForm({
            firstName: profile.firstName,
            lastName: profile.lastName,
        });
    }, [profile.firstName, profile.lastName]);

    const generalErrors = useMemo(() => validateGeneral(form), [form]);

    const isGeneralValid = Object.keys(generalErrors).length === 0;

    const isGeneralChanged =
        form.firstName.trim() !== profile.firstName ||
        form.lastName.trim() !== profile.lastName;

    const profileName = useMemo(() => {
        const roleLabel = ROLE_LABELS[profile.role] || profile.role;
        return `${profile.firstName} ${profile.lastName} (${roleLabel})`;
    }, [profile.firstName, profile.lastName, profile.role]);

    const handleChange = (e) => {
        const { name, value } = e.target;

        setForm((prev) => ({
            ...prev,
            [name]: value,
        }));
    };

    const handleBlur = (e) => {
        const { name } = e.target;

        setTouched((prev) => ({
            ...prev,
            [name]: true,
        }));
    };

    const handleSave = () => {
        setTouched({
            firstName: true,
            lastName: true,
        });

        if (!isGeneralValid) return;

        console.log("save profile", form);

        setToastData({
            type: "success",
            title: "Success",
            message: "Your Account has been updated successfully.",
        });
        setToastOpen(true);
    };

    const handlePasswordChange = (e) => {
        const { name, value } = e.target;

        setPasswordForm((prev) => ({
            ...prev,
            [name]: value,
        }));
    };

    const handlePasswordSave = () => {
        console.log("change password", passwordForm);
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
                                    <img src={userIcon} alt="" className={styles.avatarIcon} />
                                </div>

                                <Button
                                    type="button"
                                    variant="tertiary"
                                    size="sm"
                                    className={styles.uploadBtn}
                                >
                                    Upload Photo
                                </Button>
                            </div>

                            <div className={styles.formColumn}>
                                <div className={styles.profileMeta}>
                                    <div className={styles.profileName}>{profileName}</div>
                                    <div className={styles.profileEmail}>{profile.email}</div>
                                </div>

                                <div className={styles.formGrid}>
                                    <Input
                                        label="First Name"
                                        name="firstName"
                                        value={form.firstName}
                                        onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder="Enter your first name"
                                        hint="e.g. Jonson"
                                        error={touched.firstName ? generalErrors.firstName : ""}
                                    />

                                    <Input
                                        label="Last Name"
                                        name="lastName"
                                        value={form.lastName}
                                        onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder="Enter your last name"
                                        hint="e.g. Doe"
                                        error={touched.lastName ? generalErrors.lastName : ""}
                                    />
                                </div>

                                <div className={styles.actions}>
                                    <Button
                                        type="button"
                                        variant="primary"
                                        size="lg"
                                        onClick={handleSave}
                                        disabled={!isGeneralValid || !isGeneralChanged}
                                    >
                                        Save Changes
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
                                label="Password"
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
                                >
                                    Save Changes
                                </Button>
                            </div>
                        </div>
                    </div>
                )}

                {activeSection === "email" && (
                    <div className={styles.card}>
                        <div className={styles.placeholder}>
                            Тут буде секція зміни email
                        </div>
                    </div>
                )}
            </ProfileLayout>

            {toastData ? (
                <Toast
                    open={toastOpen}
                    type={toastData.type}
                    title={toastData.title}
                    message={toastData.message}
                    onClose={() => {
                        setToastOpen(false);
                        setToastData(null);
                    }}
                />
            ) : null}
        </>
    );
}