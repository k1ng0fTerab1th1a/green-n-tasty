import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { checkPasswordAllRules } from "../../utils/passwordRules";

import { AuthLayout, Input, PasswordInput, Button } from "../../components/index.js";

import heroImg from "../../assets/images/login-hero.svg";
import styles from "./Register.module.css";

import { signUp } from "../../services/auth";

const NAME_RE = /^[a-zA-Z\s'-]+$/;

function validate(form) {
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

    if (!form.email.trim()) errors.email = "Email address is required. Please enter your email to continue";
    else if (!/^\S+@\S+\.\S+$/.test(form.email))
        errors.email = "Invalid email address. Please ensure it follows the format: username@domain.com";

    if (!form.password.trim()) {
        errors.password = "Password is required. Please enter your password to continue.";
    } else {
        const { isValid } = checkPasswordAllRules(form.password);
        if (!isValid) {
            errors.password = "Password is too weak. Please meet all requirements.";
        }
    }

    if (!form.confirmPassword.trim()) errors.confirmPassword = "Confirm password is required.";
    else if (form.confirmPassword !== form.password) errors.confirmPassword = "Passwords don't match.";

    return errors;
}

export default function Register() {
    const navigate = useNavigate();

    const [form, setForm] = useState({
        firstName: "",
        lastName: "",
        email: "",
        password: "",
        confirmPassword: "",
    });

    const [touched, setTouched] = useState({});
    const [serverErrors, setServerErrors] = useState({});
    const errors = useMemo(() => validate(form), [form]);

    const isValid = Object.keys(errors).length === 0;

    const onChange = (e) => {
        const { name, value } = e.target;
        setForm((p) => ({ ...p, [name]: value }));
    };

    const onBlur = (e) => {
        const { name } = e.target;
        setTouched((p) => ({ ...p, [name]: true }));
    };

    const onSubmit = async (e) => {
        e.preventDefault();

        setTouched({
            firstName: true,
            lastName: true,
            email: true,
            password: true,
            confirmPassword: true,
        });

        if (!isValid) return;

        try {
            setServerErrors({});

            await signUp({
                firstName: form.firstName.trim(),
                lastName: form.lastName.trim(),
                email: form.email.trim(),
                password: form.password,
            });

            navigate("/login", {
                state: {
                    toast: {
                        type: "success",
                        title: "Success",
                        message:
                            "Your account has been created successfully. Please sign in with your details.",
                    },
                },
            });
        } catch (err) {
            if (err.response?.status === 409) {
                setServerErrors({
                    email: err.response.data?.message || "User already exists.",
                });
            } else {
                console.error(err);
            }
        }
    };

    const heroTitle = (
        <>
            <span className="auth-hero-accent">Green</span> & <span>Tasty</span>
        </>
    );

    return (
        <AuthLayout
            kicker="LET’S GET YOU STARTED"
            title="Create an Account"
            heroTitle={heroTitle}
            heroImage={heroImg}
            heroAlt="Green & Tasty"
        >
            <form className={styles['register-form']} onSubmit={onSubmit}>
                <div className={styles['register-grid-2']}>
                    <Input
                        name="firstName"
                        label="First Name"
                        placeholder="Enter your First Name"
                        value={form.firstName}
                        onChange={onChange}
                        onBlur={onBlur}
                        hint="e.g. Jonson"
                        error={touched.firstName ? errors.firstName : ""}
                    />

                    <Input
                        name="lastName"
                        label="Last Name"
                        placeholder="Enter your Last Name"
                        value={form.lastName}
                        onChange={onChange}
                        onBlur={onBlur}
                        hint="e.g. Doe"
                        error={touched.lastName ? errors.lastName : ""}
                    />
                </div>

                <Input
                    name="email"
                    label="Email"
                    placeholder="Enter your Email"
                    value={form.email}
                    onChange={(e) => {
                        onChange(e);
                        setServerErrors((prev) => ({ ...prev, email: "" }));
                    }}
                    onBlur={onBlur}
                    hint="e.g. username@domain.com"
                    error={
                        touched.email
                            ? errors.email || serverErrors.email
                            : serverErrors.email
                    }
                />

                <PasswordInput
                    name="password"
                    label="Password"
                    placeholder="Enter your Password"
                    value={form.password}
                    onChange={onChange}
                    onBlur={onBlur}
                    error={touched.password ? errors.password : ""}
                    showStrength={true}
                    showChecklist={true}
                />

                <PasswordInput
                    name="confirmPassword"
                    label="Confirm New Password"
                    placeholder="Confirm New Password"
                    value={form.confirmPassword}
                    matchValue={form.password}
                    onChange={onChange}
                    onBlur={onBlur}
                    error={touched.confirmPassword ? errors.confirmPassword : ""}
                    showStrength={false}
                    showChecklist={false}
                    hint="Confirm password must match new password"
                />

                <div className={styles['register-actions']}>
                    <Button
                        type="submit"
                        variant="primary"
                        size="lg"
                        fullWidth
                        disabled={!isValid}
                    >
                        Create an Account
                    </Button>

                    <div className={styles['register-footer']}>
                        <span className="caption">Already have an account?</span>{" "}
                        <Link className={styles['register-link']} to="/login">
                            Login
                        </Link>{" "}
                        <span className="caption">instead</span>
                    </div>
                </div>
            </form>
        </AuthLayout>
    );
}