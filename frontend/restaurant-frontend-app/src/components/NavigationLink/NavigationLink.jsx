import { NavLink } from "react-router-dom";
import styles from "./NavigationLink.module.css";

/**
 * NavigationLink
 * Props:
 * - to: string
 * - children: node
 * - end?: boolean
 * - className?: string
 */
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