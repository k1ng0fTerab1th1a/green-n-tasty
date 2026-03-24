import { forwardRef, useId, useMemo } from "react";
import styles from "./Input.module.css";

const Input = forwardRef(function Input(
    {
        label,
        labelRight,
        hint,
        error,
        hintVariant = "text",
        hintTone = "neutral",
        leftIcon,
        rightIcon,
        className = "",
        inputClassName = "",
        id,
        type = "text",
        value,
        defaultValue,
        ...props
    },
    ref
) {
    const autoId = useId();
    const inputId = id || autoId;

    const isFilled = useMemo(() => {
        if (value !== undefined && value !== null) return String(value).length > 0;
        if (defaultValue !== undefined && defaultValue !== null) return String(defaultValue).length > 0;
        return false;
    }, [value, defaultValue]);

    const stateClass = error ? styles.error : "";

    return (
        <div className={`${styles.field} ${className}`}>
            {label ? (
                <div className={styles.labelRow}>
                    <label
                        className={styles.label}
                        htmlFor={inputId}
                    >
                        {label}
                    </label>

                    {labelRight ? <div className={styles.labelRight}>{labelRight}</div> : null}
                </div>
            ) : null}

            <div className={`${styles.control} ${stateClass} ${isFilled ? styles.filled : ""}`}>
                {leftIcon ? <span className={styles.iconLeft}>{leftIcon}</span> : null}

                <input
                    id={inputId}
                    ref={ref}
                    type={type}
                    value={value}
                    defaultValue={defaultValue}
                    className={`${styles.input} ${inputClassName}`}
                    aria-invalid={!!error}
                    aria-describedby={hint || error ? `${inputId}-hint` : undefined}
                    {...props}
                />

                {rightIcon ? <span className={styles.iconRight}>{rightIcon}</span> : null}
            </div>

            {(error || hint) ? (
                <div
                    id={`${inputId}-hint`}
                    className={`${styles.hint} ${error ? styles.hintError : ""} ${
                        !error && hint && hintVariant === "bullet" ? styles.hintBullet : ""
                    } ${
                        !error && hintTone === "success" ? styles.hintSuccess : ""
                    }`}
                >
                    {!error && hint && hintVariant === "bullet" ? (
                        <span
                            className={`${styles.hintDot} ${hintTone === "success" ? styles.hintDotSuccess : ""}`}
                        />
                    ) : null}

                    <span>{error || hint}</span>
                </div>
            ) : null}
        </div>
    );
});

export default Input;