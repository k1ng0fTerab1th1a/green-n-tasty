import { Button } from "../index.js";
import styles from "./BookingCard.module.css";
import pinIcon from "../../assets/icons/pin.svg";
import calendarIcon from "../../assets/icons/calendar.svg";
import clockIcon from "../../assets/icons/clock.svg";
import userIcon from "../../assets/icons/people.svg";

export default function BookingCard({
                                        booking,
                                        onCancel,
                                        onEdit,
                                        onFeedback,
                                        hasFeedback
                                    }) {
    const { address, date, time, guests, status } = booking;
    const statusClass = styles[status.toLowerCase().replace(/\s+/g, "")] || "";

    return (
        <div className={`card ${styles.card}`}>
            <div className={styles.header}>
                <div className={styles.infoRow}>
                    <img src={pinIcon} alt="" className={styles.icon} />
                    <span className="body-bold">{address}</span>
                </div>
                <div className={`${styles.badge} ${statusClass}`}>
                    <span className="caption">{status}</span>
                </div>
            </div>

            <div className={styles.details}>
                <div className={styles.infoRow}>
                    <img src={calendarIcon} alt="" className={styles.icon} />
                    <span className="body">{date}</span>
                </div>
                <div className={styles.infoRow}>
                    <img src={clockIcon} alt="" className={styles.icon} />
                    <span className="body">{time}</span>
                </div>
                <div className={styles.infoRow}>
                    <img src={userIcon} alt="" className={styles.icon} />
                    <span className="body">{guests} Guests</span>
                </div>
            </div>

            <div className={styles.actions}>
                {status === "Reserved" && (
                    <>
                        <Button
                            variant="tertiary"
                            onClick={onCancel}
                            className={styles.cancelBtn}
                        >
                            Cancel
                        </Button>
                        <Button
                            variant="secondary"
                            size="lg"
                            onClick={onEdit}
                            className={styles.editBtn}
                        >
                            Edit
                        </Button>
                    </>
                )}

                {status === "In Progress" && (
                    <Button
                        variant="secondary"
                        size="lg"
                        fullWidth
                        onClick={onFeedback}
                    >
                        Leave Feedback
                    </Button>
                )}

                {status === "Finished" && (
                    <Button
                        variant="secondary"
                        size="lg"
                        fullWidth
                        onClick={onFeedback}
                    >
                        {hasFeedback ? "Update Feedback" : "Leave Feedback"}
                    </Button>
                )}
            </div>
        </div>
    );
}