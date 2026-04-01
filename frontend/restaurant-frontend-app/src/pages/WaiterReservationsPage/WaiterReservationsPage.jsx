import {useState, useEffect, useCallback} from "react";
import {
    MainLayout,
    PageBanner,
    Toast,
    Button,
    Dropdown,
    CreateReservationModal,
    WaiterReservationCard,
    CreateOrderModal,
    EditOrderModal,
    EditReservationModal   // ⬅ ДОДАЛИ
} from "../../components/index.js";

import { useAuth } from "../../auth/AuthContext.jsx";
import {
    getWaiterReservations,
    createWaiterReservation,
    updateReservation,
    deleteReservation,
    startReservation,
    setMealsServed,
    finishReservation,
    getReservationReceipt
} from "../../services/reservations";
import {
    createOrder,
    getOrderByReservation,
    addDishToOrder,
    removeDishFromOrder,
} from "../../services/orders";
import styles from "./WaiterReservationsPage.module.css";

import calendarIcon from "../../assets/icons/calendar_bl.svg";
import clockIcon from "../../assets/icons/clock_bl.svg";
import searchIcon from "../../assets/icons/search.svg";
import chevronDownIcon from "../../assets/icons/chevron-down.svg";

export default function WaiterReservationsPage() {
    const { auth } = useAuth();
    const [reservations, setReservations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [toast, setToast] = useState({ open: false, type: "success", title: "", message: "" });

    const [filterDate, setFilterDate] = useState(new Date().toISOString().split('T')[0]);
    const [filterTime, setFilterTime] = useState("12:00");
    const [filterTable, setFilterTable] = useState("any");

    const [isOrderModalOpen, setIsOrderModalOpen] = useState(false);
    const [selectedReservation, setSelectedReservation] = useState(null);
    const [isEditOrderModalOpen, setIsEditOrderModalOpen] = useState(false);

    const [isEditReservationModalOpen, setIsEditReservationModalOpen] = useState(false);
    const [reservationForEdit, setReservationForEdit] = useState(null);

    const [orderOperationId, setOrderOperationId] = useState(null);

    const welcomeTitle = `Hello, ${auth?.username || "Waiter"}`;

    const generateOperationId = () => {
        if (typeof crypto !== "undefined" && crypto.randomUUID) {
            return crypto.randomUUID();
        }
        return (
            Date.now().toString(36) +
            Math.random().toString(36).substring(2, 10)
        );
    };


    const ensureTimeFormat = (timeStr) => {
        if (!timeStr) return "00:00";
        const [hours, minutes] = timeStr.split(':');
        return `${hours.padStart(2, '0')}:${minutes.padStart(2, '0')}`;
    };

    const loadData = useCallback(async () => {
        try {
            setLoading(true);
            const result = await getWaiterReservations(filterDate);
            if (result.isSuccess) {
                setReservations(result.data || []);
            } else {
                showToast("error", "Error", result.message || "Failed to load data");
            }
        } catch (error) {
            showToast("error", "Error", "Something went wrong while fetching reservations");
        } finally {
            setLoading(false);
        }
    }, [filterDate]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    const showToast = (type, title, message) => {
        setToast({ open: true, type, title, message });
    };

    const handleCreateReservation = async (formData) => {
        const payload = {
            locationId: "location-1",
            tableNumber: formData.tableNumber,
            date: formData.date,
            timeFrom: ensureTimeFormat(formData.timeFrom),
            timeTo: ensureTimeFormat(formData.timeTo),
            guestsCount: formData.guestsCount,
        };

        if (formData.customerType === 'existing') {
            payload.customerId = formData.customerId;
        } else {
            payload.customerId = null;
            payload.visitorName = formData.visitorName;
        }

        console.log("Final Payload to server:", payload);

        const result = await createWaiterReservation(payload);

        if (result.isSuccess) {
            showToast("success", "Success", "Reservation created successfully");
            setIsCreateModalOpen(false);
            if (formData.date === filterDate) {
                loadData();
            }
        } else {
            showToast("error", "Failed", result.message || "Validation Error");
        }
    };

    const displayDate = new Date(filterDate).toLocaleDateString("en-US", {
        month: "short", day: "numeric", year: "numeric",
    });

    const statusPriority = {
        finished: 0,
        inprogress: 1,
        reserved: 2,
        cancelled: 3,
    };

    const getStatusKey = (status) =>
        status ? status.toString().toLowerCase().replace(/\s+/g, "") : "";

    const sortedReservations = [...reservations].sort((a, b) => {
        const aKey = getStatusKey(a.status);
        const bKey = getStatusKey(b.status);

        const aPr = statusPriority[aKey] ?? 999;
        const bPr = statusPriority[bKey] ?? 999;

        if (aPr !== bPr) return aPr - bPr;

        const aStart = a.startDateTime || "";
        const bStart = b.startDateTime || "";
        return aStart.localeCompare(bStart);
    });

    const handleOpenEditOrder = async (reservation) => {
        const result = await getOrderByReservation(reservation.id);

        if (!result.isSuccess || !result.data) {
            showToast("error", "Failed", result.message || "Failed to load order details.");
            return;
        }

        const order = result.data;

        const mappedDishes = (order.dishes || []).map((d) => ({
            id: d.dishId,
            name: d.name,
            description: d.description,
            price: d.priceAtOrder,
            imageUrl: d.photoUrl,
            quantity: d.quantity,
        }));

        setSelectedReservation({
            ...reservation,
            orderId: order.id,
            dishes: mappedDishes,
            totalAmount: order.totalAmount,
        });

        setIsEditOrderModalOpen(true);
    };

    const handleConfirmOrder = async (dishes) => {
        if (!selectedReservation) {
            showToast("error", "Error", "No reservation selected for order.");
            return;
        }

        const result = await createOrder(selectedReservation.id, dishes);

        if (result.isSuccess) {
            showToast("success", "Success", "Order has been created successfully.");
            setIsOrderModalOpen(false);
            // оновити список, щоб відобразити dishCount, статус тощо
            await loadData();
        } else {
            showToast("error", "Failed", result.message || "Failed to create order.");
        }
    };

    const handleSaveOrderChanges = async (updatedDishes) => {
        if (!selectedReservation) {
            showToast("error", "Error", "No reservation selected for order.");
            return;
        }

        const reservationId = selectedReservation.id;
        const originalDishes = selectedReservation.dishes || [];

        const origById = new Map(originalDishes.map((d) => [d.id, d]));
        const updatedById = new Map(updatedDishes.map((d) => [d.id, d]));

        const additions = [];
        const removals = [];

        updatedDishes.forEach((upd) => {
            const orig = origById.get(upd.id);
            if (!orig) {
                if (upd.quantity > 0) {
                    additions.push({ dishId: upd.id, quantity: upd.quantity });
                }
                return;
            }

            if (upd.quantity > orig.quantity) {
                additions.push({
                    dishId: upd.id,
                    quantity: upd.quantity - orig.quantity,
                });
            } else if (upd.quantity < orig.quantity) {
                removals.push({
                    dishId: upd.id,
                    quantity: orig.quantity - upd.quantity,
                });
            }
        });

        originalDishes.forEach((orig) => {
            if (!updatedById.has(orig.id) && orig.quantity > 0) {
                removals.push({ dishId: orig.id, quantity: orig.quantity });
            }
        });

        if (additions.length === 0 && removals.length === 0) {
            setIsEditOrderModalOpen(false);
            showToast("success", "Saved", "No changes to apply.");
            return;
        }

        try {
            // КОЖЕН запит має СВІЙ operationId
            for (const add of additions) {
                const opId = generateOperationId();
                const res = await addDishToOrder(reservationId, {
                    operationId: opId,
                    dishId: add.dishId,
                    quantity: add.quantity,
                });
                if (!res.isSuccess) {
                    throw new Error(res.message || "Failed to add dish.");
                }
            }

            for (const rem of removals) {
                const opId = generateOperationId();
                const res = await removeDishFromOrder(reservationId, {
                    operationId: opId,
                    dishId: rem.dishId,
                    quantity: rem.quantity,
                });
                if (!res.isSuccess) {
                    throw new Error(res.message || "Failed to remove dish.");
                }
            }

            setIsEditOrderModalOpen(false);
            showToast("success", "Success", "Order has been updated successfully.");
            await loadData();
        } catch (error) {
            console.error("Failed to update order:", error);
            showToast(
                "error",
                "Failed",
                error.message || "Failed to update order. Please try again."
            );
        }
    };

    const handleUpdateReservation = async (updateData) => {
        const result = await updateReservation(updateData);

        if (result.isSuccess) {
            showToast("success", "Success", "Reservation updated successfully");
            setIsEditReservationModalOpen(false);
            loadData();
        } else {
            showToast("error", "Failed", result.message || "Failed to update reservation");
        }
    };

    const handleStartReservation = async (id) => {
        const result = await startReservation(id);
        if (result.isSuccess) {
            showToast("success", "Started", "Reservation is now in progress.");
            loadData();
        } else {
            showToast("error", "Failed", result.message || "Failed to start reservation");
        }
    };

    const handleMealsServed = async (id) => {
        const result = await setMealsServed(id);
        if (result.isSuccess) {
            showToast("success", "Updated", "Meals marked as served.");
            loadData();
        } else {
            showToast("error", "Failed", result.message || "Failed to update meals status");
        }
    };

    const handleFinishReservation = async (id) => {
        const result = await finishReservation(id);
        if (result.isSuccess) {
            showToast("success", "Finished", "Reservation has been finished.");
            loadData();
        } else {
            showToast("error", "Failed", result.message || "Failed to finish reservation");
        }
    };

    const handleCancelReservation = async (id) => {
        const result = await deleteReservation(id);
        if (result.isSuccess) {
            showToast("success", "Cancelled", "Reservation has been cancelled.");
            loadData();
        } else {
            showToast("error", "Failed", result.message || "Failed to cancel reservation");
        }
    };



    // const handleReceipt = async (id) => {
    //     const result = await getReservationReceipt(id);
    //
    //     if (result.isSuccess && result.data) {
    //         // Можна просто показати текст у toast
    //         showToast("success", "Receipt", result.data);
    //
    //     } else {
    //         showToast("error", "Failed", result.message || "Failed to generate receipt");
    //     }
    // };

    return (
        <MainLayout role="waiter">
            <div className={styles.page}>
                <PageBanner title={welcomeTitle} />
                <div className={styles.contentContainer}>
                    <div className={styles.filtersWrapper}>
                        <div className={styles.filtersBar}>
                            <div className={styles.filterInputGroup}>
                                <img src={calendarIcon} alt="" className={styles.fieldIcon} />
                                <div className={styles.nativeInputWrap}>
                                    <input
                                        type="date"
                                        value={filterDate}
                                        onChange={(e) => setFilterDate(e.target.value)}
                                        className={styles.nativeInput}
                                    />
                                    <span className={styles.inputValue}>{displayDate}</span>
                                </div>
                                <img src={chevronDownIcon} alt="" className={styles.chevronIcon} />
                            </div>

                            {/*<div className={styles.filterInputGroup}>*/}
                            {/*    <img src={clockIcon} alt="" className={styles.fieldIcon} />*/}
                            {/*    <div className={styles.nativeInputWrap}>*/}
                            {/*        <input*/}
                            {/*            type="time"*/}
                            {/*            value={filterTime}*/}
                            {/*            onChange={(e) => setFilterTime(e.target.value)}*/}
                            {/*            className={styles.nativeInput}*/}
                            {/*        />*/}
                            {/*        <span className={styles.inputValue}>{filterTime}</span>*/}
                            {/*    </div>*/}
                            {/*    <img src={chevronDownIcon} alt="" className={styles.chevronIcon} />*/}
                            {/*</div>*/}

                            {/*<Dropdown*/}
                            {/*    options={[*/}
                            {/*        { value: "any", label: "Any table" },*/}
                            {/*        { value: "1", label: "Table 1" },*/}
                            {/*        { value: "2", label: "Table 2" }*/}
                            {/*    ]}*/}
                            {/*    value={filterTable}*/}
                            {/*    onChange={setFilterTable}*/}
                            {/*    className={styles.filterDropdown}*/}
                            {/*/>*/}

                            <button className={styles.searchBtn} onClick={loadData}>
                                <img src={searchIcon} alt="Search" />
                            </button>
                        </div>
                    </div>

                    <div className={styles.summaryRow}>
                        <p className={`${styles.summaryText} body`}>
                            You have <strong>{reservations.length} reservations</strong> for {displayDate}
                        </p>
                        <Button className={styles.createBtn} onClick={() => setIsCreateModalOpen(true)}>
                            Create New Reservation
                        </Button>
                    </div>

                    {loading ? (
                        <div className={styles.stateMessage}>Loading data...</div>
                    ) : (
                        <div className={styles.grid}>
                            {sortedReservations.map((res) => (
                                <WaiterReservationCard
                                    key={res.id}
                                    booking={res}
                                    onCancel={() => handleCancelReservation(res.id)}
                                    onEdit={() => {
                                        const date = res.date || (res.startDateTime ? res.startDateTime.split("T")[0] : "");
                                        const timeFrom = res.timeFrom || (res.startDateTime ? res.startDateTime.slice(11, 16) : "");
                                        const timeTo = res.timeTo || (res.endDateTime ? res.endDateTime.slice(11, 16) : "");

                                        setReservationForEdit({
                                            id: res.id,
                                            date,
                                            guestNumber: res.guestsCount,
                                            tableNumber: res.tableNumber,
                                            timeFrom,
                                            timeTo,
                                            customerId: res.customerId,
                                            customerName: res.customerName,
                                            visitorName: res.visitorName
                                        });
                                        setIsEditReservationModalOpen(true);
                                    }}
                                    onEditOrder={() => handleOpenEditOrder(res)}   // ⬅ ТУТ
                                    onCreateOrder={() => {
                                        setSelectedReservation(res);
                                        setIsOrderModalOpen(true);
                                    }}
                                    onStart={() => handleStartReservation(res.id)}
                                    onFinish={() => handleFinishReservation(res.id)}
                                    onMealServed={() => handleMealsServed(res.id)}
                                    // onReceipt={() => handleReceipt(res.id)}
                                />
                            ))}
                        </div>
                    )}
                </div>
            </div>

            <CreateReservationModal
                isOpen={isCreateModalOpen}
                onClose={() => setIsCreateModalOpen(false)}
                onConfirm={handleCreateReservation}
            />

            <CreateOrderModal
                isOpen={isOrderModalOpen}
                onClose={() => setIsOrderModalOpen(false)}
                reservation={selectedReservation}
                onConfirm={handleConfirmOrder}
            />

            <EditOrderModal
                isOpen={isEditOrderModalOpen}
                onClose={() => setIsEditOrderModalOpen(false)}
                reservation={selectedReservation}
                onSave={handleSaveOrderChanges}
            />

            {reservationForEdit && (
                <EditReservationModal
                    key={reservationForEdit.id}
                    isOpen={isEditReservationModalOpen}
                    onClose={() => setIsEditReservationModalOpen(false)}
                    reservation={reservationForEdit}
                    onConfirm={handleUpdateReservation}
                />
            )}


            <Toast
                {...toast}
                onClose={() => setToast(prev => ({ ...prev, open: false }))}
            />
        </MainLayout>
    );
}