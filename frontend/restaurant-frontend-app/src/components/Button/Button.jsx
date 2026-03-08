import styles from "./Button.module.css";

export default function Button({
                                   variant = "primary",
                                   size = "lg",
                                   fullWidth = false,
                                   leftIcon,
                                   rightIcon,
                                   className = "",
                                   children,
                                   type = "button",
                                   ...props
                               }) {
    const cls = [
        styles.btn,
        styles[`variant_${variant}`],
        styles[`size_${size}`],
        fullWidth ? styles.fullWidth : "",
        className,
    ]
        .filter(Boolean)
        .join(" ");

    return (
        <button type={type} className={cls} {...props}>
            {leftIcon && <span className={styles.icon}>{leftIcon}</span>}

            <span className={`${styles.text} button-text`}>
                {children}
            </span>

            {rightIcon && <span className={styles.icon}>{rightIcon}</span>}
        </button>
    );
}