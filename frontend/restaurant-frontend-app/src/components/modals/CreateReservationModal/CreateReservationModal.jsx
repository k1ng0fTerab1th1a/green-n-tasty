import { useState } from "react";
import { Modal, Radio, Dropdown, Button, TableSelector } from "../../index.js";
import styles from "./CreateReservationModal.module.css";

// Іконки
import locationIcon from "../../../assets/icons/pin.svg";
import userIcon from "../../../assets/icons/person.svg";
import clockIcon from "../../../assets/icons/clock.svg";
import guestsIcon from "../../../assets/icons/people.svg";

export default function CreateReservationModal({ isOpen, onClose, onConfirm }) {
    const [location, setLocation] = useState("48 Rustaveli Avenue");
    const [customerType, setCustomerType] = useState("visitor"); // 'visitor' | 'existing'
    const [customerName, setCustomerName] = useState("");
    const [guests, setGuests] = useState(10);
    const [timeFrom, setTimeFrom] = useState("12:15 p.m.");
    const [timeTo, setTimeTo] = useState("1:45 p.m.");
    const [table, setTable] = useState("Table 1");

    const handleGuestsChange = (val) => {
        setGuests(prev => Math.max(1, prev + val));
    };

    const handleSubmit = () => {
        onConfirm({
            location,
            customerType,
            customerName: customerType === 'existing' ? customerName : 'Visitor',
            guests,
            timeFrom,
            timeTo,
            table
        });
        onClose();
    };

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="New Reservation">
            <div className={styles.form}>
                {/* Location Selection */}
                <Dropdown
                    value={location}
                    onChange={setLocation}
                    options={[{ value: "48 Rustaveli Avenue", label: "48 Rustaveli Avenue" }]}
                    leftIcon={locationIcon}
                />

                {/* Customer Type Selection */}
                <div className={styles.radioGroup}>
                    <div className={`${styles.radioCard} ${customerType === 'visitor' ? styles.active : ''}`}>
                        <Radio
                            name="customerType"
                            value="visitor"
                            checked={customerType === 'visitor'}
                            onChange={setCustomerType}
                            label="Visitor"
                        />
                    </div>
                    <div className={`${styles.radioCard} ${customerType === 'existing' ? styles.active : ''}`}>
                        <Radio
                            name="customerType"
                            value="existing"
                            checked={customerType === 'existing'}
                            onChange={setCustomerType}
                            label="Existing Customer"
                        />
                    </div>
                </div>

                {/* Conditional Name Input */}
                {customerType === 'existing' && (
                    <div className={styles.nameInputSection}>
                        <label className="body-bold">Customer's Name</label>
                        <div className={styles.inputWrapper}>
                            <input
                                type="text"
                                placeholder="Enter Customer's Name"
                                value={customerName}
                                onChange={(e) => setCustomerName(e.target.value)}
                                className={styles.input}
                            />
                            <span className={styles.inputHint}>e.g. Jonson Doe</span>
                        </div>
                    </div>
                )}

                {/* Guests Counter */}
                <div className={styles.guestsRow}>
                    <div className={styles.iconLabel}>
                        <img src={guestsIcon} alt="" />
                        <span className="body">Guests</span>
                    </div>
                    <div className={styles.counter}>
                        <button onClick={() => handleGuestsChange(-1)} className={styles.counterBtn}>−</button>
                        <span className="body-bold">{guests}</span>
                        <button onClick={() => handleGuestsChange(1)} className={styles.counterBtn}>+</button>
                    </div>
                </div>

                {/* Time Selection */}
                <div className={styles.timeSection}>
                    <h3 className="body-bold">Time</h3>
                    <p className={styles.timeHint}>Please choose your preferred time from the dropdowns below</p>
                    <div className={styles.timeGrid}>
                        <div className={styles.timeCol}>
                            <label className="caption">From</label>
                            <Dropdown
                                value={timeFrom}
                                onChange={setTimeFrom}
                                options={[{ value: "12:15 p.m.", label: "12:15 p.m." }]}
                                leftIcon={clockIcon}
                            />
                        </div>
                        <div className={styles.timeCol}>
                            <label className="caption">To</label>
                            <Dropdown
                                value={timeTo}
                                onChange={setTimeTo}
                                options={[{ value: "1:45 p.m.", label: "1:45 p.m." }]}
                                leftIcon={clockIcon}
                            />
                        </div>
                    </div>
                </div>

                {/* Table Selection */}
                <Dropdown
                    value={table}
                    onChange={setTable}
                    options={[
                        { value: "Table 1", label: "Table 1" },
                        { value: "Table 2", label: "Table 2" }
                    ]}
                    // Передаємо кастомні класи для зміни стилю
                    className={styles.tableDropdown}
                    controlClassName={styles.tableDropdownControl}
                    placeholder="Select Table"
                />

                <Button variant="primary" fullWidth onClick={handleSubmit}>
                    Make a Reservation
                </Button>
            </div>
        </Modal>
    );
}