import { useState, useEffect } from "react";
import {
    MainLayout,
    PageBanner,
    Toast,
    Button,
    Dropdown,
    CreateReservationModal,
    WaiterReservationCard,
    CreateOrderModal,
    EditOrderModal
} from "../../components/index.js";

import { useAuth } from "../../auth/AuthContext.jsx";
import styles from "./WaiterReservationsPage.module.css";

import calendarIcon from "../../assets/icons/calendar_bl.svg";
import clockIcon from "../../assets/icons/clock_bl.svg";
import searchIcon from "../../assets/icons/search.svg";
import chevronDownIcon from "../../assets/icons/chevron-down.svg";

const MOCK_RESERVATIONS = [
    {
        id: "res-1",
        locationId: "loc-1",
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 2,
        visitorName: "Jasmine Smith",
        waiterName: "Alex Caper",
        guestsCount: 10,
        tableNumber: 1,
        status: "Reserved",
        isCreatedByWaiter: false
    },
    {
        id: "res-2",
        locationId: "loc-1",
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T11:00:00",
        endDateTime: "2024-10-14T13:00:00",
        dishCount: 0,
        visitorName: "Alex Caper",
        waiterName: "Alex Caper",
        guestsCount: 5,
        tableNumber: 2,
        status: "InProgress",
        isCreatedByWaiter: true
    },
    {
        id: "res-3",
        locationId: "loc-1",
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T12:30:00",
        dishCount: 4,
        visitorName: "Guadalupe Rath",
        waiterName: "Sarah Connor",
        guestsCount: 10,
        tableNumber: 3,
        status: "MealsServed",
        isCreatedByWaiter: false
    },
    {
        id: "res-4",
        locationId: "loc-1",
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T12:30:00",
        dishCount: 4,
        visitorName: "Guadalupe Rath",
        waiterName: "Sarah Connor",
        guestsCount: 10,
        tableNumber: 3,
        status: "Finished",
        isCreatedByWaiter: false
    }
];

export default function WaiterReservationsPage() {
    const { auth } = useAuth();
    const [reservations, setReservations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [toast, setToast] = useState({ open: false, type: "success", title: "", message: "" });

    const [filterDate, setFilterDate] = useState("2024-10-14");
    const [filterTime, setFilterTime] = useState("10:30");
    const [filterTable, setFilterTable] = useState("any");

    const [isOrderModalOpen, setIsOrderModalOpen] = useState(false);
    const [selectedReservation, setSelectedReservation] = useState(null);
    const [isEditOrderModalOpen, setIsEditOrderModalOpen] = useState(false);

    const welcomeTitle = `Hello, ${auth?.username || "Alex Caper"} (Waiter)`;

    useEffect(() => {
        const timer = setTimeout(() => {
            setReservations(MOCK_RESERVATIONS);
            setLoading(false);
        }, 800);
        return () => clearTimeout(timer);
    }, []);

    const showToast = (type, title, message) => {
        setToast({ open: true, type, title, message });
    };

    const handleCreateReservation = async (data) => {
        console.log("Form data submitted:", data);
        showToast("success", "Success", "New Reservation has been created successfully.");
        setIsCreateModalOpen(false);
    };

    const displayDate = new Date(filterDate).toLocaleDateString("en-US", {
        month: "short", day: "numeric", year: "numeric",
    });

    const handleOpenOrderModal = (reservation) => {
        setSelectedReservation(reservation);
        setIsOrderModalOpen(true);
    };

    const handleConfirmOrder = (dishes) => {
        console.log("Order created for:", selectedReservation.id, "Dishes:", dishes);
        showToast("success", "Success", "Order has been created successfully.");
        setIsOrderModalOpen(false);
    };

    const handleSaveOrderChanges = (dishes) => {
        console.log("Changes saved:", dishes);
        setIsEditOrderModalOpen(false);
        showToast(
            "success",
            "Success",
            "All changes has been saved successfully."
        );
    };



    return (
        <MainLayout role="waiter">
            <div className={styles.page}>
                <PageBanner title={welcomeTitle} />

                <div className={styles.contentContainer}>
                    {/* Фільтри */}
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

                            <div className={styles.filterInputGroup}>
                                <img src={clockIcon} alt="" className={styles.fieldIcon} />
                                <div className={styles.nativeInputWrap}>
                                    <input
                                        type="time"
                                        value={filterTime}
                                        onChange={(e) => setFilterTime(e.target.value)}
                                        className={styles.nativeInput}
                                    />
                                    <span className={styles.inputValue}>{filterTime}</span>
                                </div>
                                <img src={chevronDownIcon} alt="" className={styles.chevronIcon} />
                            </div>

                            <Dropdown
                                options={[
                                    { value: "any", label: "Any table" },
                                    { value: "1", label: "Table 1" },
                                    { value: "2", label: "Table 2" }
                                ]}
                                value={filterTable}
                                onChange={setFilterTable}
                                className={styles.filterDropdown}
                            />

                            <button className={styles.searchBtn}>
                                <img src={searchIcon} alt="Search" />
                            </button>
                        </div>
                    </div>

                    <div className={styles.summaryRow}>
                        <p className={`${styles.summaryText} body`}>
                            You have <strong>{reservations.length} reservations</strong> for {displayDate}, {filterTime}
                        </p>
                        <Button className={styles.createBtn} onClick={() => setIsCreateModalOpen(true)}>
                            Create New Reservation
                        </Button>
                    </div>

                    {loading ? (
                        <div className={styles.stateMessage}>Loading data...</div>
                    ) : (
                        <div className={styles.grid}>
                            {reservations.map((res) => (
                                <WaiterReservationCard
                                    key={res.id}
                                    booking={res}
                                    onEdit={() => console.log("Edit Reservation Info")}

                                    onEditOrder={() => {
                                        setSelectedReservation(res);
                                        setIsEditOrderModalOpen(true);
                                    }}

                                    onCreateOrder={handleOpenOrderModal}
                                    onStart={() => showToast("success", "Started", "Reservation is now in progress")}
                                    onFinish={() => showToast("success", "Finished", "Reservation completed")}
                                    onMealServed={() => showToast("success", "Served", "Meals have been served")}
                                    onReceipt={() => showToast("success", "Receipt", "Generating receipt...")}
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

            <Toast
                {...toast}
                onClose={() => setToast(prev => ({ ...prev, open: false }))}
            />
        </MainLayout>
    );
}