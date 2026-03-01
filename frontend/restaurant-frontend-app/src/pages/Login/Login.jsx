import { useMemo, useState, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";

import { AuthLayout, Input, Button, PasswordInput, Toast } from "../../components/index.js";

import heroImg from "../../assets/images/login-hero.svg";
import "./Login.css";


async function fakeSignIn({ email, password }) {
    await new Promise((r) => setTimeout(r, 600));

    // DEMO
    if (email === "locked@domain.com") {
        const err = new Error("ACCOUNT_LOCKED");
        err.code = "ACCOUNT_LOCKED";
        throw err;
    }
    if (email !== "user@domain.com" || password !== "Test123.") {
        const err = new Error("INVALID_CREDENTIALS");
        err.code = "INVALID_CREDENTIALS";
        throw err;
    }

    return { token: "demo-token" };
}

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

export default function Login() {
    const location = useLocation();
    const navigate = useNavigate()

    const [form, setForm] = useState({ email: "", password: "" });
    const [touched, setTouched] = useState({ email: false, password: false });

    // server states
    const [status, setStatus] = useState("idle"); // idle | loading | invalid | locked | server_error
    const [banner, setBanner] = useState(""); // locked/server_error)
    const [toastOpen, setToastOpen] = useState(false);
    const [toastData, setToastData] = useState(null);

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

            // TODO: Replace with real API: Wait for login (form)
            const res = await fakeSignIn(form);

            // TODO: save token, redirect
            console.log("signed in:", res);
            setStatus("idle");
        } catch (err) {
            const code = err?.code || err?.message;

            if (code === "ACCOUNT_LOCKED") {
                setStatus("locked");
                setBanner(
                    "Your account is temporarily locked due to multiple failed login attempts. Please try again later."
                );
                return;
            }

            if (code === "INVALID_CREDENTIALS") {
                setStatus("invalid");
                return;
            }

            // network/server fallback
            setStatus("server_error");
            setBanner("Something went wrong. Please try again.");
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
                <form className="login-form" onSubmit={onSubmit}>
                    {banner ? <div className="login-banner">{banner}</div> : null}

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

                    <div className="login-password">
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

                        <Link className="login-forgot" to="/forgot-password">
                            Forgot password?
                        </Link>
                    </div>

                    <div className="login-actions">
                        <Button
                            type="submit"
                            variant="primary"
                            size="lg"
                            fullWidth
                            disabled={!isClientValid || isLoading}
                        >
                            {isLoading ? "Signing in..." : "Sign In"}
                        </Button>

                        <div className="login-footer">
                            <span className="caption">Don’t have an account?</span>{" "}
                            <Link className="login-link" to="/register">
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