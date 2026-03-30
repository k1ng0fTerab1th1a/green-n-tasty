import { useState } from "react";
import styles from "./TableSelector.module.css";
import chevronDown from "../../../assets/icons/chevron-down.svg";

export default function TableSelector({ value, options, onChange }) {
    const [isOpen, setIsOpen] = useState(false);
    const selectedOption = options.find(opt => opt.value === value);
    const displayLabel = selectedOption ? selectedOption.label : value || "Select Table";

    return (
        <div className={styles.wrapper}>
            <div className={styles.control} onClick={() => setIsOpen(!isOpen)}>
                <span className="body-bold">{displayLabel}</span>
                <img
                    src={chevronDown}
                    alt=""
                    className={`${styles.arrow} ${isOpen ? styles.arrowOpen : ""}`}
                />
            </div>

            {isOpen && (
                <>
                    <div className={styles.overlay} onClick={() => setIsOpen(false)} />

                    <div className={styles.menu}>
                        {options.map((opt) => (
                            <div
                                key={opt.value}
                                className={`${styles.item} ${opt.value === value ? styles.active : ""}`}
                                onClick={() => {
                                    onChange(opt.value);
                                    setIsOpen(false);
                                }}
                            >
                                {opt.label}
                            </div>
                        ))}
                    </div>
                </>
            )}
        </div>
    );
}