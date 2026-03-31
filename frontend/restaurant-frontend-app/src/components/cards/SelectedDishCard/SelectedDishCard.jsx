import styles from "./SelectedDishCard.module.css";
import trashIcon from "../../../assets/icons/trash.svg";

export default function SelectedDishCard({ dish, onRemove, onUpdateQuantity }) {
    const { name, description, price, imageUrl, quantity } = dish;

    return (
        <div className={styles.card}>
            <div className={styles.content}>
                <img src={imageUrl} alt={name} className={styles.dishImage} />

                <div className={styles.info}>
                    <div className={styles.header}>
                        <h4 className="body-bold">{name}</h4>
                        <button className={styles.deleteBtn} onClick={onRemove}>
                            <img src={trashIcon} alt="Delete" />
                        </button>
                    </div>

                    <p className={`caption ${styles.description}`}>{description}</p>

                    <div className={styles.footer}>
                        <div className={styles.counter}>
                            <button
                                className={`${styles.countBtn} ${quantity > 1 ? styles.active : ""}`}
                                onClick={() => onUpdateQuantity(quantity - 1)}
                                disabled={quantity <= 1}
                            >
                                —
                            </button>
                            <span className="body-bold">{quantity}</span>
                            <button
                                className={`${styles.countBtn} ${styles.active}`}
                                onClick={() => onUpdateQuantity(quantity + 1)}
                            >
                                +
                            </button>
                        </div>
                        <span className="body-bold">{price * quantity} $</span>
                    </div>
                </div>
            </div>
        </div>
    );
}