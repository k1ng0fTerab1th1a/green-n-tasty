import { useState } from "react";
import { Button, TableSelector } from "../../index.js";
import styles from "./WaiterReservationCard.module.css";

import pinIcon from "../../../assets/icons/pin.svg";
import calendarIcon from "../../../assets/icons/calendar.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import dishIcon from "../../../assets/icons/dish.svg";
import userIcon from "../../../assets/icons/person.svg";
import guestsIcon from "../../../assets/icons/people.svg";

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
        guestsCount,
        status,
        visitorName,
        waiterName,
        dishCount,
        tableNumber,
        isCreatedByWaiter
    } = booking;

    const [selectedTable, setSelectedTable] = useState(tableNumber?.toString() || "");

    const dateObj = startDateTime ? new Date(startDateTime) : null;
    const displayDate = dateObj ? dateObj.toLocaleDateString() : "No date";
    const displayTime = dateObj ? dateObj.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : "No time";

    const tableOptions = [
        { value: "1", label: "Table 1" },
        { value: "2", label: "Table 2" },
        { value: "3", label: "Table 3" }
    ];

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
                            <Button variant="tertiary" onClick={onMealServed}>MealServed</Button>
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
                        <Button variant="secondary" onClick={onReceipt}>RECEIPT</Button>
                    </>
                );

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
                        <span className="body-bold">{displayTime}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={dishIcon} alt="" className={styles.icon} />
                        <span className="body-bold">Pre-order: {dishCount} dishes</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={userIcon} alt="" className={styles.icon} />
                        <span className="body-bold">
                            {isCreatedByWaiter ? `Waiter: ${waiterName}` : `Guest: ${visitorName}`}
                        </span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={guestsIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{guestsCount} Guests</span>
                    </div>
                </div>

                <div className={styles.statusSide}>
                    <TableSelector
                        value={selectedTable}
                        options={tableOptions}
                        onChange={(val) => setSelectedTable(val)}
                    />
                    <div className={styles.badge}>
                        <span className="caption">{status || "New"}</span>
                    </div>
                </div>
            </div>

            <div className={styles.actions}>
                {renderActions()}
            </div>
        </div>
    );
}