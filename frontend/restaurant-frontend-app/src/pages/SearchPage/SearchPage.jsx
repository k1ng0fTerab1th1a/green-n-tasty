import { useMemo, useState } from "react";
import {
    MainLayout,
    TableCard,
    SearchPanel,
} from "../../components/index.js";
import styles from "./SearchPage.module.css";

import heroImage from "../../assets/images/main-hero.jpg";

const mockLocations = [
    { id: "loc-1", address: "48 Rustaveli Avenue" },
    { id: "loc-2", address: "14 Baratashvili Street" },
    { id: "loc-3", address: "9 Abashidze Street" },
];

const mockTables = [
    {
        id: 1,
        location: "48 Rustaveli Avenue",
        tableNumber: 1,
        capacity: 4,
        image: heroImage,
        date: "Oct 14, 2024",
        slots: [
            "10:30 a.m. - 12:00 p.m",
            "12:15 p.m. - 1:45 p.m",
            "2:00 p.m. - 3:30 p.m",
            "3:45 p.m. - 5:15 p.m",
            "5:30 p.m. - 7:00 p.m",
        ],
    },
    {
        id: 2,
        location: "48 Rustaveli Avenue",
        tableNumber: 2,
        capacity: 4,
        image: heroImage,
        date: "Oct 14, 2024",
        slots: [
            "10:30 a.m. - 12:00 p.m",
            "12:15 p.m. - 1:45 p.m",
            "2:00 p.m. - 3:30 p.m",
            "3:45 p.m. - 5:15 p.m",
            "5:30 p.m. - 7:00 p.m",
        ],
    },
    {
        id: 3,
        location: "48 Rustaveli Avenue",
        tableNumber: 3,
        capacity: 4,
        image: heroImage,
        date: "Oct 14, 2024",
        slots: [
            "10:30 a.m. - 12:00 p.m",
            "12:15 p.m. - 1:45 p.m",
            "2:00 p.m. - 3:30 p.m",
            "3:45 p.m. - 5:15 p.m",
            "5:30 p.m. - 7:00 p.m",
        ],
    },
    {
        id: 4,
        location: "48 Rustaveli Avenue",
        tableNumber: 4,
        capacity: 4,
        image: heroImage,
        date: "Oct 14, 2024",
        slots: [
            "10:30 a.m. - 12:00 p.m",
            "12:15 p.m. - 1:45 p.m",
            "2:00 p.m. - 3:30 p.m",
            "3:45 p.m. - 5:15 p.m",
            "5:30 p.m. - 7:00 p.m",
        ],
    },
];

export default function SearchPage() {
    const [filters, setFilters] = useState({
        locationId: "loc-1",
        date: "",
        time: "",
        guests: 1,
    });

    const filteredTables = useMemo(() => {
        return mockTables.filter((table) => {
            const selectedLocationAddress = mockLocations.find(
                (location) => location.id === filters.locationId
            )?.address;

            const matchesLocation =
                !filters.locationId || table.location === selectedLocationAddress;

            const matchesGuests =
                !filters.guests || table.capacity >= filters.guests;

            return matchesLocation && matchesGuests;
        });
    }, [filters]);

    const handleSearch = (data) => {
        setFilters(data);
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
                            locations={mockLocations}
                            dates={[
                                { value: "2026-03-10", label: "Oct 14, 2024" },
                                { value: "2026-03-11", label: "Oct 15, 2024" },
                                { value: "2026-03-12", label: "Oct 16, 2024" },
                            ]}
                            times={[
                                { value: "10:30", label: "10:30 a.m." },
                                { value: "12:15", label: "12:15 p.m." },
                                { value: "14:00", label: "2:00 p.m." },
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
                            {filteredTables.length} tables available
                        </h2>
                    </div>

                    <div className={styles.resultsGrid}>
                        {filteredTables.map((table) => (
                            <TableCard
                                key={table.id}
                                image={table.image}
                                location={table.location}
                                tableNumber={table.tableNumber}
                                capacity={table.capacity}
                                date={table.date}
                                slots={table.slots}
                            />
                        ))}
                    </div>
                </section>
            </section>
        </MainLayout>
    );
}