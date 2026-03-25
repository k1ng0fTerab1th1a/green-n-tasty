import { useMemo, useState, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import {
    MainLayout,
    TableCard
} from "../../components/index.js";
import SearchPanel from "./SearchPanel/SearchPanel";
import styles from "./SearchPage.module.css";
import { getAvailableTables } from "../../services/bookings";
import { getLocationsSelectOptions } from "../../services/locations";
import heroImage from "../../assets/images/main-hero.jpg";
import tableImage from "../../assets/images/tableImage.jpg";

export default function SearchPage() {
    const [searchParams] = useSearchParams();
    const urlLocationId = searchParams.get("locationId") || "";

    const [locations, setLocations] = useState([]);
    const [tables, setTables] = useState([]);
    const [loading, setLoading] = useState(false);
    const [pageError, setPageError] = useState("");

    const [filters, setFilters] = useState({
        locationId: urlLocationId,
        date: new Date().toISOString().split('T')[0],
        time: "",
        guests: 1,
    });

    useEffect(() => {
        let isMounted = true;
        async function fetchInitialData() {
            try {
                const locationsData = await getLocationsSelectOptions();
                if (!isMounted) return;
                setLocations(locationsData || []);

                fetchAvailableTables(filters);
            } catch (err) {
                console.error("Failed to fetch locations select options:", err);
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

    const formatTime = (isoString) => {
        if (!isoString) return "";
        const date = new Date(isoString);
        if (isNaN(date.getTime())) return "";

        return new Intl.DateTimeFormat(undefined, {
            hour: "2-digit",
            minute: "2-digit",
            hour12: false,
            timeZone: "Asia/Tbilisi"
        }).format(date);
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
                            selectedLocation={filters.locationId}
                            selectedDate={filters.date}
                            selectedTime={filters.time}
                            guests={filters.guests}
                            onLocationChange={(value) => setFilters(p => ({ ...p, locationId: value }))}
                            onDateChange={(value) => setFilters(p => ({ ...p, date: value }))}
                            onTimeChange={(value) => setFilters(p => ({ ...p, time: value }))}
                            onGuestsChange={(value) => setFilters(p => ({ ...p, guests: value }))}
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

                    {pageError ? (
                        <div className={styles.stateMessageError}>{pageError}</div>
                    ) : tables.length === 0 && !loading ? (
                        <div className={styles.stateMessage}>No tables found for the selected criteria.</div>
                    ) : (
                        <div className={styles.resultsGrid}>
                            {tables.map((table) => (
                                <TableCard
                                    key={`${table.locationId}-${table.tableNumber}`}
                                    id={table.id}
                                    locationId={table.locationId}
                                    image={table.locationImage || tableImage}
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
                    )}
                </section>
            </section>
        </MainLayout>
    );
}