import { useMemo, useState, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";

import { AuthLayout, Input, Button, PasswordInput, Toast } from "../../components/index.js";
import { signIn } from "../../services/auth";
import { useAuth } from "../../auth/AuthContext";

import heroImg from "../../assets/icons/login-hero.svg";
import styles from "./LoginPage.module.css";

function validate(form) {
    const errors = {};

    if (!form.email.trim()) {
        errors.email = "Email address is required. Please enter your email to continue";
    } else if (!/^\S+@\S+\.\S+$/.test(form.email)) {
        errors.email = "Invalid email address. Please ensure it follows the format: username@domain.com";
    }

    if (!form.password.trim()) {
        errors.password = "Password is required. Please enter your password to continue.";
    }

    return errors;
}

export default function LoginPage() {
    const location = useLocation();
    const navigate = useNavigate()

    const [form, setForm] = useState({ email: "", password: "" });
    const [touched, setTouched] = useState({ email: false, password: false });

    const [status, setStatus] = useState("idle"); // idle | loading | invalid | locked | server_error
    const [banner, setBanner] = useState(""); // locked/server_error)
    const [toastOpen, setToastOpen] = useState(false);
    const [toastData, setToastData] = useState(null);
    const { signInSuccess } = useAuth();

    useEffect(() => {
        const toast = location.state?.toast;
        if (!toast) return;

        setToastData(toast);
        setToastOpen(true);

        navigate(location.pathname, { replace: true, state: {} });
    }, [location.state, location.pathname, navigate]);

    const clientErrors = useMemo(() => validate(form), [form]);

    const invalidMsg = "Incorrect email or password. Try again or create an account.";

    const serverFieldErrors = useMemo(() => {
        if (status === "invalid" || status === "locked") {
            return { email: invalidMsg, password: invalidMsg };
        }
        return { email: "", password: "" };
    }, [status]);

    const emailError =
        (touched.email ? clientErrors.email : "") || serverFieldErrors.email;

    const passwordError =
        (touched.password ? clientErrors.password : "") || serverFieldErrors.password;

    const isLoading = status === "loading";

    const isClientValid = Object.keys(clientErrors).length === 0;

    const onChange = (e) => {
        const { name, value } = e.target;

        if (status === "invalid" || status === "locked" || status === "server_error") {
            setStatus("idle");
            setBanner("");
        }

        setForm((p) => ({ ...p, [name]: value }));
    };

    const onBlur = (e) => {
        const { name } = e.target;
        setTouched((p) => ({ ...p, [name]: true }));
    };

    const onSubmit = async (e) => {
        e.preventDefault();

        setTouched({ email: true, password: true });
        if (!isClientValid) return;

        try {
            setStatus("loading");
            setBanner("");

            const res = await signIn({
                email: form.email.trim(),
                password: form.password,
            });

            if (!res?.isSuccess) {
                setStatus("server_error");
                setBanner(res?.message || "LoginPage failed. Please try again.");
                return;
            }

            const { idToken, refreshToken, username, role, email } = res.data;

            if (!idToken || !refreshToken) {
                setStatus("server_error");
                setBanner("Tokens were not returned by the server.");
                return;
            }

            signInSuccess({
                idToken,
                refreshToken,
                username,
                role,
                email: email || form.email.trim(),
            });

            setStatus("idle");
            navigate("/main", { replace: true });
        } catch (err) {
            const httpStatus = err?.response?.status;
            const serverMsg = err?.response?.data?.message;

            if (httpStatus === 401 || httpStatus === 400) {
                setStatus("invalid");
                return;
            }
            if (httpStatus === 423) {
                setStatus("locked");
                setBanner(
                    serverMsg ||
                    "Your account is temporarily locked due to multiple failed login attempts. Please try again later."
                );
                return;
            }
            if (httpStatus === 403) {
                setStatus("locked");
                setBanner(serverMsg || "Access denied. Please contact support.");
                return;
            }

            setStatus("server_error");
            setBanner(serverMsg || "Something went wrong. Please try again.");
        }
    };

    const heroTitle = (
        <>
            <span className="auth-hero-accent">Green</span> & <span>Tasty</span>
        </>
    );

    return (
        <>
            <AuthLayout
                kicker="WELCOME BACK"
                title="Sign In to Your Account"
                heroTitle={heroTitle}
                heroImage={heroImg}
                heroAlt="Green & Tasty"
            >
                <form className={styles['login-form']} onSubmit={onSubmit}>
                    {banner ? <div className={styles['login-banner']}>{banner}</div> : null}

                    <Input
                        name="email"
                        label="Email"
                        placeholder="Enter your Email"
                        value={form.email}
                        onChange={onChange}
                        onBlur={onBlur}
                        hint="e.g. username@domain.com"
                        error={emailError}
                    />

                    <div className={styles['login-password']}>
                        <PasswordInput
                            name="password"
                            label="Password"
                            placeholder="Enter your Password"
                            value={form.password}
                            onChange={onChange}
                            onBlur={onBlur}
                            error={passwordError}
                            showStrength={false}
                            showChecklist={false}
                        />

                        <Link className={styles['login-forgot']} to="/forgot-password">
                            Forgot password?
                        </Link>
                    </div>

                    <div className={styles['login-actions']}>
                        <Button
                            type="submit"
                            variant="primary"
                            size="lg"
                            fullWidth
                            disabled={!isClientValid || isLoading}
                        >
                            {isLoading ? "Signing in..." : "Sign In"}
                        </Button>

                        <div className={styles['login-footer']}>
                            <span className="caption">Don’t have an account?</span>{" "}
                            <Link className={styles['login-link']} to="/register">
                                Create an Account
                            </Link>
                        </div>
                    </div>
                </form>
            </AuthLayout>
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