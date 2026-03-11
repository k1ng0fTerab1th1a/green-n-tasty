import { useState, useMemo } from "react";
import { Button, Dropdown, Modal } from "../index.js";
import styles from "./ReservationForm.module.css";
import { createReservation } from "../../services/reservations";

import usersIcon from "../../assets/icons/user.svg";
import clockIcon from "../../assets/icons/clock_bl.svg";

export default function ReservationForm({
                                            isOpen,
                                            onClose,
                                            onSuccess,
                                            tableInfo,
                                            selectedSlot,
                                        }) {
    const maxCapacity = tableInfo?.capacity || 1;
    const [guests, setGuests] = useState(maxCapacity);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState("");

    const slotStart = selectedSlot?.split(" - ")[0] || "";
    const slotEnd = selectedSlot?.split(" - ")[1] || "";

    const [timeFrom, setTimeFrom] = useState(slotStart);
    const [timeTo, setTimeTo] = useState(slotEnd);

    const generateTimeSteps = (startStr, endStr) => {
        if (!startStr || !endStr) return [];
        const steps = [];

        const parseTimeToDate = (timeStr) => {
            const [time, modifier] = timeStr.split(" ");
            let [hours, minutes] = time.split(":");
            if (hours === "12") hours = "00";
            if (modifier === "pm") hours = parseInt(hours, 10) + 12;
            const d = new Date();
            d.setHours(parseInt(hours, 10), parseInt(minutes, 10), 0, 0);
            return d;
        };

        let current = parseTimeToDate(startStr);
        const end = parseTimeToDate(endStr);

        while (current <= end) {
            const label = current.toLocaleTimeString("en-US", {
                hour: "numeric",
                minute: "2-digit",
                hour12: true,
            }).toLowerCase();
            steps.push({ value: label, label: label });
            current.setMinutes(current.getMinutes() + 15);
        }
        return steps;
    };

    const timeOptions = useMemo(() => generateTimeSteps(slotStart, slotEnd), [slotStart, slotEnd]);

    const timeToOptions = useMemo(() => {
        const startIndex = timeOptions.findIndex(opt => opt.value === timeFrom);
        return timeOptions.slice(startIndex + 1);
    }, [timeOptions, timeFrom]);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        setError("");

        try {
            if (!tableInfo?.locationId) {
                setError("Error: Location ID is missing.");
                setIsSubmitting(false);
                return;
            }

            const reservationData = {
                locationId: tableInfo.locationId,
                tableNumber: tableInfo.tableNumber,
                date: tableInfo.date,
                timeFrom: timeFrom,
                timeTo: timeTo,
                guestsCount: guests
            };

            await createReservation(reservationData);
            onSuccess(reservationData);
        } catch (err) {
            if (err.response && err.response.data) {
                const serverData = err.response.data;

                if (serverData.errors) {
                    const messages = Object.values(serverData.errors).flat();
                    setError(messages.join(" "));
                }
                else if (serverData.title || serverData.message) {
                    setError(serverData.title || serverData.message);
                }
                else {
                    setError("Server error occurred. Please try again.");
                }
            } else {
                setError("Network error. Please check your connection.");
            }
        } finally {
            setIsSubmitting(false);
        }
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
                    <p className={styles.sectionHint}>Table seating capacity: {maxCapacity} people</p>

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
                                    disabled={isSubmitting}
                                >
                                    −
                                </button>
                                <span className={styles.counterValue}>{guests}</span>
                                <button
                                    type="button"
                                    className={styles.counterBtn}
                                    onClick={() => setGuests(Math.min(maxCapacity, guests + 1))}
                                    disabled={isSubmitting}
                                >
                                    +
                                </button>
                            </div>
                        </div>
                    </div>
                </section>

                <section className={styles.section}>
                    <h3 className={styles.sectionTitle}>Time</h3>
                    <div className={styles.timeRow}>
                        <Dropdown
                            label="From"
                            value={timeFrom}
                            options={timeOptions}
                            onChange={setTimeFrom}
                            disabled={isSubmitting}
                            leftIcon={<img src={clockIcon} alt="" />}
                        />
                        <Dropdown
                            label="To"
                            value={timeTo}
                            options={timeToOptions}
                            onChange={setTimeTo}
                            disabled={isSubmitting}
                            leftIcon={<img src={clockIcon} alt="" />}
                        />
                    </div>
                </section>

                {error && <p className={styles.errorMessage}>{error}</p>}

                <Button
                    variant="primary"
                    fullWidth
                    className={styles.submitBtn}
                    onClick={handleSubmit}
                    disabled={isSubmitting}
                >
                    {isSubmitting ? "Creating..." : "Make a Reservation"}
                </Button>
            </div>
        </Modal>
    );
}