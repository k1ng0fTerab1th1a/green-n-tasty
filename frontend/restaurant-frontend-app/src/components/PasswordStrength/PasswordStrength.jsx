import styles from "./PasswordStrength.module.css";

export default function PasswordStrength({ value = "weak", className = "" }) {
    const labelMap = {
        weak: "Weak",
        medium: "Medium",
        strong: "Strong",
    };

    return (
        <div className={`${styles.badge} ${styles[value]} ${className}`}>
            <span className={styles.dot} />
            <span className={styles.text}>{labelMap[value] || "Weak"}</span>
        </div>
    );
}