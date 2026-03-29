import { useState, useEffect } from "react";
import {
    MainLayout,
    PageBanner,
    Toast,
    Button,
    WaiterReservationCard,
    Dropdown
} from "../../components/index.js";
import { useAuth } from "../../auth/AuthContext.jsx";
import styles from "./WaiterReservationsPage.module.css";

import calendarIcon from "../../assets/icons/calendar_bl.svg";
import clockIcon from "../../assets/icons/clock_bl.svg";
import searchIcon from "../../assets/icons/search.svg";
import chevronDownIcon from "../../assets/icons/chevron-down.svg";

const MOCK_RESERVATIONS = [
    {
        id: 1,
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 2,
        customerName: "Jasmine Smith",
        guestsCount: 10,
        tableNumber: 1,
        status: "Pre-order"
    },
    {
        id: 2,
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 0,
        customerName: "Alex Caper (Visitor 1)",
        guestsCount: 5,
        tableNumber: 2,
        status: ""
    },
    {
        id: 3,
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 2,
        customerName: "Guadalupe Rath",
        guestsCount: 10,
        tableNumber: 3,
        status: "Pre-order"
    },
    {
        id: 4,
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 0,
        customerName: "John Doe",
        guestsCount: 4,
        tableNumber: 4,
        status: ""
    },
    {
        id: 5,
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 2,
        customerName: "Marie Curie",
        guestsCount: 2,
        tableNumber: 5,
        status: "Pre-order"
    },
    {
        id: 6,
        locationAddress: "48 Rustaveli Avenue",
        startDateTime: "2024-10-14T10:30:00",
        endDateTime: "2024-10-14T12:00:00",
        dishCount: 0,
        customerName: "Steve Jobs",
        guestsCount: 8,
        tableNumber: 6,
        status: ""
    }
];

export default function WaiterReservationsPage() {
    const { auth } = useAuth();
    const [reservations, setReservations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [toast, setToast] = useState({ open: false, type: "success", title: "", message: "" });

    const [filterDate, setFilterDate] = useState("2024-10-12");
    const [filterTime, setFilterTime] = useState("10:30"); // Формат 24г
    const [filterTable, setFilterTable] = useState("any");

    const welcomeTitle = `Hello, ${auth?.username || "Alex Caper"} (Waiter)`;

    useEffect(() => {
        const timer = setTimeout(() => {
            setReservations(MOCK_RESERVATIONS);
            setLoading(false);
        }, 800);
        return () => clearTimeout(timer);
    }, []);

    const tableOptions = [
        { value: "any", label: "Any table" },
        { value: "1", label: "Table 1" },
        { value: "2", label: "Table 2" },
        { value: "3", label: "Table 3" }
    ];

    const displayDate = new Date(filterDate).toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
    });

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
                                options={tableOptions}
                                value={filterTable}
                                onChange={setFilterTable}
                                className={styles.filterDropdown}
                                controlClassName={styles.dropdownControl}
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
                        <Button className={styles.createBtn} size="lg">
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
                                    booking={{
                                        ...res,
                                        address: res.locationAddress,
                                        date: "Oct 14, 2024",
                                        time: "10:30 - 12:00",
                                        guests: res.guestsCount,
                                        table: `Table ${res.tableNumber}`
                                    }}
                                    onCancel={() => console.log("Cancel", res.id)}
                                    onEdit={() => console.log("Edit", res.id)}
                                />
                            ))}
                        </div>
                    )}
                </div>
            </div>

            <Toast
                {...toast}
                onClose={() => setToast(prev => ({ ...prev, open: false }))}
            />
        </MainLayout>
    );
}