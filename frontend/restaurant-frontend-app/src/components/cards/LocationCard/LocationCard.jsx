import styles from "./LocationCard.module.css";

import pinIcon from "../../../assets/icons/pin.svg";


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