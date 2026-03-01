import { useEffect, useMemo, useState } from "react";
import { useLocation } from "react-router-dom";
import "./Login.css";

import logo from "../../assets/images/login-hero.svg";
import eyeIcon from "../../assets/icons/eye.svg";
import eyeOffIcon from "../../assets/icons/eye-off.svg";

import { api } from "../../services/api";
import Toast from "../../components/Toast/Toast";

function validate(form) {
    const errors = {};

    // email
    if (!form.email.trim()) {
        errors.email = "Email address is required. Please enter your email to continue";
    } else if (!/^\S+@\S+\.\S+$/.test(form.email.trim())) {
        errors.email = "Invalid email address. Please ensure it follows the format: username@domain.com";
    }

    // password
    if (!form.password.trim()) {
        errors.password = "Password is required. Please enter your password to continue.";
    }

    return errors;
}

function Login() {
    const location = useLocation();

    const [form, setForm] = useState({ email: "", password: "" });
    const [touched, setTouched] = useState({});
    const [showPass, setShowPass] = useState(false);
    const [loading, setLoading] = useState(false);

    const [credentialsError, setCredentialsError] = useState(""); // под полями
    const [lockedError, setLockedError] = useState(""); // верхний баннер

    const [toast, setToast] = useState(null);

    const errors = useMemo(() => validate(form), [form]);
    const isValid = useMemo(() => Object.keys(errors).length === 0, [errors]);

    // success toast
    useEffect(() => {
        const msg = location.state?.successMessage;
        if (!msg) return;
        setToast(msg);
        window.history.replaceState({}, "", window.location.pathname);
    }, [location.state]);

    const onChange = (e) => {
        const { name, value } = e.target;
        setForm((prev) => ({ ...prev, [name]: value }));

        // очищаем серверные ошибки при вводе
        setCredentialsError("");
        setLockedError("");
    };

    const onBlur = (e) => {
        setTouched((prev) => ({ ...prev, [e.target.name]: true }));
    };

    const inputStateClass = (name) => {
        if (!touched[name]) return "";
        return errors[name] ? "input--error" : "";
    };

    const onSubmit = async (e) => {
        e.preventDefault();

        setTouched({ email: true, password: true });
        setCredentialsError("");
        setLockedError("");

        if (!isValid) return;

        try {
            setLoading(true);

            const res = await api.post("/auth/sign-in", {
                email: form.email.trim(),
                password: form.password,
            });

            const data = res.data;

            localStorage.setItem("accessToken", data.accessToken);
            localStorage.setItem("role", data.role);
            localStorage.setItem("username", data.username);

            // TODO
            // window.location.href = "/";
            // setToast("Logged in successfully!");
            alert("Login success (demo)");
        } catch (err) {
            const status = err?.response?.status;
            const apiMsg = err?.response?.data?.message;

            if (status === 423 || status === 429) {
                setLockedError(
                    "Your account is temporarily locked due to multiple failed login attempts. Please try again later."
                );
                setCredentialsError("Incorrect email or password. Try again or create an account.");
                return;
            }

            if (status === 401 || status === 400) {
                setCredentialsError("Incorrect email or password. Try again or create an account.");
                return;
            }

            setCredentialsError(apiMsg || err?.message || "Something went wrong. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-page">
            <div className="login-card">
                {/* LEFT */}
                <div className="login-left">
                    <div className="block-title login-kicker">WELCOME BACK</div>
                    <h1 className="login-title h2">Sign In to Your Account</h1>

                    {lockedError && <div className="login-alert">{lockedError}</div>}

                    <form className="login-form" onSubmit={onSubmit}>
                        <div className="field">
                            <label className="label body-bold">Email</label>
                            <input
                                name="email"
                                type="email"
                                className={`input ${inputStateClass("email")}`}
                                placeholder="Enter your Email"
                                value={form.email}
                                onChange={onChange}
                                onBlur={onBlur}
                            />

                            <div
                                className={`hint caption ${
                                    (touched.email && errors.email) || credentialsError ? "hint--error" : ""
                                }`}
                            >
                                {credentialsError
                                    ? credentialsError
                                    : touched.email && errors.email
                                        ? errors.email
                                        : "e.g. username@domain.com"}
                            </div>
                        </div>

                        <div className="field">
                            <label className="label body-bold">Password</label>

                            <div className="input-wrap">
                                <input
                                    name="password"
                                    type={showPass ? "text" : "password"}
                                    className={`input input--with-icon ${inputStateClass("password")}`}
                                    placeholder="Enter your Password"
                                    value={form.password}
                                    onChange={onChange}
                                    onBlur={onBlur}
                                />

                                <button
                                    type="button"
                                    className="icon-btn"
                                    onClick={() => setShowPass((v) => !v)}
                                    aria-label={showPass ? "Hide password" : "Show password"}
                                >
                                    <img className="icon" src={showPass ? eyeOffIcon : eyeIcon} alt="" aria-hidden="true" />
                                </button>
                            </div>

                            <div
                                className={`hint caption ${
                                    (touched.password && errors.password) || credentialsError ? "hint--error" : ""
                                }`}
                            >
                                {credentialsError
                                    ? credentialsError
                                    : touched.password && errors.password
                                        ? errors.password
                                        : " "}
                            </div>

                            <a className="link login-forgot" href="/forgot-password">
                                Forgot password?
                            </a>
                        </div>

                        <button
                            className={`submit button-text ${isValid ? "submit--ok" : ""}`}
                            type="submit"
                            disabled={!isValid || loading}
                        >
                            {loading ? "Signing In..." : "Sign In"}
                        </button>

                        <div className="bottom caption">
                            Don’t have an account?{" "}
                            <a className="link link-text" href="/register">
                                Create an Account
                            </a>
                        </div>
                    </form>
                </div>

                {/* RIGHT */}
                <div className="login-right">
                    <div className="brand h1">
                        <span className="brand-green">Green</span> <span>&amp;</span>{" "}
                        <span className="brand-dark">Tasty</span>
                    </div>

                    <img className="brand-logo" src={logo} alt="Green & Tasty logo" />
                </div>

                <Toast
                    open={!!toast}
                    type="success"
                    title="Success"
                    message={toast || ""}
                    onClose={() => setToast(null)}
                    autoCloseMs={4500}
                    showDelayMs={200}
                />
            </div>
        </div>
    );
}

export default Login;
