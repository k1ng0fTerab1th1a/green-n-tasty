import { useEffect } from "react";
import styles from "./Modal.module.css";

export default function Modal({ isOpen, onClose, title, subtitle, children }) {
    useEffect(() => {
        const handleEscape = (e) => {
            if (e.key === "Escape") onClose();
        };

        if (isOpen) {
            window.addEventListener("keydown", handleEscape);
            document.body.style.overflow = "hidden";
        }

        return () => {
            window.removeEventListener("keydown", handleEscape);
            document.body.style.overflow = "unset";
        };
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <button
                    className={styles.closeButton}
                    onClick={onClose}
                    aria-label="Close"
                >
                    &times;
                </button>
                <div className={styles.header}>
                    <h2 className="h2">{title}</h2>
                    {subtitle && (
                        <p className="body" style={{ color: "var(--neutral-600)", marginTop: "8px" }}>
                            {subtitle}
                        </p>
                    )}
                </div>
                <div className={styles.content}>
                    {children}
                </div>
            </div>
        </div>
    );
}