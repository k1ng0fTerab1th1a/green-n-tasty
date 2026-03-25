import { NavLink } from "react-router-dom";
import styles from "./NavigationLink.module.css";

export default function NavigationLink({ to, children, end = false, className = "" }) {
    return (
        <NavLink
            to={to}
            end={end}
            className={({ isActive }) =>
                `${styles.link} ${isActive ? styles.active : ""} ${className}`
            }
        >
            {children}
        </NavLink>
    );
}