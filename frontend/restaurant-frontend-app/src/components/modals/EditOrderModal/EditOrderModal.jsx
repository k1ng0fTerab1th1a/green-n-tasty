import { useState, useEffect } from "react";
import { Modal, Button, SelectedDishCard, Input } from "../../index.js";
import { searchDishes, getDishById } from "../../../services/dishes";
import styles from "../CreateOrderModal/CreateOrderModal.module.css";

export default function EditOrderModal({ isOpen, onClose, reservation, onSave }) {
    const [searchQuery, setSearchQuery] = useState("");
    const [searchResults, setSearchResults] = useState([]);
    const [isSearching, setIsSearching] = useState(false);
    const [selectedDishes, setSelectedDishes] = useState([]);

    useEffect(() => {
        if (isOpen && reservation?.dishes) {
            setSelectedDishes(reservation.dishes);
        }

        if (!isOpen) {
            setSearchQuery("");
            setSearchResults([]);
            setSelectedDishes([]);
        }
    }, [isOpen, reservation]);

    useEffect(() => {
        const trimmed = searchQuery.trim();

        if (!trimmed) {
            setSearchResults([]);
            setIsSearching(false);
            return;
        }

        setIsSearching(true);

        const delayDebounceFn = setTimeout(async () => {
            const res = await searchDishes(trimmed);
            setSearchResults(res.isSuccess ? (res.data || []) : []);
            setIsSearching(false);
        }, 600);

        return () => clearTimeout(delayDebounceFn);
    }, [searchQuery]);

    const handleSearchChange = (e) => {
        setSearchQuery(e.target.value);
    };

    const handleSelectDish = async (shortDish) => {
        const exists = selectedDishes.find((d) => d.id === shortDish.id);
        if (exists) {
            handleUpdateQuantity(shortDish.id, exists.quantity + 1);
            setSearchResults([]);
            setSearchQuery("");
            return;
        }

        const res = await getDishById(shortDish.id);
        let fullDish;

        if (res.isSuccess && res.data) {
            const d = res.data;
            fullDish = {
                id: d.id,
                name: d.name,
                description: d.description,
                price: d.price,
                imageUrl: d.imageUrl,
            };
        } else {
            fullDish = {
                id: shortDish.id,
                name: shortDish.name,
                description: "",
                price: 0,
                imageUrl: "",
            };
        }

        setSelectedDishes([...selectedDishes, { ...fullDish, quantity: 1 }]);
        setSearchResults([]);
        setSearchQuery("");
    };

    const handleRemoveDish = (dishId) => {
        setSelectedDishes(selectedDishes.filter((d) => d.id !== dishId));
    };

    const handleUpdateQuantity = (dishId, newQty) => {
        if (newQty < 1) return;
        setSelectedDishes(
            selectedDishes.map((d) =>
                d.id === dishId ? { ...d, quantity: newQty } : d
            )
        );
    };

    if (!reservation) return null;

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="Edit an Order">
            <div className={styles.container}>
                <div className={styles.infoBlock}>
                    <p className="body">
                        Reservation <strong>#{reservation.id}</strong>
                    </p>
                    <p className="body">
                        Waiter <strong>{reservation.waiterName || "Alex Caper"}</strong>
                    </p>
                    <p className="body">
                        Customer{" "}
                        <strong>
                            {reservation.visitorName || reservation.customerName}
                        </strong>
                    </p>
                </div>

                <div className={styles.dishesSection}>
                    <div className={styles.dishesHeader}>
                        <h3 className="h3">Dishes</h3>
                        <p className="body" style={{ color: "#000000" }}>
                            You have added <strong>{selectedDishes.length} dishes</strong>
                        </p>
                    </div>

                    <div className={styles.selectedList}>
                        {selectedDishes.map((dish) => (
                            <SelectedDishCard
                                key={dish.id}
                                dish={{
                                    ...dish,
                                    totalPrice: (dish.price * dish.quantity).toFixed(2)
                                }}
                                onRemove={() => handleRemoveDish(dish.id)}
                                onUpdateQuantity={(qty) =>
                                    handleUpdateQuantity(dish.id, qty)
                                }
                            />
                        ))}
                    </div>

                    <div className={styles.searchFieldWrapper}>
                        <Input
                            label="Dishes Name"
                            placeholder="Enter Dishes Name"
                            hint="e.g. Fresh Strawberry Mint Salad"
                            value={searchQuery}
                            onChange={handleSearchChange}
                        />

                        <div className={styles.suggestionsWrapper}>
                            {isSearching && (
                                <p className="caption" style={{ color: "#666" }}>
                                    Searching...
                                </p>
                            )}

                            {!isSearching && searchQuery && searchResults.length === 0 && (
                                <p className="caption" style={{ textAlign: "center", color: "#666" }}>
                                    No dishes found
                                </p>
                            )}

                            {!isSearching && searchResults.length > 0 && (
                                <div className={styles.suggestionsList}>
                                    {searchResults.map((item, index) => (
                                        <button
                                            key={item.id || index}
                                            type="button"
                                            onClick={() => handleSelectDish(item)}
                                            className={styles.suggestionItem}
                                        >
                                            <span className="body-bold">{item.name}</span>
                                            {item.dishType && (
                                                <span
                                                    className="caption"
                                                    style={{ marginLeft: 8, color: "#666" }}
                                                >
                                                        · {item.dishType}
                                                    </span>
                                            )}
                                        </button>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <Button
                    className={styles.submitBtn}
                    fullWidth
                    onClick={() => onSave(selectedDishes)}
                >
                    Save Changes
                </Button>
            </div>
        </Modal>
    );
}