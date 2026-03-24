import React, { useEffect, useMemo, useState, useRef } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
    Button,
    DishCard,
    ReviewCard,
    Dropdown,
    Pagination,
    Tab,
    NavigationLink,
    MainLayout, Breadcrumbs,
} from "../../components/index.js";

import {
    getLocationById,
    getLocationFeedbacks,
    getLocationSpecialityDishes,
} from "../../services/locations";

import styles from "./LocationPage.module.css";
import star from "../../assets/icons/star-filled.svg";
import pinIcon from "../../assets/icons/pin.svg";
import fallbackImage from "../../assets/images/main-hero.jpg";

const parsePrice = (v) => v == null ? 0 : Number(String(v).replace(/[^\d.,-]/g, "").replace(",", ".")) || 0;
const parseRating = (v) => v == null ? 0 : Number(String(v).replace(/[^\d.,-]/g, "").replace(",", ".")) || 0;
const mapReviewDate = (v) => v ? new Date(v).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" }) : "";

export default function LocationPage() {
    const navigate = useNavigate();
    const { locationId } = useParams();

    const [activeTab, setActiveTab] = useState("service");
    const [sortBy, setSortBy] = useState("date,desc");

    const [page, setPage] = useState(1);
    const [allLoadedReviews, setAllLoadedReviews] = useState([]);
    const [nextBatchToken, setNextBatchToken] = useState(null);
    const [hasMoreOnServer, setHasMoreOnServer] = useState(true);

    const [location, setLocation] = useState(null);
    const [dishes, setDishes] = useState([]);

    const [loading, setLoading] = useState(true);
    const [reviewsLoading, setReviewsLoading] = useState(false);
    const [pageError, setPageError] = useState("");

    const abortControllerRef = useRef(null);
    const isFetchingRef = useRef(false);

    const reviewsPerPage = 4;
    const batchSize = 12;

    const sortOptions = useMemo(() => [
        { label: "Newest first", value: "date,desc" },
        { label: "Oldest first", value: "date,asc" },
        { label: "Top rated first", value: "rate,desc" },
        { label: "Low rated first", value: "rate,asc" },
    ], []);

    const handleBookTableClick = () => navigate(`/search?locationId=${locationId}`);

    useEffect(() => {
        if (abortControllerRef.current) abortControllerRef.current.abort();

        isFetchingRef.current = false;
        setPage(1);
        setAllLoadedReviews([]);
        setNextBatchToken(null);
        setHasMoreOnServer(true);
    }, [activeTab, sortBy]);

    useEffect(() => {
        let isMounted = true;
        async function loadStaticData() {
            if (!locationId) return;
            try {
                setLoading(true);
                const [lData, dData] = await Promise.all([
                    getLocationById(locationId),
                    getLocationSpecialityDishes(locationId),
                ]);
                if (!isMounted) return;

                setLocation(lData ? {
                    ...lData,
                    rating: parseRating(lData.rating),
                    imageSrc: lData.imageUrl || fallbackImage,
                    description: Array.isArray(lData.description) ? lData.description : [lData.description || "Welcome!"],
                } : null);

                setDishes(Array.isArray(dData) ? dData.map(d => ({
                    ...d,
                    price: parsePrice(d.price),
                    imageSrc: d.previewImageUrl || d.imageUrl || fallbackImage,
                    available: d.state === "ON",
                })) : []);
            } catch (err) {
                if (isMounted) setPageError("Failed to load location data.");
            } finally {
                if (isMounted) setLoading(false);
            }
        }
        loadStaticData();
        return () => { isMounted = false; };
    }, [locationId]);

    useEffect(() => {
        let isMounted = true;

        async function loadReviews() {
            const pagesInMemory = Math.ceil(allLoadedReviews.length / reviewsPerPage);
            const needsMoreData = allLoadedReviews.length === 0 || page > pagesInMemory;

            if (!locationId || !hasMoreOnServer || !needsMoreData || isFetchingRef.current) return;

            try {
                isFetchingRef.current = true;
                setReviewsLoading(true);

                if (abortControllerRef.current) abortControllerRef.current.abort();
                abortControllerRef.current = new AbortController();

                const apiType = activeTab === "service" ? "waiter" : "kitchen";

                const result = await getLocationFeedbacks(
                    locationId,
                    apiType,
                    [sortBy],
                    batchSize,
                    nextBatchToken
                );

                if (!isMounted) return;

                const normalized = (result.items || []).map(item => ({
                    id: item.id,
                    name: item.userName || "Guest",
                    date: mapReviewDate(item.date),
                    rating: parseRating(item.rate),
                    text: item.comment || "",
                    avatarSrc: item.userAvatarUrl || null,
                }));

                setAllLoadedReviews(prev => {
                    const existingIds = new Set(prev.map(i => i.id));
                    const uniqueNew = normalized.filter(i => !existingIds.has(i.id));
                    return [...prev, ...uniqueNew];
                });

                setNextBatchToken(result.nextPageToken);
                setHasMoreOnServer(!!result.nextPageToken);
            } catch (err) {
                if (err.name !== 'CanceledError' && err.name !== 'AbortError') {
                    console.error("Batch load error:", err);
                }
            } finally {
                if (isMounted) {
                    setReviewsLoading(false);
                    isFetchingRef.current = false;
                }
            }
        }

        loadReviews();
        return () => { isMounted = false; };
    }, [locationId, activeTab, sortBy, page, allLoadedReviews.length]);

    const totalPagesForUI = useMemo(() => {
        const currentLoadedPages = Math.ceil(allLoadedReviews.length / reviewsPerPage);
        return hasMoreOnServer ? currentLoadedPages + 1 : currentLoadedPages;
    }, [allLoadedReviews.length, hasMoreOnServer]);

    const currentReviewsToDisplay = useMemo(() => {
        const startIndex = (page - 1) * reviewsPerPage;
        if (allLoadedReviews.length === 0) return [];
        return allLoadedReviews.slice(startIndex, startIndex + reviewsPerPage);
    }, [allLoadedReviews, page]);

    if (loading) return <MainLayout><div className={styles.stateMessage}>Loading...</div></MainLayout>;
    if (pageError || !location) return <MainLayout><div className={styles.stateMessageError}>{pageError}</div></MainLayout>;

    const breadcrumbItems = [
        { label: "Main page", to: "/main" },
        { label: location.address || "Location" }
    ];

    return (
        <MainLayout>
            <div className={styles.container}>
                <Breadcrumbs items={breadcrumbItems} />

                {/* Top Section */}
                <section className={styles.top}>
                    <div className={styles.info}>
                        <h1 className={`${styles.title} h1`}>Green & Tasty</h1>
                        <div className={styles.meta}>
                            <div className={styles.addressRow}>
                                <img src={pinIcon} alt="" className={styles.pinIcon} />
                                <span className="body-bold">{location.address}</span>
                            </div>
                            <div className={styles.rating}>
                                <span className="body-bold">{location.rating}</span>
                                <img src={star} alt="" className={styles.star} />
                            </div>
                        </div>
                        <div className={`${styles.desc} body`}>
                            {location.description.map((text, index) => <p key={index}>{text}</p>)}
                        </div>
                        <Button variant="primary" onClick={handleBookTableClick} className={styles.cta}>Book a table</Button>
                    </div>
                    <div className={styles.photoWrap}>
                        <img className={styles.photo} src={location.imageSrc} alt="" />
                    </div>
                </section>

                {/* Specialty Dishes */}
                <section className={styles.section}>
                    <h2 className="h2">Specialty Dishes</h2>
                    {dishes.length === 0 ? <div className={styles.stateMessage}>No dishes.</div> : (
                        <div className={styles.gridDishes}>
                            {dishes.map((dish) => <DishCard key={dish.id} {...dish} />)}
                        </div>
                    )}
                </section>

                {/* Reviews */}
                <section className={styles.section}>
                    <h2 className="h2">Customer Reviews</h2>
                    <div className={styles.reviewsNav}>
                        <div className={styles.tabs}>
                            <Tab
                                active={activeTab === "service"}
                                onClick={() => setActiveTab("service")}
                            >
                                Service
                            </Tab>
                            <Tab
                                active={activeTab === "cuisine"}
                                onClick={() => setActiveTab("cuisine")}
                            >
                                Cuisine experience
                            </Tab>
                        </div>

                        <div className={styles.sortRow}>
                            <span className="body-reg">Sort by:</span>
                            <Dropdown
                                value={sortBy}
                                options={sortOptions}
                                onChange={setSortBy}
                            />
                        </div>
                    </div>

                    {allLoadedReviews.length === 0 && reviewsLoading ? (
                        <div className={styles.stateMessage}>Loading reviews...</div>
                    ) : (
                        <>
                            {allLoadedReviews.length === 0 ? (
                                <div className={styles.stateMessage}>No reviews yet.</div>
                            ) : (
                                <>
                                    <div className={styles.gridReviews}>
                                        {currentReviewsToDisplay.map((r) => (
                                            <ReviewCard key={r.id} {...r} />
                                        ))}
                                    </div>
                                    <div className={styles.pagination}>
                                        <Pagination
                                            page={page}
                                            totalPages={totalPagesForUI}
                                            onChange={(p) => {
                                                setPage(p);
                                            }}
                                        />
                                    </div>
                                </>
                            )}
                        </>
                    )}
                </section>
            </div>
        </MainLayout>
    );
}