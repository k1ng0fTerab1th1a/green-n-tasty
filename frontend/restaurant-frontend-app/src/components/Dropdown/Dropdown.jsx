import { useEffect, useMemo, useRef, useState } from "react";
import styles from "./Dropdown.module.css";

import chevronDown from "../../assets/icons/chevron-down.svg";
import chevronUp from "../../assets/icons/chevron-up.svg";

export default function Dropdown({
                                     label,
                                     value = null,
                                     onChange,
                                     options = [],
                                     placeholder = "Text",
                                     leftIcon = null,
                                     error = "",
                                     hint = "",
                                     disabled = false,
                                     className = "",
                                     controlClassName = "",
                                     valueClassName = "",
                                     menuClassName = "",
                                     itemClassName = "",
                                     onClear,
                                 }) {
    const [open, setOpen] = useState(false);
    const [activeIndex, setActiveIndex] = useState(-1);

    const rootRef = useRef(null);
    const buttonRef = useRef(null);

    const selected = useMemo(
        () => options.find((o) => o.value === value) || null,
        [options, value]
    );

    const handleClear = (e) => {
        e.stopPropagation();
        onChange?.(null);
    };

    useEffect(() => {
        if (!open) return;

        const onDoc = (e) => {
            if (!rootRef.current) return;
            if (!rootRef.current.contains(e.target)) setOpen(false);
        };

        document.addEventListener("mousedown", onDoc);
        return () => document.removeEventListener("mousedown", onDoc);
    }, [open]);

    useEffect(() => {
        if (!open) return;
        const idx = options.findIndex((o) => o.value === value && !o.disabled);
        setActiveIndex(idx >= 0 ? idx : firstEnabledIndex(options));
    }, [open, options, value]);

    const hasValue = !!selected;
    const hasError = !!error;

    const toggle = () => {
        if (disabled) return;
        setOpen((p) => !p);
    };

    const choose = (opt) => {
        if (opt.disabled) return;
        onChange?.(opt.value);
        setOpen(false);
        buttonRef.current?.focus();
    };

    const onKeyDown = (e) => {
        if (disabled) return;

        if (!open && (e.key === "Enter" || e.key === " " || e.key === "ArrowDown")) {
            e.preventDefault();
            setOpen(true);
            return;
        }

        if (!open) return;

        if (e.key === "Escape") {
            e.preventDefault();
            setOpen(false);
            return;
        }

        if (e.key === "ArrowDown") {
            e.preventDefault();
            setActiveIndex((i) => nextEnabledIndex(options, i));
            return;
        }

        if (e.key === "ArrowUp") {
            e.preventDefault();
            setActiveIndex((i) => prevEnabledIndex(options, i));
            return;
        }

        if (e.key === "Enter") {
            e.preventDefault();
            const opt = options[activeIndex];
            if (opt) choose(opt);
        }
    };

    return (
        <div className={`${styles.field} ${className}`} ref={rootRef}>
            {label ? <div className={styles.label}>{label}</div> : null}

            <button
                ref={buttonRef}
                type="button"
                className={[
                    styles.control,
                    controlClassName,
                    open ? styles.active : "",
                    hasError ? styles.error : "",
                    disabled ? styles.disabled : "",
                ].join(" ")}
                onClick={toggle}
                onKeyDown={onKeyDown}
            >
                {leftIcon && (
                    <span className={styles.leftIcon}>
                        {typeof leftIcon === "string" ? <img src={leftIcon} alt="" /> : leftIcon}
                    </span>
                )}

                <span className={[styles.value, !hasValue ? styles.placeholder : ""].join(" ")}>
                    {hasValue ? selected.label : placeholder}
                </span>

                <span className={styles.chevron}>
                    {/* Логіка: якщо є значення і ми не в стані disabled — показуємо хрестик */}
                    {hasValue && !disabled ? (
                        <span className={styles.clearBtn} onClick={handleClear} title="Clear filter">
                            ✕
                        </span>
                    ) : (
                        <img src={open ? chevronUp : chevronDown} alt="" />
                    )}
                </span>
            </button>

            {open ? (
                <div className={`${styles.menu} ${menuClassName}`} role="listbox" tabIndex={-1}>
                    {options.map((opt, idx) => {
                        const isSelected = value === opt.value;
                        const isActive = idx === activeIndex;

                        return (
                            <button
                                key={String(opt.value)}
                                type="button"
                                className={[
                                    styles.item,
                                    itemClassName,
                                    isSelected ? styles.itemSelected : "",
                                    isActive ? styles.itemActive : "",
                                    opt.disabled ? styles.itemDisabled : "",
                                ].join(" ")}
                                onClick={() => choose(opt)}
                                role="option"
                                aria-selected={isSelected}
                                disabled={opt.disabled}
                            >
                                {opt.label}
                            </button>
                        );
                    })}
                </div>
            ) : null}

            {hint ? (
                <div className={`${styles.hint} ${hasError ? styles.hintError : ""}`}>
                    {hint}
                </div>
            ) : null}
        </div>
    );
}

function firstEnabledIndex(options) {
    return options.findIndex((o) => !o.disabled);
}

function nextEnabledIndex(options, current) {
    if (!options.length) return -1;
    for (let step = 1; step <= options.length; step++) {
        const idx = (current + step) % options.length;
        if (!options[idx].disabled) return idx;
    }
    return current;
}

function prevEnabledIndex(options, current) {
    if (!options.length) return -1;
    for (let step = 1; step <= options.length; step++) {
        const idx = (current - step + options.length) % options.length;
        if (!options[idx].disabled) return idx;
    }
    return current;
}