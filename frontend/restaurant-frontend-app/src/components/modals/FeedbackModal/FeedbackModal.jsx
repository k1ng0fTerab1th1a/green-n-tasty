import { useState, useEffect } from "react";
import { Modal, Tab, Star, Button } from "../../index.js";
import styles from "./FeedbackModal.module.css";

export default function FeedbackModal({
                                          isOpen,
                                          onClose,
                                          onSubmit,
                                          reservationId,
                                          waiter,
                                          bookingStatus,
                                          initialData,
                                          isMealServed = false,   // ⬅ ДОДАТИ
                                      }) {
    const [activeTab, setActiveTab] = useState("service");
    const [serviceRating, setServiceRating] = useState(initialData?.serviceRating || 4);
    const [culinaryRating, setCulinaryRating] = useState(initialData?.culinaryRating || 4);

    const [serviceComment, setServiceComment] = useState(initialData?.serviceComment || "");
    const [cuisineComment, setCuisineComment] = useState(initialData?.cuisineComment || "");

    const isCulinaryDisabled =
        bookingStatus?.toLowerCase().replace(/\s+/g, "") === "inprogress" &&
        !isMealServed;

    const displayWaiter = waiter || {
        name: "Mario Jast",
        role: "Waiter",
        rating: 4.96,
        avatar: ""
    };

    const handleSubmit = () => {
        const isService = activeTab === "service";
        if (!isService && isCulinaryDisabled) return;

        onSubmit?.({
            reservationId,
            serviceRating: isService ? serviceRating : 0,
            serviceComment: isService ? serviceComment : "",
            culinaryRating: !isService ? culinaryRating : 0,
            cuisineComment: !isService ? cuisineComment : ""
        });
    };

    const isCurrentTabDisabled = activeTab === "culinary" && isCulinaryDisabled;
    const currentComment = activeTab === "service" ? serviceComment : cuisineComment;

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title="Give Feedback"
            subtitle="Please rate your experience below"
        >
            <div className={styles.container}>
                <div className={styles.tabsRow}>
                    <Tab active={activeTab === "service"} onClick={() => setActiveTab("service")}>
                        Service
                    </Tab>
                    <Tab
                        active={activeTab === "culinary"}
                        onClick={() => setActiveTab("culinary")}
                        className={isCulinaryDisabled ? styles.grayTab : ""}
                    >
                        Culinary Experience
                    </Tab>
                </div>

                <div className={styles.content}>
                    {activeTab === "service" ? (
                        <div className={styles.serviceSection}>
                            <div className={styles.waiterCard}>
                                <div className={styles.avatarWrap}>
                                    <div className={styles.placeholderAvatar}>
                                        {displayWaiter.name.charAt(0)}
                                    </div>
                                </div>
                                <div className={styles.waiterMeta}>
                                    <div className={styles.waiterName}>{displayWaiter.name}</div>
                                    <div className={styles.waiterRole}>{displayWaiter.role}</div>
                                    <div className={styles.waiterRating}>
                                        {displayWaiter.rating} <span className={styles.miniStar}>★</span>
                                    </div>
                                </div>
                            </div>

                            <div className={styles.ratingRow}>
                                <div className={styles.stars}>
                                    {[1, 2, 3, 4, 5].map((val) => (
                                        <Star
                                            key={val}
                                            checked={val <= serviceRating}
                                            onChange={() => setServiceRating(val)}
                                            size={32}
                                        />
                                    ))}
                                </div>
                                <span className={styles.ratingText}>{serviceRating}/5 stars</span>
                            </div>
                        </div>
                    ) : (
                        <div className={`${styles.culinarySection} ${isCulinaryDisabled ? styles.disabledContent : ""}`}>
                            {isCulinaryDisabled && (
                                <div className={styles.infoBox}>
                                    <p className={styles.infoText}>
                                        <b>Kitchen feedback is currently unavailable.</b><br/>
                                        You can rate the culinary experience once your meal has been served.
                                    </p>
                                </div>
                            )}
                            <div className={styles.ratingRow}>
                                <div className={styles.stars}>
                                    {[1, 2, 3, 4, 5].map((val) => (
                                        <Star
                                            key={val}
                                            checked={val <= culinaryRating}
                                            onChange={() => !isCulinaryDisabled && setCulinaryRating(val)}
                                            size={32}
                                        />
                                    ))}
                                </div>
                                <span className={styles.ratingText}>{culinaryRating}/5 stars</span>
                            </div>
                        </div>
                    )}

                    <div className={styles.commentField}>
                        <textarea
                            className={styles.textarea}
                            placeholder={isCurrentTabDisabled ? "Feedback is locked for this section" : "Add your comments"}
                            value={currentComment}
                            onChange={(e) => {
                                if (activeTab === "service") setServiceComment(e.target.value);
                                else setCuisineComment(e.target.value);
                            }}
                            disabled={isCurrentTabDisabled}
                        />
                    </div>
                </div>

                <Button
                    variant={isCurrentTabDisabled ? "secondary" : "primary"}
                    fullWidth
                    onClick={handleSubmit}
                    className={styles.submitBtn}
                    disabled={isCurrentTabDisabled}
                >
                    Submit Feedback
                </Button>
            </div>
        </Modal>
    );
}