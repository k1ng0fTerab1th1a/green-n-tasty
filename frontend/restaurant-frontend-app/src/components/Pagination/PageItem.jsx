import styles from "./PageItem.module.css";

/**
 * PageItem
 * Props:
 * - active?: boolean
 * - children (number/string)
 * - onClick?: () => void
 * - disabled?: boolean
 */
export default function PageItem({ active = false, children, onClick, disabled = false }) {
    return (
        <button
            type="button"
            className={`${styles.page} ${active ? styles.active : ""}`}
            onClick={onClick}
            disabled={disabled}
        >
            {children}
        </button>
    );
}