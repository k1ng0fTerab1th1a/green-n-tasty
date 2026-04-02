import { useState, useEffect, useRef } from "react";
import { useAuth } from "../../../auth/AuthContext.jsx";
import {Modal, Radio, Dropdown, Button, SearchDropdown, Input} from "../../index.js";
import { getAvailableTables } from "../../../services/bookings";
import { getWaiterCustomers } from "../../../services/reservations";
import styles from "./CreateReservationModal.module.css";

import calendarIcon from "../../../assets/icons/calendar.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import guestsIcon from "../../../assets/icons/people.svg";
import chevronDownIcon from "../../../assets/icons/chevron-down.svg";

export default function CreateReservationModal({ isOpen, onClose, onConfirm }) {
    const { auth } = useAuth();
    const dateInputRef = useRef(null);

    const [customerType, setCustomerType] = useState("visitor");
    const [customerSearch, setCustomerSearch] = useState("");
    const [selectedCustomer, setSelectedCustomer] = useState(null);
    const [customersList, setCustomersList] = useState([]);
    const [isApiSearchPaused, setIsApiSearchPaused] = useState(false);

    const [visitorName, setVisitorName] = useState("");

    const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
    const [guests, setGuests] = useState(2);
    const [timeFrom, setTimeFrom] = useState("");
    const [timeTo, setTimeTo] = useState("");
    const [slotStartTime, setSlotStartTime] = useState("");
    const [slotEndTime, setSlotEndTime] = useState("");

    const [table, setTable] = useState("");
    const [availableTables, setAvailableTables] = useState([]);
    const [isLoadingTables, setIsLoadingTables] = useState(false);

    const displayDate = new Date(date).toLocaleDateString("en-US", {
        month: "short", day: "numeric", year: "numeric",
    });

    const timeOptions = Array.from({ length: 24 * 4 }, (_, i) => {
        const hours = Math.floor(i / 4).toString().padStart(2, '0');
        const minutes = ((i % 4) * 15).toString().padStart(2, '0');
        return { value: `${hours}:${minutes}`, label: `${hours}:${minutes}` };
    });

    const buildTimeRange = (start, end) => {
        if (!start || !end) return timeOptions;

        const startIdx = timeOptions.findIndex(o => o.value === start);
        const endIdx = timeOptions.findIndex(o => o.value === end);

        if (startIdx === -1 || endIdx === -1) return timeOptions;

        if (startIdx <= endIdx) {
            return timeOptions.slice(startIdx, endIdx + 1);
        }

        return [
            ...timeOptions.slice(startIdx),
            ...timeOptions.slice(0, endIdx + 1)
        ];
    };

    const timeFromOptions = buildTimeRange(slotStartTime, slotEndTime);
    const minForTo = timeFrom || slotStartTime;
    let timeToOptions = buildTimeRange(minForTo, slotEndTime);
    timeToOptions = timeToOptions.filter(o => o.value !== minForTo);

    useEffect(() => {
        if (!slotStartTime || !slotEndTime) return;

        const rangeFrom = buildTimeRange(slotStartTime, slotEndTime);
        const rangeTo = buildTimeRange(timeFrom || slotStartTime, slotEndTime);

        if (timeFrom && !rangeFrom.some(o => o.value === timeFrom)) {
            setTimeFrom("");
        }
        if (timeTo && !rangeTo.some(o => o.value === timeTo)) {
            setTimeTo("");
        }
    }, [timeFrom, timeTo, slotStartTime, slotEndTime]);

    const handleDateContainerClick = () => {
        if (dateInputRef.current) {
            if (dateInputRef.current.showPicker) {
                dateInputRef.current.showPicker();
            } else {
                dateInputRef.current.focus();
            }
        }
    };

    useEffect(() => {
        if (customerType !== "existing" || customerSearch.length < 2 || isApiSearchPaused) {
            if (isApiSearchPaused) setIsApiSearchPaused(false);
            setCustomersList([]);
            return;
        }

        const delayDebounceFn = setTimeout(async () => {
            const result = await getWaiterCustomers(customerSearch);
            if (result.isSuccess && result.data) {
                const mapped = result.data.map(c => ({
                    id: c.customerId,
                    name: c.username,
                    email: c.maskedEmail
                }));
                setCustomersList(mapped);
            }
        }, 500);

        return () => clearTimeout(delayDebounceFn);
    }, [customerSearch, customerType, isApiSearchPaused]);

    useEffect(() => {
        if (!isOpen) return;

        const parseTimeToMinutes = (timeStr) => {
            if (!timeStr) return null;
            const [h, m] = timeStr.split(":").map(Number);
            return h * 60 + m;
        };


        const minutesToTime = (mins) => {
            const normalized = ((mins % (24 * 60)) + (24 * 60)) % (24 * 60);
            const h = Math.floor(normalized / 60).toString().padStart(2, "0");
            const m = (normalized % 60).toString().padStart(2, "0");
            return `${h}:${m}`;
        };

        const fetchTables = async () => {
            const params = {
                locationId: "location-1",
                date: date,
                guests: guests,
                time: timeFrom || undefined,
            };

            try {
                setIsLoadingTables(true);
                const result = await getAvailableTables(params);

                if (result.isSuccess && result.data) {
                    const mappedTables = result.data.map(t => {
                        const slotsInfo = t.availableSlots && t.availableSlots.length > 0
                            ? t.availableSlots.map(slot => {
                                const startTime = slot.startOffset.includes('T')
                                    ? slot.startOffset.split('T')[1].substring(0, 5)
                                    : slot.startOffset;
                                const endTime = slot.endOffset.includes('T')
                                    ? slot.endOffset.split('T')[1].substring(0, 5)
                                    : slot.endOffset;
                                return `${startTime}-${endTime}`;
                            }).join(', ')
                            : "No slots";

                        return {
                            value: t.tableNumber.toString(),
                            label: `Table ${t.tableNumber} (${t.capacity} pers.) (${slotsInfo})`
                        };
                    });

                    setAvailableTables(mappedTables);

                    let currentTableValue = table;
                    if (!mappedTables.some(t => t.value === table)) {
                        currentTableValue = mappedTables.length > 0 ? mappedTables[0].value : "";
                        setTable(currentTableValue);
                    }

                    if (result.data.length > 0 && currentTableValue) {
                        const selectedTableData = result.data.find(
                            t => t.tableNumber.toString() === currentTableValue
                        ) || result.data[0];

                        if (selectedTableData && selectedTableData.availableSlots && selectedTableData.availableSlots.length > 0) {
                            let minStart = Infinity;
                            let maxEnd = -Infinity;

                            selectedTableData.availableSlots.forEach(slot => {
                                const rawStart = slot.startOffset.includes("T")
                                    ? slot.startOffset.split("T")[1].substring(0, 5)
                                    : slot.startOffset.substring(0, 5);
                                const rawEnd = slot.endOffset.includes("T")
                                    ? slot.endOffset.split("T")[1].substring(0, 5)
                                    : slot.endOffset.substring(0, 5);

                                const s = parseTimeToMinutes(rawStart);
                                let e = parseTimeToMinutes(rawEnd);

                                if (e !== null && s !== null && e < s) {
                                    e += 24 * 60;
                                }

                                if (s !== null && s < minStart) minStart = s;
                                if (e !== null && e > maxEnd) maxEnd = e;
                            });

                            if (minStart !== Infinity && maxEnd !== -Infinity) {
                                const startTime = minutesToTime(minStart);
                                const endTime = minutesToTime(maxEnd);

                                setSlotStartTime(startTime);
                                setSlotEndTime(endTime);

                                setTimeFrom(startTime);
                                setTimeTo(endTime);
                            }
                        } else {
                            setSlotStartTime("");
                            setSlotEndTime("");
                            setTimeFrom("");
                            setTimeTo("");
                        }
                    } else {
                        setSlotStartTime("");
                        setSlotEndTime("");
                        setTimeFrom("");
                        setTimeTo("");
                    }
                }
            } catch (error) {
                console.error("Error fetching tables:", error);
            } finally {
                setIsLoadingTables(false);
            }
        };

        fetchTables();
    }, [date, guests, isOpen]);

    const handleSelectCustomer = (customer) => {
        setSelectedCustomer(customer);
        setIsApiSearchPaused(true);
        setCustomerSearch(customer.name);
        setCustomersList([]);
    };

    const handleInputChange = (value) => {
        if (selectedCustomer && value !== selectedCustomer.name) {
            setSelectedCustomer(null);
        }
        setCustomerSearch(value);
    };

    const handleSubmit = () => {
        const formData = {
            date,
            timeFrom: timeFrom || "12:00",
            timeTo: timeTo || "14:00",
            guestsCount: Number(guests),
            tableNumber: Number(table),
            customerType
        };

        if (customerType === 'existing') {
            formData.customerId = selectedCustomer?.id;
        } else {
            formData.visitorName = visitorName;
        }

        onConfirm(formData);
        onClose();
    };

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="New Reservation">
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
                                onChange={(e) => {
                                    const newDate = e.target.value;
                                    setDate(newDate);
                                    setTimeFrom("");
                                    setCustomerType("visitor");
                                    setCustomerSearch("");
                                    setSelectedCustomer(null);
                                    setVisitorName("");
                                    setTimeTo("");
                                    setSlotStartTime("");
                                    setSlotEndTime("");
                                }}
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
                                onInputChange={handleInputChange}
                                onSelect={handleSelectCustomer}
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
                                placeholder="Enter visitior name ..."
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
                        <span className="ody-bold">Guests</span>
                    </div>
                    <div className={styles.counter}>
                        <button onClick={() => setGuests(prev => Math.max(1, prev - 1))} className={styles.counterBtn}>−</button>
                        <span className="body-bold">{guests}</span>
                        <button onClick={() => setGuests(prev => prev + 1)} className={styles.counterBtn}>+</button>
                    </div>
                </div>

                <div className={styles.timeSection}>
                    <label className="h3">Time</label>
                    <label className="body">Please choose your preferred time from the dropdowns below</label>
                    <div className={styles.timeGrid}>
                        <div className={styles.timeCol}>
                            <label className="body-bold">From</label>
                            <Dropdown
                                value={timeFrom}
                                onChange={setTimeFrom}
                                options={timeFromOptions}
                                leftIcon={clockIcon}
                                disabled={!slotEndTime}
                                placeholder={!slotEndTime ? "Select time" : "Select time"}
                            />
                        </div>
                        <div className={styles.timeCol}>
                            <label className="body-bold">To</label>
                            <Dropdown
                                value={timeTo}
                                onChange={setTimeTo}
                                options={timeToOptions}
                                leftIcon={clockIcon}
                                disabled={!timeFrom || !slotEndTime}
                                placeholder={!timeFrom ? "Select time" : "Select time"}
                            />
                        </div>
                    </div>
                </div>

                <div className={styles.tableSection}>
                    <label className="body-bold">Available tables</label>
                    <Dropdown
                        value={table} onChange={setTable}
                        options={availableTables}
                        placeholder={isLoadingTables ? "Searching..." : "Choose Table"}
                        disabled={isLoadingTables || availableTables.length === 0}
                    />
                </div>

                <Button
                    variant="primary" fullWidth onClick={handleSubmit}
                    disabled={!table || isLoadingTables || (customerType === 'existing' && !selectedCustomer) || (customerType === 'visitor' && !visitorName)}
                >
                    Confirm Reservation
                </Button>
            </div>
        </Modal>
    );
}