import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
    Button,
    DishCard,
    ReviewCard,
    Dropdown,
    Pagination,
    Tab,
    Star,
    NavigationLink,
    MainLayout
} from "../../components/index.js";

import styles from "./LocationPage.module.css";

import heroImg from "../../assets/images/main-hero.jpg";
import dish1 from "../../assets/images/main-hero.jpg";
import dish2 from "../../assets/images/main-hero.jpg";
import dish3 from "../../assets/images/main-hero.jpg";
import dish4 from "../../assets/images/main-hero.jpg";

export default function LocationPage() {
    const navigate = useNavigate();
    const { locationId } = useParams();

    const [activeTab, setActiveTab] = useState("service");
    const [sortBy, setSortBy] = useState("Top rated first");
    const [page, setPage] = useState(1);

    const location = useMemo(
        () => ({
            id: Number(locationId) || 1,
            name: "Green & Tasty",
            address: "48 Rustaveli Avenue",
            rating: 4.73,
            imageSrc: heroImg,
            description: [
                "Located on bustling Rustaveli Avenue, this branch offers a modern, yet cozy atmosphere.",
                "Known for our fresh, locally sourced dishes, we focus on health and sustainability, blending Georgian cuisine with a modern twist.",
                "With extensive seasonal specials, it’s perfect for casual lunches and intimate dinners.",
            ],
        }),
        [locationId]
    );

    const dishes = useMemo(
        () => [
            { id: 1, name: "Fresh Strawberry Mint Salad", price: 17, weight: 430, imageSrc: dish1, available: true },
            { id: 2, name: "Avocado Pine Nut Bowl", price: 17, weight: 430, imageSrc: dish2, available: true },
            { id: 3, name: "Roasted Sweet Potato & Lentil Salad", price: 17, weight: 430, imageSrc: dish3, available: true },
            { id: 4, name: "Spring Salad", price: 17, weight: 430, imageSrc: dish4, available: true },
        ],
        []
    );

    const reviews = useMemo(
        () => [
            {
                id: 1,
                author: "David",
                date: "Aug 29, 2024",
                rating: 5,
                text:
                    "Absolutely loved this restaurant! The outdoor terrace was perfect for a relaxing evening, and the menu had so many fresh, healthy options. Definitely coming back soon!",
                avatarSrc: null,
                anonymous: false,
            },
            {
                id: 2,
                author: "User 1765",
                date: "Aug 29, 2024",
                rating: 5,
                text:
                    "The best dining experience I’ve had in Tbilisi. The vegan options were fantastic, and the service was very attentive.",
                avatarSrc: null,
                anonymous: true,
            },
            {
                id: 3,
                author: "Giorgi",
                date: "Aug 29, 2024",
                rating: 4,
                text:
                    "Great location and cozy atmosphere. The seasonal menu is excellent. Highly recommend the specials.",
                avatarSrc: null,
                anonymous: false,
            },
            {
                id: 4,
                author: "Anna",
                date: "Aug 29, 2024",
                rating: 5,
                text:
                    "Loved the attention to details. Fresh ingredients and modern Georgian flavors. Amazing!",
                avatarSrc: "https://i.pravatar.cc/80?img=47",
                anonymous: false,
            },
        ],
        []
    );

    const sortItems = useMemo(() => ["Top rated first", "Low rated first", "Newest first", "Oldest first"], []);
    const totalPages = 3;

    return (
        <MainLayout headerProps={{ isAuth: false, role: "customer" }}>
            <div className={styles.breadcrumbs}>
                <NavigationLink to="/" variant="link" className={styles.crumb}>
                    Main page
                </NavigationLink>
                <span className={styles.sep}>›</span>
                <span className={styles.crumbActive}>Location {location.address}</span>
            </div>

            {/* TOP BLOCK */}
            <section className={styles.top}>
                <div className={styles.info}>
                    <h1 className={styles.title}>{location.name}</h1>

                    <div className={styles.meta}>
                        <div className={styles.address}>{location.address}</div>

                        <div className={styles.rating}>
                            <Star value={location.rating} />
                            <span className={styles.ratingValue}>{location.rating}</span>
                        </div>
                    </div>

                    <div className={styles.desc}>
                        {location.description.map((t, i) => (
                            <p key={i}>{t}</p>
                        ))}
                    </div>

                    <Button
                        variant="primary"
                        size="lg"
                        className={styles.cta}
                        onClick={() => navigate(`/locations/${location.id}/book`)}
                    >
                        Book a Table
                    </Button>
                </div>

                <div className={styles.photoWrap}>
                    <img className={styles.photo} src={location.imageSrc} alt="" />
                </div>
            </section>

            {/* DISHES */}
            <section className={styles.section}>
                <h2 className={styles.sectionTitle}>Specialty Dishes</h2>

                <div className={styles.gridDishes}>
                    {dishes.map((d) => (
                        <DishCard
                            key={d.id}
                            name={d.name}
                            price={d.price}
                            weight={d.weight}
                            imageSrc={d.imageSrc}
                            available={d.available}
                            onPreOrder={() => console.log("preorder", d.id)}
                        />
                    ))}
                </div>
            </section>

            {/* REVIEWS */}
            <section className={styles.section}>
                <div className={styles.reviewsHeader}>
                    <h2 className={styles.sectionTitle}>Customer Reviews</h2>

                    <div className={styles.sortRow}>
                        <span className={styles.sortLabel}>Sort by</span>
                        <Dropdown value={sortBy} items={sortItems} onChange={setSortBy} size="sm" />
                    </div>
                </div>

                <div className={styles.tabs}>
                    <Tab active={activeTab === "service"} onClick={() => setActiveTab("service")}>
                        Service
                    </Tab>
                    <Tab active={activeTab === "cuisine"} onClick={() => setActiveTab("cuisine")}>
                        Cuisine experience
                    </Tab>
                </div>

                <div className={styles.gridReviews}>
                    {reviews.map((r) => (
                        <ReviewCard
                            key={r.id}
                            name={r.author}
                            date={r.date}
                            rating={r.rating}
                            text={r.text}
                            avatarSrc={r.avatarSrc}
                            anonymous={r.anonymous}
                        />
                    ))}
                </div>

                <div className={styles.pagination}>
                    <Pagination page={page} totalPages={totalPages} onChange={setPage} />
                </div>
            </section>
        </MainLayout>
    );
}