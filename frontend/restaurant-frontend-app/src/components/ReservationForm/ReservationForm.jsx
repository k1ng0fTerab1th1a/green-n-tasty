import { useState } from "react";
import { Button, Dropdown, Modal} from "../index.js";
import styles from "./ReservationForm.module.css";

import usersIcon from "../../assets/icons/user.svg";
import clockIcon from "../../assets/icons/clock_bl.svg";

export default function ReservationForm({
                                            isOpen,
                                            onClose,
                                            onSuccess,
                                            tableInfo,
                                            selectedSlot,
                                            allSlots = []
                                        }) {
    const [guests, setGuests] = useState(10);
    const [timeFrom, setTimeFrom] = useState(selectedSlot?.split(" - ")[0] || "");
    const [timeTo, setTimeTo] = useState(selectedSlot?.split(" - ")[1] || "");

    const timeOptions = allSlots.map(slot => ({
        value: slot.split(" - ")[0],
        label: slot.split(" - ")[0]
    }));

    const timeToOptions = allSlots.map(slot => ({
        value: slot.split(" - ")[1],
        label: slot.split(" - ")[1]
    }));

    const handleSubmit = () => {
        onSuccess();
    };

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title="Make a Reservation"
            subtitle={(
                <>
                    You are making a reservation at <strong>{tableInfo?.location || 'Green & Tasty'}</strong>, <strong>Table {tableInfo?.tableNumber}</strong>, for <strong>{tableInfo?.date}</strong>
                </>
            )}
        >
            <div className={styles.container}>
                <section className={styles.section}>
                    <h3 className={styles.sectionTitle}>Guests</h3>
                    <p className={styles.sectionHint}>Please specify the number of guests.</p>
                    <p className={styles.sectionHint}>Table seating capacity: {tableInfo?.capacity} people</p>

                    <div className={styles.guestsControl}>
                        <div className={styles.guestsInputWrapper}>
                            <div className={styles.guestsLabelGroup}>
                                <img src={usersIcon} alt="" className={styles.icon} />
                                <span className={styles.guestsLabel}>Guests</span>
                            </div>
                            <div className={styles.counter}>
                                <button
                                    type="button"
                                    className={styles.counterBtn}
                                    onClick={() => setGuests(Math.max(1, guests - 1))}
                                >
                                    −
                                </button>
                                <span className={styles.counterValue}>{guests}</span>
                                <button
                                    type="button"
                                    className={styles.counterBtn}
                                    onClick={() => setGuests(guests + 1)}
                                >
                                    +
                                </button>
                            </div>
                        </div>
                    </div>
                </section>

                <section className={styles.section}>
                    <h3 className={styles.sectionTitle}>Time</h3>
                    <p className={styles.sectionHint}>Please choose your preferred time from the dropdowns below</p>
                    <div className={styles.timeRow}>
                        <Dropdown
                            label="From"
                            value={timeFrom}
                            options={timeOptions}
                            onChange={setTimeFrom}
                            leftIcon={<img src={clockIcon} alt="" />}
                        />
                        <Dropdown
                            label="To"
                            value={timeTo}
                            options={timeToOptions}
                            onChange={setTimeTo}
                            leftIcon={<img src={clockIcon} alt="" />}
                        />
                    </div>
                </section>

                <Button
                    variant="primary"
                    fullWidth
                    className={styles.submitBtn}
                    onClick={handleSubmit}
                >
                    Make a Reservation
                </Button>
            </div>
        </Modal>
    );
}