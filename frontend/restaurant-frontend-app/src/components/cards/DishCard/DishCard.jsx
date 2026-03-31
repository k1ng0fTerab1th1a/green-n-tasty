import { useMemo } from "react";
import styles from "./DishCard.module.css";
import { Button } from "../../index.js";


export default function DishCard({
                                     name,
                                     price,
                                     weight,
                                     imageSrc,
                                     available = true,
                                     badgeText,
                                     onPreOrder,
                                     onClick,
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
            onClick={available ? onClick : null} // 2. Вішаємо подію (тільки якщо доступно)
            style={{ cursor: available ? 'pointer' : 'default' }} // Додай курсор для UX
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