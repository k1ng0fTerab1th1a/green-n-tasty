import styles from "./LocationCard.module.css";

import pinIcon from "../../assets/icons/pin.svg"; // підстав свій svg

/**
 * LocationCard
 *
 * Props:
 * - title?: string (default "Locations" — якщо треба зверху на сторінці, краще НЕ тут)
 * - imageSrc: string
 * - address: string
 * - tables: number | string
 * - occupancy: number | string (0..100)
 * - onClick?: () => void
 * - className?: string
 */
export default function LocationCard({
                                         imageSrc,
                                         address,
                                         tables,
                                         occupancy,
                                         onClick,
                                         className = "",
                                     }) {
    const Tag = onClick ? "button" : "div";

    return (
        <Tag
            className={`${styles.card} ${onClick ? styles.clickable : ""} ${className}`}
            onClick={onClick}
            type={onClick ? "button" : undefined}
        >
            <div className={styles.imageWrap}>
                <img src={imageSrc} alt={address} className={styles.image} />
            </div>

            <div className={styles.body}>
                <div className={styles.addressRow}>
                    <img src={pinIcon} alt="" className={styles.pin} />
                    <div className={styles.address}>{address}</div>
                </div>

                <div className={styles.statsRow}>
                    <div className={styles.stat}>
                        <span className={styles.statLabel}>Total capacity:</span>
                        <span className={styles.statValue}>{tables} tables</span>
                    </div>

                    <div className={styles.stat}>
                        <span className={styles.statLabel}>Average occupancy:</span>
                        <span className={styles.statValue}>{occupancy}%</span>
                    </div>
                </div>
            </div>
        </Tag>
    );
}