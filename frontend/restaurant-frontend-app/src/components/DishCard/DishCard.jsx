import { useMemo } from "react";
import styles from "./DishCard.module.css";
import { Button } from "../index.js";

/**
 * DishCard
 *
 * Props:
 * - name: string
 * - price: number | string   (e.g. 17 or "17")
 * - weight: number | string  (e.g. 430 or "430")
 * - imageSrc: string
 * - available?: boolean (default true)
 * - badgeText?: string (default "On Stop" when !available)
 * - onPreOrder?: () => void
 * - currency?: string (default "$")
 * - className?: string
 */
export default function DishCard({
                                     name,
                                     price,
                                     weight,
                                     imageSrc,
                                     available = true,
                                     badgeText,
                                     onPreOrder,
                                     currency = "$",
                                     className = "",
                                 }) {
    const stopLabel = useMemo(() => {
        if (available) return "";
        return badgeText || "On Stop";
    }, [available, badgeText]);

    return (
        <div
            className={`${styles.card} ${available ? styles.available : styles.notAvailable} ${className}`}
            aria-disabled={!available}
        >
            {!available ? <div className={styles.badge}>{stopLabel}</div> : null}

            <div className={styles.imageWrap}>
                <img className={styles.image} src={imageSrc} alt={name} />
            </div>

            <div className={styles.body}>
                <div className={styles.name}>{name}</div>

                <div className={styles.metaRow}>
                    <div className={styles.price}>
                        {price} {currency}
                    </div>
                    <div className={styles.weight}>{weight} g</div>
                </div>

                {available ? (
                    <div className={styles.cta}>
                        <Button
                            variant="primary"
                            size="lg"
                            onClick={onPreOrder}
                            className={styles.preorderBtn}
                        >
                            Pre-order
                        </Button>
                    </div>
                ) : null}
            </div>
        </div>
    );
}