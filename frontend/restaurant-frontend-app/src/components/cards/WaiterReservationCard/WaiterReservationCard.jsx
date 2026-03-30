import { Button, TableSelector } from "../../index.js";
import styles from "./WaiterReservationCard.module.css";

import pinIcon from "../../../assets/icons/pin.svg";
import calendarIcon from "../../../assets/icons/calendar.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import dishIcon from "../../../assets/icons/dish.svg";
import userIcon from "../../../assets/icons/person.svg";
import guestsIcon from "../../../assets/icons/people.svg";

export default function WaiterReservationCard({ booking, onCancel, onEdit }) {
    const { address, date, time, guests, status, table = "Table 1", customerName, dishCount } = booking;

    const tableOptions = [
        { value: "Table 1", label: "Table 1" },
        { value: "Table 2", label: "Table 2" },
        { value: "Table 3", label: "Table 3" }
    ];

    return (
        <div className={styles.card}>
            <div className={styles.mainContent}>
                <div className={styles.infoSide}>
                    <div className={styles.infoRow}>
                        <img src={pinIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{address}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={calendarIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{date}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={clockIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{time}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={dishIcon} alt="" className={styles.icon} />
                        <span className="body-bold">Pre-order: {dishCount} dishes</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={userIcon} alt="" className={styles.icon} />
                        <span className="body-bold">Waiter {customerName}</span>
                    </div>
                    <div className={styles.infoRow}>
                        <img src={guestsIcon} alt="" className={styles.icon} />
                        <span className="body-bold">{guests} Guests</span>
                    </div>
                </div>

                <div className={styles.statusSide}>
                    <TableSelector
                        value={table}
                        options={tableOptions}
                        onChange={(val) => console.log(val)}
                    />
                    {status?.toLowerCase() === "pre-order" && (
                        <div className={styles.badge}>
                            <span className="caption">Pre-order</span>
                        </div>
                    )}
                </div>
            </div>

            <div className={styles.actions}>
                <Button
                    variant="tertiary"
                    onClick={onCancel}
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
            </div>
        </div>
    );
}