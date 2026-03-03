import styles from "./ReviewCard.module.css";

import userIcon from "../../assets/icons/user-circle.svg"; // постав свій svg

/**
 * ReviewCard
 *
 * Props:
 * - name: string (може бути "Anna" або "User 1765")
 * - date: string (наприклад "6/8/2024" або "Oct 14, 2024")
 * - rating: number (0..5)
 * - text: string
 * - avatarSrc?: string | null
 * - anonymous?: boolean (default false) -> якщо true і name не передали, зробить "User ####"
 * - userId?: number|string (для генерації імені якщо anonymous)
 * - className?: string
 */
export default function ReviewCard({
                                       name = "",
                                       date = "",
                                       rating = 0,
                                       text = "",
                                       avatarSrc = null,
                                       anonymous = false,
                                       userId,
                                       className = "",
                                   }) {
    const displayName = (() => {
        if (name?.trim()) return name.trim();
        if (anonymous) {
            const id = userId ?? Math.floor(1000 + Math.random() * 9000);
            return `User ${id}`;
        }
        return "User";
    })();

    const safeRating = clamp(Math.round(Number(rating) || 0), 0, 5);

    return (
        <article className={`${styles.card} ${className}`}>
            <header className={styles.header}>
                <div className={styles.user}>
                    <div className={styles.avatar}>
                        {avatarSrc ? (
                            <img src={avatarSrc} alt="" className={styles.avatarImg} />
                        ) : (
                            <img src={userIcon} alt="" className={styles.avatarFallback} />
                        )}
                    </div>

                    <div className={styles.meta}>
                        <div className={styles.name}>{displayName}</div>
                        {date ? <div className={styles.date}>{date}</div> : null}
                    </div>
                </div>

                <Stars value={safeRating} />
            </header>

            <p className={styles.text}>{text}</p>
        </article>
    );
}

function Stars({ value = 0 }) {
    const stars = Array.from({ length: 5 }, (_, i) => i < value);

    return (
        <div className={styles.stars} aria-label={`${value} out of 5`}>
            {stars.map((on, i) => (
                <span key={i} className={`${styles.star} ${on ? styles.starOn : styles.starOff}`}>
          ★
        </span>
            ))}
        </div>
    );
}

function clamp(n, a, b) {
    return Math.max(a, Math.min(b, n));
}