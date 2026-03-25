import { useState, useMemo, useEffect } from "react";
import { Button, Dropdown, Modal } from "../../index.js";
import styles from "./ReservationModal.module.css";
import { createReservation, updateReservation } from "../../../services/reservations.js";

import usersIcon from "../../../assets/icons/user.svg";
import clockIcon from "../../../assets/icons/clock_bl.svg";

export default function ReservationModal({
                                            isOpen,
                                            onClose,
                                            onSuccess,
                                            tableInfo,
                                            selectedSlot,
                                        }) {
    const isEditMode = !!tableInfo?.id;
    const maxCapacity = tableInfo?.capacity || 1;

    const [guests, setGuests] = useState(tableInfo?.guestsCount || 1);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState("");

    const cleanTimeStr = (str) => str ? str.trim() : "";

    const rangeStart = useMemo(() => {
        const start = isEditMode
            ? (tableInfo?.workingHoursStart || "10:00")
            : (selectedSlot?.split(" - ")[0] || tableInfo?.timeFrom || "");
        return cleanTimeStr(start);
    }, [isEditMode, tableInfo, selectedSlot]);

    const rangeEnd = useMemo(() => {
        const end = isEditMode
            ? (tableInfo?.workingHoursEnd || "22:00")
            : (selectedSlot?.split(" - ")[1] || tableInfo?.timeTo || "");
        return cleanTimeStr(end);
    }, [isEditMode, tableInfo, selectedSlot]);

    const [timeFrom, setTimeFrom] = useState("");
    const [timeTo, setTimeTo] = useState("");

    useEffect(() => {
        if (isOpen && tableInfo) {
            setGuests(tableInfo.guestsCount || 1);
            setTimeFrom(cleanTimeStr(tableInfo.timeFrom) || rangeStart);
            setTimeTo(cleanTimeStr(tableInfo.timeTo) || rangeEnd);
            setError("");
        }
    }, [isOpen, tableInfo, rangeStart, rangeEnd]);

    const generateTimeSteps = (startStr, endStr) => {
        if (!startStr || !endStr) return [];
        const steps = [];

        const parseTimeToDate = (timeStr) => {
            const [hours, minutes] = timeStr.split(":");
            const d = new Date();
            d.setHours(parseInt(hours, 10), parseInt(minutes, 10), 0, 0);
            return d;
        };

        try {
            let current = parseTimeToDate(startStr);
            let end = parseTimeToDate(endStr);

            if (end < current) {
                end.setDate(end.getDate() + 1);
            }

            while (current <= end) {
                const label = current.toLocaleTimeString(undefined, {
                    hour: "2-digit",
                    minute: "2-digit",
                    hour12: false,
                });

                steps.push({ value: label, label: label });
                current.setMinutes(current.getMinutes() + 15);
            }
        } catch (e) {
            console.error("Time parsing error:", e);
        }
        return steps;
    };

    const timeOptions = useMemo(() => generateTimeSteps(rangeStart, rangeEnd), [rangeStart, rangeEnd]);

    const timeToOptions = useMemo(() => {
        const startIndex = timeOptions.findIndex(opt => opt.value === timeFrom);
        return startIndex !== -1 ? timeOptions.slice(startIndex + 1) : timeOptions;
    }, [timeOptions, timeFrom]);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        setError("");

        try {
            const reservationData = {
                guestNumber: guests,
                tableNumber: tableInfo.tableNumber,
                date: tableInfo.date,
                timeFrom: timeFrom,
                timeTo: timeTo,
            };

            if (isEditMode) {
                await updateReservation({ ...reservationData, id: tableInfo.id });
                onSuccess({ ...reservationData, id: tableInfo.id });
            } else {
                if (!tableInfo?.locationId) {
                    setError("Error: Location ID is missing.");
                    setIsSubmitting(false);
                    return;
                }
                const createData = { ...reservationData, locationId: tableInfo.locationId, guestsCount: guests };
                await createReservation(createData);
                onSuccess(createData);
            }
            onClose();
        } catch (err) {
            const serverData = err.response?.data;
            setError(serverData?.message || serverData?.title || "Operation failed.");
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title={isEditMode ? "Edit Your Reservation" : "Make a Reservation"}
            subtitle={(
                <>
                    You are {isEditMode ? "updating" : "making"} a reservation at <strong>{tableInfo?.location || 'Green & Tasty'}</strong>, <strong>Table {tableInfo?.tableNumber}</strong>, for <strong>{tableInfo?.date}</strong>
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
                    {isSubmitting ? "Processing..." : isEditMode ? "Update Reservation" : "Make a Reservation"}
                </Button>
            </div>
        </Modal>
    );
}