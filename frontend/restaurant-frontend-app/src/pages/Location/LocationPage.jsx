import { useEffect, useMemo, useState } from "react";
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
    MainLayout,
} from "../../components/index.js";

import {
    getLocationById,
    getLocationFeedbacks,
    getLocationSpecialityDishes,
} from "../../services/locations";

import styles from "./LocationPage.module.css";
import pinIcon from "../../assets/icons/pin.svg";
import fallbackImage from "../../assets/images/main-hero.jpg";

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

function parseRating(value) {
    if (value == null) return 0;
    const parsed = Number(String(value).replace(/[^\d.,-]/g, "").replace(",", "."));
    return Number.isNaN(parsed) ? 0 : parsed;
}

function mapReviewDate(value) {
    if (!value) return "";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return String(value);
    return date.toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
    });
}

export default function LocationPage() {
    const navigate = useNavigate();
    const { locationId } = useParams();

    const [activeTab, setActiveTab] = useState("service");
    const [sortBy, setSortBy] = useState("top-rated");
    const [page, setPage] = useState(1);

    const [location, setLocation] = useState(null);
    const [dishes, setDishes] = useState([]);
    const [reviews, setReviews] = useState([]);

    const [loading, setLoading] = useState(true);
    const [pageError, setPageError] = useState("");

    const reviewsPerPage = 4;

    const sortOptions = useMemo(
        () => [
            { label: "Top rated first", value: "top-rated" },
            { label: "Low rated first", value: "low-rated" },
            { label: "Newest first", value: "newest" },
            { label: "Oldest first", value: "oldest" },
        ],
        []
    );

    const handleBookTableClick = () => {
        //navigate(`/search?locationId=${locationId}`);
    };

    useEffect(() => {
        setPage(1);
    }, [activeTab, sortBy]);

    useEffect(() => {
        let isMounted = true;
        async function loadPage() {
            if (!locationId) return;

            try {
                setLoading(true);
                setPageError("");

                const apiType = activeTab === "service" ? "waiter" : "kitchen";

                const [locationData, dishesData, feedbacksData] = await Promise.all([
                    getLocationById(locationId),
                    getLocationSpecialityDishes(locationId),
                    getLocationFeedbacks(locationId, apiType),
                ]);

                if (!isMounted) return;

                const normalizedLocation = locationData ? {
                    id: locationData.id,
                    name: locationData.name || "Green & Tasty",
                    address: locationData.address || "Unknown address",
                    rating: parseRating(locationData.rating),
                    imageSrc: locationData.imageUrl || fallbackImage,
                    description: typeof locationData.description === 'string'
                        ? [locationData.description]
                        : (Array.isArray(locationData.description) ? locationData.description : ["Welcome!"]),
                } : null;

                const normalizedDishes = Array.isArray(dishesData)
                    ? dishesData.map((dish, index) => ({
                        id: dish.id || `dish-${index}`,
                        name: dish.name || "Unnamed dish",
                        price: parsePrice(dish.price),
                        weight: parseWeight(dish.weight),
                        imageSrc: dish.previewImageUrl || dish.imageUrl || fallbackImage,
                        available: dish.state === "ON",
                    }))
                    : [];

                const normalizedReviews = Array.isArray(feedbacksData)
                    ? feedbacksData.map((item, index) => ({
                        id: item.id || `review-${index}`,
                        name: item.authorName || item.userName || "Guest",
                        date: mapReviewDate(item.date),
                        rating: parseRating(item.rate),
                        text: item.comment || "",
                        avatarSrc: item.avatarUrl || null,
                        category: item.type === "waiter" ? "service" : "cuisine",
                    }))
                    : [];

                setLocation(normalizedLocation);
                setDishes(normalizedDishes);
                setReviews(normalizedReviews);

                if (!normalizedLocation) setPageError("Location not found");
            } catch (err) {
                if (isMounted) setPageError("Failed to load location data.");
            } finally {
                if (isMounted) setLoading(false);
            }
        }
        loadPage();
        return () => { isMounted = false; };
    }, [locationId, activeTab]);

    const sortedReviews = useMemo(() => {
        const result = [...reviews];

        switch (sortBy) {
            case "top-rated":
                result.sort((a, b) => b.rating - a.rating);
                break;
            case "low-rated":
                result.sort((a, b) => a.rating - b.rating);
                break;
            case "newest":
                result.sort((a, b) => new Date(b.date) - new Date(a.date));
                break;
            case "oldest":
                result.sort((a, b) => new Date(a.date) - new Date(b.date));
                break;
            default:
                break;
        }
        return result;
    }, [reviews, sortBy]);

    const totalPages = Math.max(1, Math.ceil(sortedReviews.length / reviewsPerPage));
    const paginatedReviews = useMemo(() => {
        const start = (page - 1) * reviewsPerPage;
        return sortedReviews.slice(start, start + reviewsPerPage);
    }, [sortedReviews, page]);

    if (loading) return <MainLayout><div className={styles.stateMessage}>Loading location...</div></MainLayout>;
    if (pageError || !location) return <MainLayout><div className={styles.stateMessageError}>{pageError || "Location not found"}</div></MainLayout>;

    return (
        <MainLayout>
            <div className={styles.container}>
                <div className={styles.breadcrumbs}>
                    <NavigationLink to="/main" className={styles.crumb}>Main page</NavigationLink>
                    <span className={styles.sep}>›</span>
                    <span className={styles.crumbActive}>{location.name}</span>
                </div>

                <section className={styles.top}>
                    <div className={styles.info}>
                        <h1 className={`${styles.title} h1`}>{location.name}</h1>
                        <div className={styles.meta}>
                            <div className={styles.addressRow}>
                                <img src={pinIcon} alt="" className={styles.pinIcon} />
                                <span className="body-bold">{location.address}</span>
                            </div>
                            <div className={styles.rating}>
                                <span className="body-bold">{location.rating}</span>
                                <Star value={1} size={16} />
                            </div>
                        </div>
                        <div className={`${styles.desc} body`}>
                            {location.description.map((text, index) => (
                                <p key={index}>{text}</p>
                            ))}
                        </div>
                        <Button
                            variant="primary"
                            onClick={handleBookTableClick}
                            className={styles.cta}
                        >
                            Book a table
                        </Button>
                    </div>
                    <div className={styles.photoWrap}>
                        <img className={styles.photo} src={location.imageSrc} alt="" />
                    </div>
                </section>

                <section className={styles.section}>
                    <h2 className="h2">Specialty Dishes</h2>
                    {dishes.length === 0 ? (
                        <div className={styles.stateMessage}>No specialty dishes yet.</div>
                    ) : (
                        <div className={styles.gridDishes}>
                            {dishes.map((dish) => (
                                <DishCard key={dish.id} {...dish} />
                            ))}
                        </div>
                    )}
                </section>

                <section className={styles.section}>
                    <div className={styles.reviewsHeader}>
                        <h2 className="h2">Customer Reviews</h2>
                        <div className={styles.sortRow}>
                            <span className="caption">Sort by:</span>
                            <Dropdown value={sortBy} options={sortOptions} onChange={setSortBy} />
                        </div>
                    </div>
                    <div className={styles.tabs}>
                        <Tab active={activeTab === "service"} onClick={() => setActiveTab("service")}>Service</Tab>
                        <Tab active={activeTab === "cuisine"} onClick={() => setActiveTab("cuisine")}>Cuisine experience</Tab>
                    </div>
                    {paginatedReviews.length === 0 ? (
                        <div className={styles.stateMessage}>No reviews yet.</div>
                    ) : (
                        <>
                            <div className={styles.gridReviews}>
                                {paginatedReviews.map((review) => (
                                    <ReviewCard key={review.id} {...review} />
                                ))}
                            </div>
                            <div className={styles.pagination}>
                                <Pagination
                                    page={page}
                                    totalPages={totalPages}
                                    onChange={setPage}
                                />
                            </div>
                        </>
                    )}
                </section>
            </div>
        </MainLayout>
    );
}