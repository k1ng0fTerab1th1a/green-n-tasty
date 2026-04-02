import { useState, useEffect, useRef } from "react";
import { useAuth } from "../../../auth/AuthContext.jsx";
import { Modal, Radio, Dropdown, Button, SearchDropdown, Input } from "../../index.js";
import { getAvailableTables } from "../../../services/bookings";
import { getWaiterCustomers, getReservationById } from "../../../services/reservations";
import styles from "../CreateReservationModal/CreateReservationModal.module.css";

import calendarIcon from "../../../assets/icons/calendar.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import guestsIcon from "../../../assets/icons/people.svg";
import chevronDownIcon from "../../../assets/icons/chevron-down.svg";

export default function EditReservationModal({ isOpen, onClose, onConfirm, reservation }) {
    const { auth } = useAuth();
    const dateInputRef = useRef(null);

    const [customerType, setCustomerType] = useState("visitor");
    const [customerSearch, setCustomerSearch] = useState("");
    const [selectedCustomer, setSelectedCustomer] = useState(null);
    const [customersList, setCustomersList] = useState([]);
    const [isApiSearchPaused, setIsApiSearchPaused] = useState(false);
    const [visitorName, setVisitorName] = useState("");

    const [date, setDate] = useState("");
    const [guests, setGuests] = useState(1);
    const [timeFrom, setTimeFrom] = useState("");
    const [timeTo, setTimeTo] = useState("");
    const [slotStartTime, setSlotStartTime] = useState("");
    const [slotEndTime, setSlotEndTime] = useState("");

    const [table, setTable] = useState("");
    const [availableTables, setAvailableTables] = useState([]);
    const [isLoadingTables, setIsLoadingTables] = useState(false);

    const parseTimeToMinutes = (timeStr) => {
        if (!timeStr) return null;
        const [h, m] = timeStr.split(":").map(Number);
        return h * 60 + m;
    };

    const minutesToTime = (mins) => {
        const normalized = ((mins % (24 * 60)) + (24 * 60)) % (24 * 60); // щоб коректно обробити >24 год
        const h = Math.floor(normalized / 60).toString().padStart(2, "0");
        const m = (normalized % 60).toString().padStart(2, "0");
        return `${h}:${m}`;
    };

    const extractTimeFromOffset = (offset) => {
        if (!offset) return "";
        if (offset.includes("T")) {
            return offset.split("T")[1].substring(0, 5);
        }
        return offset.substring(0, 5);
    };

    // useEffect(() => {
    //     if (!isOpen) {
    //         setCustomerType("visitor");
    //         setCustomerSearch("");
    //         setSelectedCustomer(null);
    //         setCustomersList([]);
    //         setIsApiSearchPaused(false);
    //         setVisitorName("");
    //
    //         setDate("");
    //         setGuests(1);
    //         setTimeFrom("");
    //         setTimeTo("");
    //         setSlotStartTime("");
    //         setSlotEndTime("");
    //
    //         setTable("");
    //         setAvailableTables([]);
    //     }
    // }, [isOpen, reservation?.id]);

    // Заповнення полів при відкритті
    useEffect(() => {
        if (!isOpen || !reservation) return;

        const fillFromSource = (src) => {
            if (!src) return;

            const apiDate = src.date || (src.startDateTime ? src.startDateTime.split("T")[0] : "");
            const apiTimeFrom =
                src.timeFrom ||
                (src.startDateTime ? src.startDateTime.slice(11, 16) : "");
            const apiTimeTo =
                src.timeTo ||
                (src.endDateTime ? src.endDateTime.slice(11, 16) : "");

            setDate(apiDate || "");
            setGuests(
                src.guestNumber != null
                    ? src.guestNumber
                    : src.guestsCount || 1
            );
            setTimeFrom(apiTimeFrom || "");
            setTimeTo(apiTimeTo || "");
            setTable(src.tableNumber != null ? src.tableNumber.toString() : "");

            if (src.customerName) {
                setCustomerType("existing");
                setSelectedCustomer({
                    id: src.customerId || "virtual-id",
                    name: src.customerName,
                });
                setCustomerSearch(src.customerName);
                setVisitorName("");
                setIsApiSearchPaused(true);
            } else if (src.visitorName || src.waiterName) {
                setCustomerType("visitor");
                setSelectedCustomer(null);
                setCustomerSearch("");
                setVisitorName(src.visitorName || src.waiterName || "");
            } else {
                setCustomerType("visitor");
                setSelectedCustomer(null);
                setCustomerSearch("");
                setVisitorName("");
            }
        };

        fillFromSource(reservation);

        const loadFromApi = async () => {
            if (!reservation.id) return;

            const result = await getReservationById(reservation.id);
            const data = result && result.data ? result.data : result;

            if (!result || result.isSuccess === false || !data) return;

            fillFromSource(data);
        };

        loadFromApi();
    }, [isOpen, reservation]);



    const displayDate = date
        ? new Date(date).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
        })
        : "Select date";

    const timeOptions = Array.from({ length: 24 * 4 }, (_, i) => {
        const hours = Math.floor(i / 4)
            .toString()
            .padStart(2, "0");
        const minutes = ((i % 4) * 15).toString().padStart(2, "0");
        return { value: `${hours}:${minutes}`, label: `${hours}:${minutes}` };
    });

    const buildTimeRange = (start, end) => {
        if (!start || !end) return timeOptions;
        const startIdx = timeOptions.findIndex((o) => o.value === start);
        const endIdx = timeOptions.findIndex((o) => o.value === end);
        if (startIdx === -1 || endIdx === -1) return timeOptions;
        return startIdx <= endIdx
            ? timeOptions.slice(startIdx, endIdx + 1)
            : [
                ...timeOptions.slice(startIdx),
                ...timeOptions.slice(0, endIdx + 1),
            ];
    };

    const minForTo = timeFrom || slotStartTime;
    let timeToOptions = buildTimeRange(minForTo, slotEndTime).filter(
        (o) => o.value !== minForTo
    );

    const handleDateContainerClick = () => {
        if (dateInputRef.current?.showPicker) dateInputRef.current.showPicker();
        else dateInputRef.current?.focus();
    };

    // Пошук
    useEffect(() => {
        if (
            customerType !== "existing" ||
            customerSearch.length < 2 ||
            isApiSearchPaused
        ) {
            if (isApiSearchPaused) setIsApiSearchPaused(false);
            setCustomersList([]);
            return;
        }
        const delayDebounceFn = setTimeout(async () => {
            const result = await getWaiterCustomers(customerSearch);
            if (result.isSuccess && result.data) {
                setCustomersList(
                    result.data.map((c) => ({
                        id: c.customerId,
                        name: c.username,
                        email: c.maskedEmail,
                    }))
                );
            }
        }, 500);
        return () => clearTimeout(delayDebounceFn);
    }, [customerSearch, customerType, isApiSearchPaused]);

    useEffect(() => {
        if (!isOpen || !date) return;

        const fetchTables = async () => {
            setIsLoadingTables(true);
            try {
                const currentTableNumber = reservation?.tableNumber;

                const params = {
                    locationId: "location-1",
                    date,
                    guests,
                    excludeReservationId: reservation?.id || undefined,
                };

                const result = await getAvailableTables(params);
                let tablesFromApi = (result.isSuccess && result.data) ? result.data : [];

                const processedTables = tablesFromApi.map(t => {
                    const isCurrentTable = Number(t.tableNumber) === Number(currentTableNumber);

                    let segments = (t.availableSlots || []).map(s => {
                        const start = parseTimeToMinutes(extractTimeFromOffset(s.startOffset));
                        let end = parseTimeToMinutes(extractTimeFromOffset(s.endOffset));
                        if (end !== null && start !== null && end < start) {
                            end += 24 * 60; // слот через північ
                        }
                        return { start, end };
                    }).filter(seg => seg.start !== null && seg.end !== null);

                    // 🔹 ДОДАЄМО ІНТЕРВАЛ ПОТОЧНОЇ РЕЗЕРВАЦІЇ ДЛЯ ПОТОЧНОГО СТОЛУ
                    if (isCurrentTable) {
                        let resStartMin = parseTimeToMinutes(timeFrom);
                        let resEndMin   = parseTimeToMinutes(timeTo);

                        if (resEndMin !== null && resStartMin !== null && resEndMin < resStartMin) {
                            // якщо резервація теж може перетинати північ
                            resEndMin += 24 * 60;
                        }

                        if (resStartMin !== null && resEndMin !== null) {
                            segments.push({ start: resStartMin, end: resEndMin });
                        }
                    }

                    if (segments.length === 0) return t;

                    segments.sort((a, b) => a.start - b.start);

                    const merged = [];
                    let current = segments[0];

                    for (let i = 1; i < segments.length; i++) {
                        const GAP_LIMIT = 15;          // ⬅ якщо перерва ≤ 15 хв – склеюємо
                        if (segments[i].start <= current.end + GAP_LIMIT) {
                            current.end = Math.max(current.end, segments[i].end);
                        } else {
                            merged.push(current);
                            current = segments[i];
                        }
                    }
                    merged.push(current);

                    return {
                        ...t,
                        availableSlots: merged.map(seg => ({
                            startOffset: minutesToTime(seg.start),
                            endOffset: minutesToTime(seg.end)
                        }))
                    };
                });

                const mappedOptions = processedTables.map((t) => {
                    const isCurrentTable = Number(t.tableNumber) === Number(currentTableNumber);
                    let slotsLabel = "";

                    if (t.availableSlots && t.availableSlots.length > 0) {
                        const formatSlot = (s) => `${s.startOffset}–${s.endOffset}`;

                        if (isCurrentTable) {
                            const resStartMin = parseTimeToMinutes(timeFrom);
                            const currentSlotIndex = t.availableSlots.findIndex(s => {
                                const sMin = parseTimeToMinutes(s.startOffset);
                                const eMin = parseTimeToMinutes(s.endOffset);
                                return resStartMin >= sMin && resStartMin <= eMin;
                            });

                            if (currentSlotIndex !== -1) {
                                const currentSlot = t.availableSlots[currentSlotIndex];
                                const otherSlots = t.availableSlots
                                    .filter((_, idx) => idx !== currentSlotIndex)
                                    .map(formatSlot);

                                const parts = [formatSlot(currentSlot), ...otherSlots];
                                slotsLabel = ` (${parts.join(", ")})`;
                            } else {
                                const parts = t.availableSlots.map(formatSlot);
                                slotsLabel = ` (${parts.join(", ")})`;
                            }
                        } else {
                            const parts = t.availableSlots.map(formatSlot);
                            slotsLabel = ` (${parts.join(", ")})`;
                        }
                    }

                    return {
                        value: t.tableNumber.toString(),
                        label: `Table ${t.tableNumber} (${t.capacity} pers.)${slotsLabel}`,
                    };
                });

                setAvailableTables(mappedOptions);

                if (currentTableNumber && !table) {
                    setTable(currentTableNumber.toString());
                }

            } catch (error) {
                console.error("Error fetching tables:", error);
            } finally {
                setIsLoadingTables(false);
            }
        };

        fetchTables();
    }, [date, guests, isOpen, reservation, timeFrom, timeTo]);

    const handleSubmit = () => {
        const updateData = {
            id: reservation.id,
            guestNumber: Number(guests),
            tableNumber: Number(table),
            date: date,
            timeFrom: timeFrom,
            timeTo: timeTo,
        };

        onConfirm(updateData);
        onClose();
    };

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="Edit Reservation">
            <div className={styles.form}>
                <div className={styles.inputSection}>
                    <label className="h3">Date</label>
                    <div className={styles.filterInputGroup} onClick={handleDateContainerClick}>
                        <img src={calendarIcon} alt="" className={styles.fieldIcon} />
                        <div className={styles.nativeInputWrap}>
                            <input
                                ref={dateInputRef}
                                type="date"
                                value={date}
                                onChange={(e) => setDate(e.target.value)}
                                className={styles.nativeInput}
                            />
                            <span className="body-bold">{displayDate}</span>
                        </div>
                        <img src={chevronDownIcon} alt="" className={styles.chevronIcon} />
                    </div>
                </div>

                <div className={styles.radioGroup}>
                    <div className={`${styles.radioCard} ${customerType === 'visitor' ? styles.active : ''}`}>
                        <Radio
                            name="customerType" value="visitor"
                            checked={customerType === 'visitor'}
                            onChange={(val) => { setCustomerType(val); setCustomerSearch(""); setSelectedCustomer(null); }}
                            label="Visitor"
                        />
                    </div>
                    <div className={`${styles.radioCard} ${customerType === 'existing' ? styles.active : ''}`}>
                        <Radio
                            name="customerType" value="existing"
                            checked={customerType === 'existing'}
                            onChange={(val) => { setCustomerType(val); setVisitorName(""); }}
                            label="Customer"
                        />
                    </div>

                    {customerType === 'existing' ? (
                        <div className={styles.dropdownContainer}>
                            <SearchDropdown
                                items={customersList}
                                searchKey="name"
                                value={customerSearch}
                                placeholder="Enter Customer’s Name ..."
                                label="Customer’s Name"
                                onInputChange={(v) => { if (selectedCustomer && v !== selectedCustomer.name) setSelectedCustomer(null); setCustomerSearch(v); }}
                                onSelect={(c) => { setSelectedCustomer(c); setIsApiSearchPaused(true); setCustomerSearch(c.name); setCustomersList([]); }}
                                renderItem={(item) => (
                                    <span>{item.name} <small style={{color: '#666'}}>({item.email})</small></span>
                                )}
                            />
                        </div>
                    ) : (
                        <div className={styles.inputWrapper}>
                            <label className="body-bold">Visitor's Name</label>
                            <Input
                                type="text"
                                placeholder="Enter visitor name ..."
                                value={visitorName}
                                onChange={(e) => setVisitorName(e.target.value)}
                                className={styles.input}
                            />
                        </div>
                    )}
                </div>

                <div className={styles.guestsRow}>
                    <div className={styles.iconLabel}>
                        <img src={guestsIcon} alt="" />
                        <span className="body-bold">Guests</span>
                    </div>
                    <div className={styles.counter}>
                        <button onClick={() => setGuests(prev => Math.max(1, prev - 1))} className={styles.counterBtn}>−</button>
                        <span className="body-bold">{guests}</span>
                        <button onClick={() => setGuests(prev => prev + 1)} className={styles.counterBtn}>+</button>
                    </div>
                </div>

                <div className={styles.timeSection}>
                    <div className={styles.timeGrid}>
                        <div className={styles.timeCol}>
                            <label className="body-bold">From</label>
                            <Dropdown value={timeFrom} onChange={setTimeFrom} options={timeOptions} leftIcon={clockIcon} />
                        </div>
                        <div className={styles.timeCol}>
                            <label className="body-bold">To</label>
                            <Dropdown value={timeTo} onChange={setTimeTo} options={timeToOptions} leftIcon={clockIcon} disabled={!timeFrom} />
                        </div>
                    </div>
                </div>

                <div className={styles.tableSection}>
                    <label className="body-bold">Available tables</label>
                    <Dropdown
                        value={table} onChange={setTable}
                        options={availableTables}
                        placeholder={isLoadingTables ? "Searching..." : "Choose Table"}
                        disabled={isLoadingTables}
                    />
                </div>

                <Button
                    variant="primary" fullWidth onClick={handleSubmit}
                    disabled={!table || (customerType === 'existing' && !selectedCustomer) || (customerType === 'visitor' && !visitorName)}
                >
                    Save Changes
                </Button>
            </div>
        </Modal>
    );
}