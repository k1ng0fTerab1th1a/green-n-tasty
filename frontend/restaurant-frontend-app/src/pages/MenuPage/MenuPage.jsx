import React, { useState, useEffect, useMemo } from "react";
import {
    MainLayout,
    Breadcrumbs,
    DishCard,
    Button,
    Dropdown,
    DishDetailsModal,
    Toast
} from "../../components/index.js";
import { getMenuDishes, getDishById, downloadMenuFile } from "../../services/dishes";
import styles from "./MenuPage.module.css";
import fallbackImage from "../../assets/images/main-hero.jpg";

const CATEGORIES = ["Appetizers", "Main Cources", "Desserts"];

export default function MenuPage() {
    const [toast, setToast] = useState({
        open: false,
        type: "success",
        title: "Success",
        message: ""
    });

    const showToast = (type, title, message) => {
        setToast({ open: true, type, title, message });
    };

    const hideToast = () => {
        setToast(prev => ({ ...prev, open: false }));
    };

    const [dishes, setDishes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [activeCategory, setActiveCategory] = useState("Main Cources");
    const [sortBy, setSortBy] = useState("popularity,desc");
    const [page, setPage] = useState(1);
    const [selectedDish, setSelectedDish] = useState(null);
    const [isDishModalOpen, setIsDishModalOpen] = useState(false);
    const [isDownloading, setIsDownloading] = useState(false);

    const itemsPerPage = 8;

    const sortOptions = [
        { label: "Popularity Descending", value: "popularity,desc" },
        { label: "Popularity Ascending", value: "popularity,asc" },
        { label: "Price: Low to High", value: "price,asc" },
        { label: "Price: High to Low", value: "price,desc" },
    ];

    useEffect(() => {
        async function fetchMenu() {
            try {
                setLoading(true);
                const result = await getMenuDishes(activeCategory, sortBy);

                if (result.isSuccess) {
                    const normalized = (result.data || []).map(dish => ({
                        id: dish.id,
                        name: dish.name,
                        price: dish.price,
                        weight: dish.weight,
                        imageSrc: dish.imageUrl || fallbackImage,
                        available: dish.state === "ON",
                        type: dish.dishType
                    }));
                    setDishes(normalized);
                }
            } catch (err) {
                console.error("Failed to load menu:", err);
            } finally {
                setLoading(false);
            }
        }
        fetchMenu();
    }, [activeCategory, sortBy]);

    const dishesToDisplay = useMemo(() => {
        const start = (page - 1) * itemsPerPage;
        return dishes.slice(start, start + itemsPerPage);
    }, [dishes, page]);

    useEffect(() => {
        setPage(1);
    }, [activeCategory, sortBy]);

    const handleDownloadMenu = async () => {
        try {
            setIsDownloading(true);
            await downloadMenuFile();
            showToast("success", "Success", "Menu download started!");
        } catch (err) {
            console.error("Download failed", err);
            showToast("error", "Error", "Could not load menu");
        } finally {
            setIsDownloading(false);
        }
    };

    const handleDishClick = async (id) => {
        try {
            const result = await getDishById(id);
            if (result.isSuccess) {
                setSelectedDish(result.data);
                setIsDishModalOpen(true);
            }
        } catch (error) {
            console.error("Failed to load dish details");
            showToast("error", "Error", "Could not load dish details");
        }
    };

    const breadcrumbItems = [
        { label: "Main page", to: "/main" },
        { label: "Menu" }
    ];

    return (
        <MainLayout>
            <div className={styles.container}>
                <Breadcrumbs items={breadcrumbItems}/>

                <section className={styles.top}>
                    <div className={styles.overlay}/>
                    <img className={styles.photo} src={fallbackImage} alt="Menu Banner"/>
                    <div className={styles.heroContentWrapper}>
                        <div className={styles.info}>
                            <p className={`${styles.heroSubtitle} h2`}>Green & Tasty Restaurants</p>
                            <h1 className={`${styles.title} h1`}>Menu</h1>

                            <Button
                                variant="primary"
                                size="lg"
                                onClick={handleDownloadMenu}
                                disabled={isDownloading}
                                className={styles.downloadBtn}
                            >
                                {isDownloading ? "Downloading..." : "Download Menu (PDF)"}
                            </Button>
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

                    {loading ? (
                        <div className={styles.stateMessage}>Loading our delicious menu...</div>
                    ) : (
                        <div className={styles.gridDishes}>
                            {dishesToDisplay.map((dish) => (
                                <DishCard
                                    key={dish.id}
                                    {...dish}
                                    onClick={() => handleDishClick(dish.id)}
                                />
                            ))}
                        </div>
                    )}
                </section>
            </div>

            <Toast
                open={toast.open}
                type={toast.type}
                title={toast.title}
                message={toast.message}
                onClose={hideToast}
            />

            <DishDetailsModal
                isOpen={isDishModalOpen}
                onClose={() => setIsDishModalOpen(false)}
                dish={selectedDish}
            />
        </MainLayout>
    );
}