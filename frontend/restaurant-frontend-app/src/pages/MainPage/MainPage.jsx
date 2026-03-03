import { useMemo } from "react";
import { Header, DishCard, LocationCard, MainHero } from "../../components/index.js";

import styles from "./MainPage.module.css";

// demo assets (заміни на свої)
import heroImg from "../../assets/images/main-hero.jpg";
import dish1 from "../../assets/images/main-hero.jpg";
import dish2 from "../../assets/images/main-hero.jpg";
import dish3 from "../../assets/images/main-hero.jpg";
import dish4 from "../../assets/images/main-hero.jpg";

import loc1 from "../../assets/images/main-hero.jpg";
import loc2 from "../../assets/images/main-hero.jpg";
import loc3 from "../../assets/images/main-hero.jpg";

export default function MainPage() {
    const popularDishes = useMemo(
        () => [
            { id: 1, name: "Fresh Strawberry Mint Salad", price: 17, weight: 430, imageSrc: dish1 },
            { id: 2, name: "Avocado Pine Nut Bowl", price: 17, weight: 430, imageSrc: dish2 },
            { id: 3, name: "Roasted Sweet Potato & Lentil Salad", price: 17, weight: 430, imageSrc: dish3 },
            { id: 4, name: "Spring Salad", price: 17, weight: 430, imageSrc: dish4 },
        ],
        []
    );

    const locations = useMemo(
        () => [
            { id: 1, address: "48 Rustaveli Avenue", tables: 10, occupancy: 90, imageSrc: loc1 },
            { id: 2, address: "14 Baratashvili Street", tables: 16, occupancy: 78, imageSrc: loc2 },
            { id: 3, address: "9 Abashidze Street", tables: 20, occupancy: 99, imageSrc: loc3 },
        ],
        []
    );

    return (
        <>
            <Header isAuth={false} role={"customer"} />
            <MainHero
                imageSrc={heroImg}
                title="Green & Tasty"
                description={[
                    "A network of restaurants in Tbilisi, Georgia, offering fresh, locally sourced dishes with a focus on health and sustainability.",
                    "Our diverse menu includes vegetarian and vegan options, crafted to highlight the rich flavors of Georgian cuisine with a modern twist.",
                ]}
                onViewMenu={() => console.log("go to menu")}
            />
            <main className={styles.page}>
                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Most Popular Dishes</h2>

                    <div className={styles.gridDishes}>
                        {popularDishes.map((d) => (
                            <DishCard
                                key={d.id}
                                name={d.name}
                                price={d.price}
                                weight={d.weight}
                                imageSrc={d.imageSrc}
                                available
                                onPreOrder={() => console.log("preorder", d.id)}
                            />
                        ))}
                    </div>
                </section>

                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Locations</h2>

                    <div className={styles.gridLocations}>
                        {locations.map((l) => (
                            <LocationCard
                                key={l.id}
                                imageSrc={l.imageSrc}
                                address={l.address}
                                tables={l.tables}
                                occupancy={l.occupancy}
                                onClick={() => console.log("open location", l.id)}
                            />
                        ))}
                    </div>
                </section>
            </main>
        </>
    );
}