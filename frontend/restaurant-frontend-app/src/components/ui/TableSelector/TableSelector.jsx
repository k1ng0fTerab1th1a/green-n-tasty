import { useState } from "react";
import styles from "./TableSelector.module.css";
import chevronDown from "../../../assets/icons/chevron-down.svg";

export default function TableSelector({ value, options, onChange }) {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div className={styles.wrapper}>
            <div className={styles.control} onClick={() => setIsOpen(!isOpen)}>
                <span className="body-bold">{value}</span>
                <img src={chevronDown} alt="" className={styles.arrow} />
            </div>

            {isOpen && (
                <div className={styles.menu}>
                    {options.map((opt) => (
                        <div
                            key={opt.value}
                            className={styles.item}
                            onClick={() => {
                                onChange(opt.value);
                                setIsOpen(false);
                            }}
                        >
                            {opt.label}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}