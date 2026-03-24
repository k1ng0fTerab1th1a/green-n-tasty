import React, { useState, useEffect, useMemo } from "react";
import { MainLayout, Breadcrumbs, DishCard, Button, Dropdown } from "../../components/index.js";
import styles from "./MenuPage.module.css";
import fallbackImage from "../../assets/images/main-hero.jpg";
import mainHero from "../../assets/images/main-hero.jpg";

const CATEGORIES = ["Appetizers", "Main Cources", "Desserts"];
const MOCK_DISHES = [
    {
        id: 1,
        name: "Fresh Strawberry Mint Salad",
        price: 15,
        weight: 430,
        category: "Main Cources",
        imageSrc: mainHero,
        available: true
    },
    {
        id: 2,
        name: "Avocado Pine Nut Bowl",
        price: 17,
        weight: 430,
        category: "Main Cources",
        imageSrc: mainHero,
        available: false
    },
    {
        id: 3,
        name: "Roasted Sweet Potato & Lentil Salad",
        price: 10,
        weight: 430,
        category: "Main Cources",
        imageSrc: mainHero,
        available: true
    },
    {
        id: 4,
        name: "Spring Salad",
        price: 14,
        weight: 430,
        category: "Main Cources",
        imageSrc: mainHero,
        available: true
    },
]
export default function MenuPage() {
    const [activeCategory, setActiveCategory] = useState("Main Cources");
    const [sortBy, setSortBy] = useState("date,desc");
    const [page, setPage] = useState(1);

    const itemsPerPage = 8;

    const sortOptions = useMemo(() => [
        { label: "Newest first", value: "date,desc" },
        { label: "Price: Low to High", value: "price,asc" },
        { label: "Price: High to Low", value: "price,desc" },
    ], []);

    const filteredDishes = useMemo(() => {
        return MOCK_DISHES.filter(d => d.category === activeCategory);
    }, [activeCategory]);

    const totalPages = Math.ceil(filteredDishes.length / itemsPerPage) || 1;

    const dishesToDisplay = useMemo(() => {
        const start = (page - 1) * itemsPerPage;
        return filteredDishes.slice(start, start + itemsPerPage);
    }, [filteredDishes, page]);

    useEffect(() => {
        setPage(1);
    }, [activeCategory, sortBy]);

    const breadcrumbItems = [
        { label: "Main page", to: "/main" },
        { label: "Menu" }
    ];

    return (
        <MainLayout>
            <div className={styles.container}>
                <Breadcrumbs items={breadcrumbItems} />

                <section className={styles.top}>
                    <div className={styles.overlay} />
                    <img className={styles.photo} src={fallbackImage} alt="Menu Banner" />

                    <div className={styles.heroContentWrapper}>
                        <div className={styles.info}>
                            <p className={`${styles.heroSubtitle} h2`}>Green & Tasty Restaurants</p>
                            <h1 className={`${styles.title} h1`}>Menu</h1>
                        </div>
                    </div>
                </section>

                <section className={styles.section}>
                    <div className={styles.menuNav}>
                        <div className={styles.tabs}>
                            {CATEGORIES.map(cat => (
                                <Button
                                    key={cat}
                                    variant={activeCategory === cat ? "primary" : "secondary"}
                                    size="sm"
                                    onClick={() => setActiveCategory(cat)}
                                    className={styles.tabBtn}
                                >
                                    {cat}
                                </Button>
                            ))}
                        </div>

                        <div className={styles.sortRow}>
                            <span className="body-bold">Sort by:</span>
                            <Dropdown
                                value={sortBy}
                                options={sortOptions}
                                onChange={setSortBy}
                            />
                        </div>
                    </div>

                    {dishesToDisplay.length === 0 ? (
                        <div className={styles.stateMessage}>No dishes in this category yet.</div>
                    ) : (
                        <>
                            <div className={styles.gridDishes}>
                                {dishesToDisplay.map((dish) => (
                                    <DishCard
                                        key={dish.id}
                                        {...dish}
                                        onPreOrder={() => console.log("Pre-ordered:", dish.name)}
                                    />
                                ))}
                            </div>

                            {totalPages > 1 && (
                                <div className={styles.pagination}>
                                    <Pagination
                                        page={page}
                                        totalPages={totalPages}
                                        onChange={setPage}
                                    />
                                </div>
                            )}
                        </>
                    )}
                </section>
            </div>
        </MainLayout>
    );
}