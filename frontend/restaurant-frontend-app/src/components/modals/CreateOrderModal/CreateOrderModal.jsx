import { useState, useEffect } from "react";
import { Modal, Button, SearchDropdown, SelectedDishCard } from "../../index.js";
import { getMenuDishes } from "../../../services/dishes";
import styles from "./CreateOrderModal.module.css";

export default function CreateOrderModal({ isOpen, onClose, reservation, onConfirm }) {
    const [menuItems, setMenuItems] = useState([]);
    const [selectedDishes, setSelectedDishes] = useState([]);

    useEffect(() => {
        if (isOpen) {
            getMenuDishes().then(res => {
                if (res.isSuccess) setMenuItems(res.data);
            });
        }
    }, [isOpen]);

    const handleSelectDish = (dish) => {
        const exists = selectedDishes.find(d => d.id === dish.id);
        if (exists) {
            handleUpdateQuantity(dish.id, exists.quantity + 1);
        } else {
            setSelectedDishes([...selectedDishes, { ...dish, quantity: 1 }]);
        }
    };

    const handleRemoveDish = (dishId) => {
        setSelectedDishes(selectedDishes.filter(d => d.id !== dishId));
    };

    const handleUpdateQuantity = (dishId, newQty) => {
        if (newQty < 1) return;
        setSelectedDishes(selectedDishes.map(d =>
            d.id === dishId ? { ...d, quantity: newQty } : d
        ));
    };

    if (!reservation) return null;

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="Create an Order">
            <div className={styles.container}>
                <div className={styles.infoBlock}>
                    <p className="body">Reservation <strong>#{reservation.id}</strong></p>
                    <p className="body">Waiter <strong>{reservation.waiterName || "Alex Caper"}</strong></p>
                    <p className="body">Customer <strong>{reservation.visitorName || reservation.customerName}</strong></p>
                </div>

                <div className={styles.dishesSection}>
                    <div className={styles.dishesHeader}>
                        <h3 className="h3">Dishes</h3>
                        <p className="body" style={{ color: '#000000' }}>
                            You have added <strong>{selectedDishes.length} dishes</strong>
                        </p>
                    </div>

                    <div className={styles.selectedList}>
                        {selectedDishes.map(dish => (
                            <SelectedDishCard
                                key={dish.id}
                                dish={dish}
                                onRemove={() => handleRemoveDish(dish.id)}
                                onUpdateQuantity={(qty) => handleUpdateQuantity(dish.id, qty)}
                            />
                        ))}
                    </div>

                    <SearchDropdown
                        items={menuItems}
                        searchKey="name"
                        label="Dishes Name"
                        placeholder="Enter Dishes Name"
                        hint="e.g. Fresh Strawberry Mint Salad"
                        onSelect={handleSelectDish}
                    />
                </div>

                <Button
                    className={styles.submitBtn}
                    fullWidth
                    onClick={() => onConfirm(selectedDishes)}
                >
                    Create an Order
                </Button>
            </div>
        </Modal>
    );
}