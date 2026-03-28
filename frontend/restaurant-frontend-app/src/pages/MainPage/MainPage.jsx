import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { DishCard, LocationCard, MainHero, MainLayout } from "../../components/index.js";

import { getPopularDishes } from "../../services/dishes";
import { getLocations } from "../../services/locations";

import styles from "./MainPage.module.css";

import heroImg from "../../assets/images/main-hero.jpg";
import fallbackDishImage from "../../assets/images/main-hero.jpg";
import fallbackLocationImage from "../../assets/images/main-hero.jpg";

function parsePrice(value) {
    if (value == null) return 0;

    const num = String(value).replace(/[^\d.,-]/g, "").replace(",", ".");
    const parsed = Number(num);

    return Number.isNaN(parsed) ? 0 : parsed;
}

function parseWeight(value) {
    if (value == null) return "";

    const num = String(value).replace(/[^\d.,-]/g, "").replace(",", ".");
    const parsed = Number(num);

    return Number.isNaN(parsed) ? String(value) : parsed;
}

function parseNumber(value) {
    if (value == null) return 0;

    const parsed = Number(String(value).replace(/[^\d.-]/g, ""));
    return Number.isNaN(parsed) ? 0 : parsed;
}

export default function MainPage() {
    const navigate = useNavigate();

    const [popularDishes, setPopularDishes] = useState([]);
    const [locations, setLocations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [pageError, setPageError] = useState("");

    useEffect(() => {
        let isMounted = true;

        async function loadMainPage() {
            try {
                setLoading(true);
                setPageError("");

                const [dishesData, locationsData] = await Promise.all([
                    getPopularDishes(),
                    getLocations(),
                ]);

                if (!isMounted) return;

                const normalizedDishes = Array.isArray(dishesData)
                    ? dishesData.map((dish, index) => ({
                        id: dish.id || `${dish.name}-${index}`,
                        name: dish.name || "Unnamed dish",
                        price: parsePrice(dish.price),
                        weight: parseWeight(dish.weight),
                        imageSrc: dish.previewImageUrl || fallbackDishImage,
                        available: true,
                    }))
                    : [];

                const normalizedLocations = Array.isArray(locationsData)
                    ? locationsData.map((location, index) => ({
                        id: location.id || String(index),
                        address: location.address || "Unknown address",
                        tables: parseNumber(location.totalCapacity),
                        occupancy: parseNumber(location.averageOccupancy),
                        imageSrc: location.imageUrl || fallbackLocationImage,
                    }))
                    : [];

                setPopularDishes(normalizedDishes);
                setLocations(normalizedLocations);
            } catch (err) {
                console.error("Failed to load main page data:", err);

                if (!isMounted) return;
                setPageError("Failed to load data. Please try again later.");
            } finally {
                if (isMounted) {
                    setLoading(false);
                }
            }
        }

        loadMainPage();

        return () => {
            isMounted = false;
        };
    }, []);

    const defaultLocationId = useMemo(() => {
        return locations[0]?.id || "1";
    }, [locations]);

    const hero = (
        <MainHero
            imageSrc={heroImg}
            title="Green & Tasty"
            description={[
                "A network of restaurants in Tbilisi, Georgia, offering fresh, locally sourced dishes with a focus on health and sustainability.",
                "Our diverse menu includes vegetarian and vegan options, crafted to highlight the rich flavors of Georgian cuisine with a modern twist.",
            ]}
            onViewMenu={() => navigate(`/menu`)}
        />
    );

    return (
        <MainLayout hero={hero}>
            <div className={styles.container}>
                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Most Popular Dishes</h2>

                    {loading ? (
                        <div className={styles.stateMessage}>Loading dishes...</div>
                    ) : pageError ? (
                        <div className={styles.stateMessageError}>{pageError}</div>
                    ) : popularDishes.length === 0 ? (
                        <div className={styles.stateMessage}>No popular dishes found.</div>
                    ) : (
                        <div className={styles.gridDishes}>
                            {popularDishes.map((d) => (
                                <DishCard
                                    key={d.id}
                                    name={d.name}
                                    price={d.price}
                                    weight={d.weight}
                                    imageSrc={d.imageSrc}
                                    available={d.available}
                                    //onPreOrder={() => navigate(`/locations/${defaultLocationId}`)}
                                />
                            ))}
                        </div>
                    )}
                </section>

                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Locations</h2>

                    {loading ? (
                        <div className={styles.stateMessage}>Loading locations...</div>
                    ) : pageError ? (
                        <div className={styles.stateMessageError}>{pageError}</div>
                    ) : locations.length === 0 ? (
                        <div className={styles.stateMessage}>No locations available.</div>
                    ) : (
                        <div className={styles.gridLocations}>
                            {locations.map((l) => (
                                <LocationCard
                                    key={l.id}
                                    imageSrc={l.imageSrc}
                                    address={l.address}
                                    tables={l.tables}
                                    occupancy={l.occupancy}
                                    onClick={() => navigate(`/locations/${l.id}`)}
                                />
                            ))}
                        </div>
                    )}
                </section>
            </div>
        </MainLayout>
    );
}