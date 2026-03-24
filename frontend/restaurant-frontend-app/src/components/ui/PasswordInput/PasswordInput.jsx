import { useMemo, useState } from "react";
import { Input, PasswordStrength } from "../../index.js";
import { passwordRules } from "../../../utils/passwordRules.js";
import styles from "./PasswordInput.module.css";

import eyeIcon from "../../../assets/icons/eye.svg";
import eyeOffIcon from "../../../assets/icons/eye-off.svg";

function getStrengthValue(passedCount, total, hasValue) {
    if (!hasValue) return "weak";
    if (passedCount === total) return "strong";
    if (passedCount >= Math.ceil(total * 0.6)) return "medium";
    return "weak";
}

export default function PasswordInput({
                                          label = "Password",
                                          value = "",
                                          matchValue,
                                          onChange,
                                          onBlur,
                                          error = "",
                                          hint = "",
                                          placeholder = "Enter your Password",
                                          rules = passwordRules,
                                          showChecklist = true,
                                          showStrength = true,
                                          ...props
                                      }) {
    const [show, setShow] = useState(false);

    const strValue = String(value || "");
    const hasValue = strValue.length > 0;
    const isConfirmMode = !showChecklist; // confirm поле
    const matchOk = isConfirmMode && hasValue && String(matchValue || "").length > 0 && strValue === String(matchValue || "");

    const checks = useMemo(() => {
        const map = {};
        for (const r of rules) map[r.key] = r.test(strValue);
        return map;
    }, [strValue, rules]);

    const passedCount = useMemo(() => {
        return rules.reduce((acc, r) => acc + (checks[r.key] ? 1 : 0), 0);
    }, [checks, rules]);

    const strengthValue = useMemo(() => {
        return getStrengthValue(passedCount, rules.length, hasValue);
    }, [passedCount, rules.length, hasValue]);

    const right = hasValue ? (
        <div className={styles.rightArea}>
            <button
                type="button"
                className={styles.eyeBtn}
                onClick={() => setShow((p) => !p)}
                aria-label={show ? "Hide password" : "Show password"}
            >
                <img
                    className={styles.eyeIcon}
                    src={show ? eyeOffIcon : eyeIcon}
                    alt=""
                    width={20}
                    height={20}
                />
            </button>
        </div>
    ) : null;

    return (
        <div className={styles.wrapper}>
            <Input
                type={show ? "text" : "password"}
                label={label}
                value={value}
                labelRight={showStrength && hasValue ? <PasswordStrength value={strengthValue} /> : null}
                onChange={onChange}
                onBlur={onBlur}
                placeholder={placeholder}
                error={error}
                hint={error ? "" : hint}
                hintVariant={!showChecklist ? "bullet" : "text"}
                hintTone={matchOk ? "success" : "neutral"}
                rightIcon={right}
                {...props}
            />

            {showChecklist ? (
                <ul className={styles.checklist}>
                    {rules.map((r) => {
                        const ok = checks[r.key];
                        
                        const showState = hasValue || !!error;

                        const itemClass = showState
                            ? ok
                                ? styles.itemOk
                                : error
                                    ? styles.itemBad
                                    : styles.itemNeutral
                            : styles.itemNeutral;

                        return (
                            <li key={r.key} className={`${styles.item} ${itemClass}`}>
                                <span className={styles.bullet} />
                                <span className={styles.itemText}>{r.label}</span>
                            </li>
                        );
                    })}
                </ul>
            ) : null}
        </div>
    );
}