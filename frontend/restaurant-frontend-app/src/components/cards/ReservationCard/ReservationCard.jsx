import { Button } from "../../index.js";
import styles from "./ReservationCard.module.css";
import pinIcon from "../../../assets/icons/pin.svg";
import calendarIcon from "../../../assets/icons/calendar.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import userIcon from "../../../assets/icons/people.svg";

export default function ReservationCard({
                                            booking,
                                            onCancel,
                                            onEdit,
                                            onFeedback,
                                            hasFeedback
                                        }) {
    const { address, date, time, guests, status, isMealServed } = booking;

    const getStatusKey = (s) => {
        const normalized = s?.toLowerCase().replace(/\s+/g, "");
        if (normalized === "cancelled" || normalized === "canceled") return "canceled";
        return normalized || "";
    };

    const formatStatus = (s) => {
        if (!s) return "";
        const key = getStatusKey(s);
        if (key === "inprogress") return "In Progress";
        if (key === "mealsserved") return "Meal Served";
        return s.charAt(0).toUpperCase() + s.slice(1);
    };

    const statusKey = getStatusKey(status);
    const statusClass = styles[statusKey] || "";

    const canLeaveFeedback = ["inprogress", "mealsserved", "finished"].includes(statusKey);

    return (
        <div className={`card ${styles.card}`}>
            <div className={styles.header}>
                <div className={styles.infoRow}>
                    <img src={pinIcon} alt="" className={styles.icon} />
                    <span className="body-bold">{address}</span>
                </div>
                <div className={`${styles.badge} ${statusClass}`}>
                    <span className="caption">{formatStatus(status)}</span>
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
                {statusKey === "reserved" && (
                    <>
                        <Button variant="tertiary" onClick={onCancel} className={styles.cancelBtn}>Cancel</Button>
                        <Button variant="secondary" size="lg" onClick={onEdit} className={styles.editBtn}>Edit</Button>
                    </>
                )}

                {canLeaveFeedback && (
                    <Button
                        variant="secondary"
                        size="lg"
                        fullWidth
                        onClick={() => onFeedback(booking)}
                    >
                        {statusKey === "finished" && hasFeedback ? "Update Feedback" : "Leave Feedback"}
                    </Button>
                )}
            </div>
        </div>
    );
}