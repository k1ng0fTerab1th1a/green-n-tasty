import { useState } from "react";
import { Modal, Tab, Star, Button } from "../index.js";
import styles from "./FeedbackModal.module.css";

export default function FeedbackModal({
                                          isOpen,
                                          onClose,
                                          onSubmit,
                                          reservationId,
                                          waiter,
                                          initialData
                                      }) {
    const [activeTab, setActiveTab] = useState("service");
    const [serviceRating, setServiceRating] = useState(initialData?.serviceRating || 4);
    const [culinaryRating, setCulinaryRating] = useState(initialData?.culinaryRating || 4);
    const [comment, setComment] = useState(initialData?.comment || "");

    const displayWaiter = waiter || {
        name: "Mario Jast",
        role: "Waiter",
        rating: 4.96,
        avatar: ""
    };

    const handleSubmit = () => {
        onSubmit?.({
            reservationId,
            serviceRating,
            culinaryRating,
            comment,
            submittedAt: new Date().toISOString()
        });
    };

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
                    <Tab active={activeTab === "culinary"} onClick={() => setActiveTab("culinary")}>
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
                        <div className={styles.culinarySection}>
                            <div className={styles.ratingRow}>
                                <div className={styles.stars}>
                                    {[1, 2, 3, 4, 5].map((val) => (
                                        <Star
                                            key={val}
                                            checked={val <= culinaryRating}
                                            onChange={() => setCulinaryRating(val)}
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
                            placeholder="Add your comments"
                            value={comment}
                            onChange={(e) => setComment(e.target.value)}
                        />
                    </div>
                </div>

                <Button variant="primary" fullWidth onClick={handleSubmit} className={styles.submitBtn}>
                    Submit Feedback
                </Button>
            </div>
        </Modal>
    );
}