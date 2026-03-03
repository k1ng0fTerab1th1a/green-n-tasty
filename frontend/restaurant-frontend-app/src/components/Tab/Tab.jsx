import styles from "./Tab.module.css";

/**
 * Tab
 * Props:
 * - active?: boolean
 * - children
 * - onClick?: () => void
 * - disabled?: boolean
 * - className?: string
 * - type?: "button" | "submit"
 */
export default function Tab({
                                active = false,
                                children,
                                onClick,
                                disabled = false,
                                className = "",
                                type = "button",
                            }) {
    return (
        <button
            type={type}
            className={`${styles.tab} ${active ? styles.active : ""} ${className}`}
            onClick={onClick}
            disabled={disabled}
        >
            {children}
        </button>
    );
}