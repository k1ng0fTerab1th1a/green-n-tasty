import styles from "./Checkbox.module.css";

import checkIcon from "../../assets/icons/check.svg";

/**
 * Checkbox
 * Props:
 * - checked: boolean
 * - onChange: (next:boolean) => void
 * - label?: string
 * - disabled?: boolean
 */
export default function Checkbox({
                                     checked,
                                     onChange,
                                     label = "",
                                     disabled = false,
                                 }) {
    return (
        <label className={`${styles.wrap} ${disabled ? styles.disabled : ""}`}>
            <input
                className={styles.native}
                type="checkbox"
                checked={checked}
                disabled={disabled}
                onChange={(e) => onChange?.(e.target.checked)}
            />

            <span className={`${styles.box} ${checked ? styles.checked : ""}`} aria-hidden="true">
        <img className={styles.check} src={checkIcon} alt="" />
      </span>

            {label ? <span className={styles.label}>{label}</span> : null}
        </label>
    );
}