import Modal from "../Modal/Modal";
import styles from "./DishDetailsModal.module.css";

export default function DishDetailsModal({ isOpen, onClose, dish }) {
    if (!dish) return null;

    return (
        <Modal isOpen={isOpen} onClose={onClose}>
            <div className={styles.container}>
                <div className={styles.imageWrapper}>
                    <img src={dish.imageUrl} alt={dish.name} className={styles.image} />
                </div>

                <h3 className={styles.name}>{dish.name}</h3>

                <p className={styles.description}>{dish.description}</p>

                <div className={styles.nutrition}>
                    <p><strong>Calories:</strong> ~{dish.calories} kcal</p>
                    <p><strong>Protein:</strong> {dish.proteins}g</p>
                    <p><strong>Fats:</strong> {dish.fats}g</p>
                    <p><strong>Carbohydrates:</strong> {dish.carbohydrates}g</p>
                    <p className={styles.vitamins}>
                        <strong>Vitamins and minerals:</strong> {dish.vitamins}
                    </p>
                </div>

                <div className={styles.footer}>
                    <h3>{dish.price} $</h3>
                    <h3>{dish.weight} g</h3>
                </div>
            </div>
        </Modal>
    );
}