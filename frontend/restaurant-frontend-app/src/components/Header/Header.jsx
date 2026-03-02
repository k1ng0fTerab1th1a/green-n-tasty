import { useNavigate } from "react-router-dom";
import styles from "./Header.module.css";

import { NavigationLink, Button } from "../index.js";

import logoIcon from "../../assets/icons/logo.svg";
import cartIcon from "../../assets/icons/cart.svg";
import userIcon from "../../assets/icons/user.svg";
import bellIcon from "../../assets/icons/bell.svg";

export default function Header({
                                   isAuth = false,
                                   role = "user", // "user" | "waiter"
                                   onSignIn,
                                   onProfileClick,
                                   onCartClick,
                                   onBellClick,
                               }) {
    const navigate = useNavigate();

    const links = (() => {
        if (!isAuth) {
            return [
                { to: "/main", label: "Main page", end: true },
                { to: "/book", label: "Book a Table" },
            ];
        }

        if (role === "waiter") {
            return [
                { to: "/reservations", label: "Reservations", end: true },
                { to: "/menu", label: "Menu" },
            ];
        }

        if (role === "admin") {
            return [
                { to: "/reports", label: "Reports", end: true },
                { to: "/staff", label: "Staff" },
            ];
        }

        return [
            { to: "/main", label: "Main page", end: true },
            { to: "/book", label: "Book a Table" },
            { to: "/reservations", label: "Reservations" },
        ];
    })();

    const handleSignIn = () => {
        if (onSignIn) return onSignIn();
        navigate("/login");
    };

    const handleProfile = () => {
        if (onProfileClick) return onProfileClick();
        navigate("/profile");
    };

    const handleCart = () => {
        if (onCartClick) return onCartClick();
        navigate("/cart");
    };

    const handleBell = () => {
        if (onBellClick) return onBellClick();
        navigate("/notifications");
    };

    return (
        <header className={styles.header}>
            <div className={styles.inner}>
                {/* LEFT */}
                <button
                    type="button"
                    className={styles.brand}
                    onClick={() => navigate("/main")}
                    aria-label="Go to main page"
                >
                    <img src={logoIcon} alt="" className={styles.brandIcon} />
                    <span className={styles.brandText}>
            <span className={styles.brandGreen}>Green</span>
            <span className={styles.brandAmp}>&</span>
            <span className={styles.brandDark}>Tasty</span>
          </span>
                </button>

                {/* CENTER */}
                <nav className={styles.nav} aria-label="Primary navigation">
                    {links.map((l) => (
                        <NavigationLink key={l.to} to={l.to} end={!!l.end}>
                            {l.label}
                        </NavigationLink>
                    ))}
                </nav>

                {/* RIGHT */}
                <div className={styles.right}>
                    {!isAuth ? (
                        <Button variant="secondary" size="lg" onClick={handleSignIn}>
                            Sign In
                        </Button>
                    ) : role === "waiter" ? (
                        <div className={styles.icons}>
                            <button
                                type="button"
                                className={styles.iconBtn}
                                onClick={handleBell}
                                aria-label="Notifications"
                            >
                                <img src={bellIcon} alt="" />
                            </button>

                            <button
                                type="button"
                                className={styles.iconBtn}
                                onClick={handleProfile}
                                aria-label="Profile"
                            >
                                <img src={userIcon} alt="" />
                            </button>
                        </div>
                    ) : (
                        <div className={styles.icons}>
                            <button
                                type="button"
                                className={styles.iconBtn}
                                onClick={handleCart}
                                aria-label="Cart"
                            >
                                <img src={cartIcon} alt="" />
                            </button>

                            <button
                                type="button"
                                className={styles.iconBtn}
                                onClick={handleProfile}
                                aria-label="Profile"
                            >
                                <img src={userIcon} alt="" />
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </header>
    );
}