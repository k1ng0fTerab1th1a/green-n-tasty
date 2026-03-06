import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./Header.module.css";

import { NavigationLink, Button } from "../index.js";
import ProfileDropdown from "../ProfileDropdown/ProfileDropdown.jsx";

import logoIcon from "../../assets/icons/logo.svg";
import cartIcon from "../../assets/icons/cart.svg";
import userIcon from "../../assets/icons/user.svg";
import bellIcon from "../../assets/icons/bell.svg";

export default function Header({
                                   isAuth = false,
                                   role = "CUSTOMER",
                                   onSignIn,
                                   onProfileClick,
                                   onCartClick,
                                   onBellClick,
                                   userName = "User",
                                   userEmail = "",
                                   onSignOut,
                               }) {
    const navigate = useNavigate();

    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const menuWrapRef = useRef(null);

    useEffect(() => {
        if (!isMenuOpen) return;

        const onDown = (e) => {
            if (!menuWrapRef.current) return;
            if (!menuWrapRef.current.contains(e.target)) {
                setIsMenuOpen(false);
            }
        };

        const onKey = (e) => {
            if (e.key === "Escape") setIsMenuOpen(false);
        };

        document.addEventListener("mousedown", onDown);
        document.addEventListener("keydown", onKey);

        return () => {
            document.removeEventListener("mousedown", onDown);
            document.removeEventListener("keydown", onKey);
        };
    }, [isMenuOpen]);

    const normalizedRole = String(role || "").toUpperCase();

    const links = useMemo(() => {
        if (!isAuth) {
            return [
                { to: "/main", label: "Main page", end: true },
                { to: "/book", label: "Book a Table" },
            ];
        }

        if (normalizedRole === "WAITER") {
            return [
                { to: "/main", label: "Main page" },
                { to: "/reservations", label: "Reservations", end: true },
                { to: "/menu", label: "Menu" },
            ];
        }

        if (normalizedRole === "ADMIN") {
            return [
                { to: "/main", label: "Main page" },
                { to: "/reports", label: "Reports", end: true },
                { to: "/staff", label: "Staff" },
            ];
        }

        return [
            { to: "/main", label: "Main page", end: true },
            { to: "/book", label: "Book a Table" },
            { to: "/reservations", label: "Reservations" },
        ];
    }, [isAuth, normalizedRole]);

    const handleSignIn = () => {
        if (onSignIn) {
            onSignIn();
            return;
        }

        navigate("/login");
    };

    const goProfile = () => {
        setIsMenuOpen(false);

        if (onProfileClick) {
            onProfileClick();
            return;
        }

        navigate("/profile");
    };

    const handleSignOut = async () => {
        setIsMenuOpen(false);

        if (onSignOut) {
            await onSignOut();
        }

        navigate("/main", { replace: true });
    };

    const handleCart = () => {
        if (onCartClick) {
            onCartClick();
            return;
        }

        navigate("/cart");
    };

    const handleBell = () => {
        if (onBellClick) {
            onBellClick();
            return;
        }

        navigate("/notifications");
    };

    const toggleProfileMenu = () => setIsMenuOpen((prev) => !prev);

    const showBell = isAuth && normalizedRole === "WAITER";
    const showCart = isAuth && normalizedRole === "CUSTOMER";

    return (
        <header className={styles.header}>
            <div className={styles.inner}>
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

                <nav className={styles.nav} aria-label="Primary navigation">
                    {links.map((l) => (
                        <NavigationLink key={l.to} to={l.to} end={!!l.end}>
                            {l.label}
                        </NavigationLink>
                    ))}
                </nav>

                <div className={styles.right}>
                    {!isAuth ? (
                        <Button variant="secondary" size="lg" onClick={handleSignIn}>
                            Sign In
                        </Button>
                    ) : (
                        <div className={styles.icons}>
                            {showBell && (
                                <button
                                    type="button"
                                    className={styles.iconBtn}
                                    onClick={handleBell}
                                    aria-label="Notifications"
                                >
                                    <img src={bellIcon} alt="" />
                                </button>
                            )}

                            {showCart && (
                                <button
                                    type="button"
                                    className={styles.iconBtn}
                                    onClick={handleCart}
                                    aria-label="Cart"
                                >
                                    <img src={cartIcon} alt="" />
                                </button>
                            )}

                            <div className={styles.profileWrap} ref={menuWrapRef}>
                                <button
                                    type="button"
                                    className={styles.iconBtn}
                                    onClick={toggleProfileMenu}
                                    aria-label="Profile"
                                    aria-haspopup="menu"
                                    aria-expanded={isMenuOpen}
                                >
                                    <img src={userIcon} alt="" />
                                </button>

                                {isMenuOpen && (
                                    <div className={styles.dropdown}>
                                        <ProfileDropdown
                                            name={userName}
                                            email={userEmail}
                                            onProfile={goProfile}
                                            onSignOut={handleSignOut}
                                        />
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </header>
    );
}