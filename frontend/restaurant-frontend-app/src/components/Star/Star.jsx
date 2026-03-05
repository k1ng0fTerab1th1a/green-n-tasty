import styles from "./Star.module.css";

import starOn from "../../assets/icons/star-filled.svg";
import starOff from "../../assets/icons/star-outline.svg";

/**
 * Star (favorite)
 * Props:
 * - checked: boolean
 * - onChange: (next:boolean) => void
 * - disabled?: boolean
 * - size?: number (default 20)
 * - ariaLabel?: string
 */
export default function Star({
                                 checked,
                                 onChange,
                                 disabled = false,
                                 size = 20,
                                 ariaLabel = "Toggle favorite",
                             }) {
    return (
        <button
            type="button"
            className={`${styles.star} ${disabled ? styles.disabled : ""}`}
            onClick={() => !disabled && onChange?.(!checked)}
            aria-pressed={checked}
            aria-label={ariaLabel}
            disabled={disabled}
        >
            <img
                src={checked ? starOn : starOff}
                alt=""
                width={size}
                height={size}
                className={styles.icon}
            />
        </button>
    );
}