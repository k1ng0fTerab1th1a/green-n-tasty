import { useState } from "react";
import { useAuth } from "../../../auth/AuthContext.jsx";
import { AvailableSlotsModal, ReservationModal, ConfirmationModal, Toast } from "../../index.js";
import styles from "./TableCard.module.css";

import locationIcon from "../../../assets/icons/pin.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import plusIcon from "../../../assets/icons/plus.svg";

export default function TableCard({
                                      id,
                                      locationId,
                                      image,
                                      location,
                                      tableNumber,
                                      capacity,
                                      date,
                                      slots = [],
                                  }) {
    const [isSlotsModalOpen, setIsSlotsModalOpen] = useState(false);
    const [isReserveModalOpen, setIsReserveModalOpen] = useState(false);
    const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);

    const [selectedSlot, setSelectedSlot] = useState(null);
    const [finalReservationData, setFinalReservationData] = useState(null);

    const { auth } = useAuth();
    const [showAuthToast, setShowAuthToast] = useState(false);

    const handleSlotClick = (slot) => {
        if (!auth.isAuth) {
            setShowAuthToast(true);
            return;
        }
        setSelectedSlot(slot);
        setIsReserveModalOpen(true);
    };

    const handleReservationSuccess = (data) => {
        setFinalReservationData(data);
        setIsReserveModalOpen(false);
        setIsConfirmModalOpen(true);
    };

    return (
        <div className={styles.card}>
            <div className={styles.imageWrap}>
                <img src={image} alt={`Table ${tableNumber}`} />
            </div>

            <div className={styles.content}>
                <div className={styles.header}>
                    <div className={styles.location}>
                        <img src={locationIcon} alt="" className={styles.iconLoc} />
                        <span className={styles.locationText}>{location}</span>
                    </div>
                    <div className={styles.tableNumber}>Table {tableNumber}</div>
                </div>

                <div className={styles.capacity}>
                    Table seating capacity: {capacity} people
                </div>

                <div className={styles.slotsTitle}>
                    {slots.length} slots available for {date}:
                </div>

                <div className={styles.slotsGrid}>
                    {slots.slice(0, 5).map((slot, index) => (
                        <button
                            key={index}
                            className={styles.slotButton}
                            onClick={() => handleSlotClick(slot)}
                        >
                            <img src={clockIcon} alt="" className={styles.iconClock} />
                            {slot}
                        </button>
                    ))}

                    {slots.length > 5 && (
                        <button className={styles.showAll} onClick={() => setIsSlotsModalOpen(true)}>
                            <img src={plusIcon} alt="" className={styles.iconPlus} />
                            Show all
                        </button>
                    )}
                </div>
            </div>

            <Toast
                open={showAuthToast}
                type="error"
                title="Authorization required"
                message="Please sign in to book a table."
                onClose={() => setShowAuthToast(false)}
            />

            <AvailableSlotsModal
                isOpen={isSlotsModalOpen}
                onClose={() => setIsSlotsModalOpen(false)}
                slots={slots}
                onSlotSelect={handleSlotClick}
                tableInfo={{ location, tableNumber, date }}
            />

            <ReservationModal
                isOpen={isReserveModalOpen}
                onClose={() => setIsReserveModalOpen(false)}
                onSuccess={handleReservationSuccess}
                selectedSlot={selectedSlot}
                tableInfo={{
                    id,
                    locationId,
                    tableNumber,
                    location,
                    date,
                    capacity
                }}
            />

            <ConfirmationModal
                isOpen={isConfirmModalOpen}
                onClose={() => setIsConfirmModalOpen(false)}
                reservationData={{
                    restaurantName: "Green & Tasty",
                    guests: finalReservationData?.guestsCount || 0,
                    date: date,
                    timeFrom: finalReservationData?.timeFrom || "",
                    timeTo: finalReservationData?.timeTo || "",
                    tableNumber: tableNumber,
                    address: location
                }}
            />
        </div>
    );
}