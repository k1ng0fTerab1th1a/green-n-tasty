import { useState } from "react";
import { AvailableSlotsModal, ReservationForm, ConfirmationModal} from "../index.js";
import styles from "./TableCard.module.css";

import locationIcon from "../../assets/icons/pin.svg";
import clockIcon from "../../assets/icons/clock.svg";
import plusIcon from "../../assets/icons/plus.svg";

export default function TableCard({
                                      image,
                                      location,
                                      tableNumber,
                                      capacity,
                                      date,
                                      slots = [],
                                  }) {
    const [isSlotsModalOpen, setIsSlotsModalOpen] = useState(false);
    const [isReserveModalOpen, setIsReserveModalOpen] = useState(false);
    const [selectedSlot, setSelectedSlot] = useState(null);
    const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);

    const handleSlotClick = (slot) => {
        setSelectedSlot(slot);
        setIsSlotsModalOpen(false);
        setIsReserveModalOpen(true);
    };

    const handleFinalSubmit = () => {
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

                    <button className={styles.showAll} onClick={() => setIsSlotsModalOpen(true)}>
                        <img src={plusIcon} alt="" className={styles.iconPlus} />
                        Show all
                    </button>
                </div>
            </div>

            <AvailableSlotsModal
                isOpen={isSlotsModalOpen}
                onClose={() => setIsSlotsModalOpen(false)}
                slots={slots}
                onSlotSelect={handleSlotClick}
                tableInfo={{ location, tableNumber, date }}
            />

            <ReservationForm
                isOpen={isReserveModalOpen}
                onClose={() => setIsReserveModalOpen(false)}
                onSuccess={handleFinalSubmit}
                selectedSlot={selectedSlot}
                allSlots={slots}
                tableInfo={{ location, tableNumber, date, capacity, image }}
            />

            <ConfirmationModal
                isOpen={isConfirmModalOpen}
                onClose={() => setIsConfirmModalOpen(false)}
                reservationData={{
                    restaurantName: "Green & Tasty",
                    guests: 10,
                    date: date,
                    timeFrom: selectedSlot?.split(" - ")[0] || "",
                    timeTo: selectedSlot?.split(" - ")[1] || "",
                    tableNumber: tableNumber,
                    address: location
                }}
            />
        </div>
    );
}