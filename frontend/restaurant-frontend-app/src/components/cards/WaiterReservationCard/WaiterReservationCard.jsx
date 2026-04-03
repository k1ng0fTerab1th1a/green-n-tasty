import styles from "./WaiterReservationCard.module.css";

import pinIcon from "../../../assets/icons/pin.svg";
import calendarIcon from "../../../assets/icons/calendar.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import dishIcon from "../../../assets/icons/dish.svg";
import userIcon from "../../../assets/icons/person.svg";
import guestsIcon from "../../../assets/icons/people.svg";
import {Button} from "../../index.js";

export default function WaiterReservationCard({
                                                  booking,
                                                  onCancel,
                                                  onEdit,
                                                  onEditOrder,
                                                  onCreateOrder,
                                                  onFinish,
                                                  onStart,
                                                  onMealServed,
                                                  onReceipt
                                              }) {
    const {
        locationAddress,
        startDateTime,
        endDateTime,
        guestsCount,
        status,
        visitorName,
        customerName,
        dishCount,
        tableNumber,
        isMealServed,
        isCreatedByWaiter,
    } = booking;

    const displayName = customerName || visitorName || "Guest";
    const dateObj = startDateTime ? new Date(startDateTime) : null;
    const displayDate = dateObj ? dateObj.toLocaleDateString() : "No date";

    const formatTime = (isoString) => {
        if (!isoString) return "--:--";
        return new Date(isoString).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
            timeZone: "Asia/Tbilisi",
            hour12: false,
        });
    };

    const displayTimeRange = `${formatTime(startDateTime)} - ${formatTime(endDateTime)}`;

    const renderActions = () => {
        const currentStatus = status?.toLowerCase();

        switch (currentStatus) {
            case "reserved":
                return (
                    <>
                        <div className={styles.leftActions}>
                            <Button variant="tertiary" onClick={onStart}>Start</Button>
                        </div>
                        <div className={styles.rightActions}>
                            <Button variant="tertiary" onClick={onCancel}>Cancel</Button>
                            <Button variant="secondary" onClick={onEdit}>Edit</Button>
                        </div>
                    </>
                );
            case "inprogress":
                return (
                    <>
                        <div className={styles.leftActions}>
                            <Button variant="tertiary" onClick={onFinish}>Finish</Button>

                            {!isMealServed && dishCount > 0 && (
                                <Button variant="tertiary" onClick={onMealServed}>
                                    MealServed
                                </Button>
                            )}
                        </div>
                        {dishCount > 0 ? (
                            <Button variant="primary" onClick={() => onEditOrder(booking)}>Edit Order</Button>
                        ) : (
                            <Button variant="primary" onClick={() => onCreateOrder(booking)}>Create Order</Button>
                        )}
                    </>
                );
            case "finished":
                return (
                    <>
                        <div className={styles.leftActions}></div>
                        {dishCount > 0 && (
                            <Button variant="secondary" onClick={onReceipt}>RECEIPT</Button>
                        )}
                    </>
                );
            case "cancelled":
                return null;
            default:
                return (
                    <>
                        <div className={styles.leftActions}>
                            <Button variant="tertiary" onClick={onFinish}>Finish</Button>
                        </div>
                        <Button variant="primary" onClick={() => onEditOrder(booking)}>Edit order</Button>
                    </>
                );
        }
    };

    return (
        <div className={styles.card}>
            <div className={styles.mainContent}>
                <div className={styles.infoSide}>
                    <div className={styles.infoRow}>
                        <img src={pinIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{locationAddress || "No address"}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={calendarIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{displayDate}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={clockIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{displayTimeRange}</span>
                    </div>
                    {/*{dishCount > 0 && (*/}
                        <div className={styles.infoRow}>
                            <img src={dishIcon} alt="" className={styles.icon} />
                            <span className="body-bold">Order: {dishCount} dishes</span>
                        </div>
                    {/*)}*/}
                    <div className={styles.infoRow}>
                        <img src={userIcon} alt="" className={styles.icon} />
                        {customerName ? "Customer: " : "Visitor: "}
                        {displayName}
                    </div>

                    <div className={styles.infoRow}>
                        <img src={guestsIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{guestsCount} Guests</span>
                    </div>
                </div>

                <div className={styles.statusSide}>
                    <span className={styles.tableLabel}>
                        Table {tableNumber ?? "-"}
                    </span>
                    <div className={styles.badge}>
                        <span className="caption">{status || "New"}</span>
                    </div>
                    {isCreatedByWaiter && (
                        <div className={`${styles.badge} ${styles.waiterBadge}`}>
                            <span className="caption">Created by Waiter</span>
                        </div>
                    )}
                </div>
            </div>
            <div className={styles.actions}>{renderActions()}</div>
        </div>
    );
}