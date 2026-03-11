import { useMemo, useState, useEffect } from "react";
import {
    MainLayout,
    TableCard,
    SearchPanel,
    NavigationLink,
    ReservationForm
} from "../../components/index.js";
import styles from "./SearchPage.module.css";
import { getAvailableTables } from "../../services/bookings";
import { getLocations } from "../../services/locations";
import heroImage from "../../assets/images/main-hero.jpg";

export default function SearchPage() {
    const [locations, setLocations] = useState([]);
    const [tables, setTables] = useState([]);
    const [loading, setLoading] = useState(false);
    const [pageError, setPageError] = useState("");

    const [isReservationModalOpen, setIsReservationModalOpen] = useState(false);
    const [selectedTable, setSelectedTable] = useState(null);
    const [activeSlot, setActiveSlot] = useState("");

    const [filters, setFilters] = useState({
        locationId: "",
        date: new Date().toISOString().split('T')[0],
        time: "",
        guests: 1,
    });

    useEffect(() => {
        let isMounted = true;
        async function fetchInitialData() {
            try {
                const locationsData = await getLocations();
                if (!isMounted) return;
                setLocations(locationsData || []);
                fetchAvailableTables(filters);
            } catch (err) {
                console.error("Failed to fetch locations:", err);
            }
        }
        fetchInitialData();
        return () => { isMounted = false; };
    }, []);

    const fetchAvailableTables = async (searchParams) => {
        try {
            setLoading(true);
            setPageError("");
            const response = await getAvailableTables(searchParams);
            setTables(Array.isArray(response.data) ? response.data : []);
        } catch (err) {
            setPageError("Failed to load tables. Please try again.");
            setTables([]);
        } finally {
            setLoading(false);
        }
    };

    const handleSearch = (data) => {
        setFilters(data);
        fetchAvailableTables(data);
    };

    const handleReservationSuccess = () => {
        alert("Стіл успішно заброньовано!");
        setIsReservationModalOpen(false);
        fetchAvailableTables(filters);
    };

    const formatTime = (isoString) => {
        if (!isoString) return "";
        const date = new Date(isoString);
        if (isNaN(date.getTime())) return "";
        return new Intl.DateTimeFormat("en-US", {
            hour: "numeric",
            minute: "2-digit",
            hour12: true,
            timeZone: "Asia/Tbilisi"
        }).format(date).toLowerCase();
    };

    const hero = (
        <section
            className={styles.hero}
            style={{ backgroundImage: `url(${heroImage})` }}
        >
            <div className={styles.overlay} />

            <div className={styles.heroInner}>
                <div className={styles.heroContent}>
                    <p className={styles.heroKicker}>Green & Tasty Restaurants</p>
                    <h1 className={styles.heroTitle}>Book a Table</h1>

                    <div className={styles.searchPanelWrap}>
                        <SearchPanel
                            locations={locations}
                            dates={[
                                { value: new Date().toISOString().split('T')[0], label: "Today" },
                                { value: "2026-03-12", label: "Mar 12, 2026" },
                                { value: "2026-03-13", label: "Mar 13, 2026" },
                            ]}
                            times={[
                                { value: "10:30", label: "10:30 a.m." },
                                { value: "12:15", label: "12:15 p.m." },
                                { value: "13:00", label: "1:00 p.m." },
                                { value: "14:45", label: "2:45 p.m." },
                                { value: "17:30", label: "5:30 p.m." },
                            ]}
                            selectedLocation={filters.locationId}
                            selectedDate={filters.date}
                            selectedTime={filters.time}
                            guests={filters.guests}
                            onLocationChange={(value) =>
                                setFilters((prev) => ({ ...prev, locationId: value }))
                            }
                            onDateChange={(value) =>
                                setFilters((prev) => ({ ...prev, date: value }))
                            }
                            onTimeChange={(value) =>
                                setFilters((prev) => ({ ...prev, time: value }))
                            }
                            onGuestsChange={(value) =>
                                setFilters((prev) => ({ ...prev, guests: value }))
                            }
                            onSubmit={handleSearch}
                        />
                    </div>
                </div>
            </div>
        </section>
    );

    return (
        <MainLayout hero={hero}>
            <section className={styles.page}>
                <section className={styles.resultsSection}>
                    <div className={styles.resultsHeader}>
                        <h2 className={styles.resultsTitle}>
                            {loading ? "Searching..." : `${tables.length} tables available`}
                        </h2>
                    </div>

                    <div className={styles.resultsGrid}>
                        {tables.map((table) => (
                            <TableCard
                                key={`${table.locationId}-${table.tableNumber}`}
                                id={table.id}
                                locationId={table.locationId}
                                image={heroImage}
                                location={table.locationAddress}
                                tableNumber={table.tableNumber}
                                capacity={table.capacity}
                                date={filters.date}
                                slots={table.availableSlots?.map(slot => {
                                    const start = formatTime(slot.startOffset);
                                    const end = formatTime(slot.endOffset);
                                    return `${start} - ${end}`;
                                }) || []}
                            />
                        ))}
                    </div>
                </section>
            </section>
        </MainLayout>
    );
}