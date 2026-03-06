import styles from "./Radio.module.css";

export default function Radio({
                                  name,
                                  value,
                                  checked,
                                  onChange,
                                  label = "",
                                  disabled = false,
                              }) {
    return (
        <label className={`${styles.wrap} ${disabled ? styles.disabled : ""}`}>
            <input
                className={styles.native}
                type="radio"
                name={name}
                value={value}
                checked={checked}
                disabled={disabled}
                onChange={() => onChange?.(value)}
            />

            <span className={`${styles.radio} ${checked ? styles.checked : ""}`} aria-hidden="true">
        <span className={styles.dot} />
      </span>

            {label ? <span className={styles.label}>{label}</span> : null}
        </label>
    );
}