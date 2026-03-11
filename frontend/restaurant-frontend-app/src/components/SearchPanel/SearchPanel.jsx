import { useMemo } from "react";
import { Button, Dropdown } from "../index.js";
import styles from "./SearchPanel.module.css";

import locationIcon from "../../assets/icons/pin_bl.svg";
import calendarIcon from "../../assets/icons/calendar_bl.svg";
import timeIcon from "../../assets/icons/clock_bl.svg";
import guestsIcon from "../../assets/icons/people_bl.svg";
import chevronDownIcon from "../../assets/icons/chevron-down.svg";

export default function SearchPanel({
                                        locations = [],
                                        selectedLocation = "",
                                        selectedDate = "",
                                        selectedTime = "",
                                        guests = 1,
                                        onLocationChange,
                                        onDateChange,
                                        onTimeChange,
                                        onGuestsChange,
                                        onSubmit,
                                        className = "",
                                    }) {
    const locationOptions = useMemo(
        () => locations.map((loc) => ({
            value: loc.id ?? loc.address,
            label: loc.address,
        })),
        [locations]
    );

    const handleGuestsMinus = () => guests > 1 && onGuestsChange?.(guests - 1);
    const handleGuestsPlus = () => onGuestsChange?.(guests + 1);

    const handleSubmit = (e) => {
        e.preventDefault();
        onSubmit?.({ locationId: selectedLocation, date: selectedDate, time: selectedTime, guests });
    };

    const getTodayDate = () => new Date().toISOString().split('T')[0];

    return (
        <form className={`${styles.searchPanel} ${className}`} onSubmit={handleSubmit}>
            <div className={styles.row}>
                {/* Location */}
                <div className={styles.col}>
                    <Dropdown
                        value={selectedLocation}
                        onChange={onLocationChange}
                        options={locationOptions}
                        placeholder="Location"
                        leftIcon={<img src={locationIcon} alt="" />}
                        className={styles.dropdownWrap}
                    />
                </div>

                {/* Date */}
                <div className={styles.col}>
                    <div className={styles.inputField}>
                        <img src={calendarIcon} alt="" className={styles.fieldIcon} />
                        <div className={styles.nativeInputWrap}>
                            <input
                                type="date"
                                value={selectedDate}
                                onChange={(e) => onDateChange(e.target.value)}
                                className={styles.nativeInput}
                                min={getTodayDate()}
                            />
                        </div>
                        {selectedDate && selectedDate !== getTodayDate() ? (
                            <button
                                type="button"
                                className={styles.clearBtn}
                                onClick={() => onDateChange(getTodayDate())}
                            >
                                ✕
                            </button>
                        ) : (
                            <img src={chevronDownIcon} alt="" className={styles.chevronIcon} />
                        )}
                    </div>
                </div>

                {/* Time */}
                <div className={styles.col}>
                    <div className={styles.inputField}>
                        <img src={timeIcon} alt="" className={styles.fieldIcon} />
                        <div className={styles.nativeInputWrap}>
                            <input
                                type="time"
                                value={selectedTime}
                                onChange={(e) => onTimeChange(e.target.value)}
                                className={styles.nativeInput}
                            />
                        </div>
                        {selectedTime ? (
                            <button
                                type="button"
                                className={styles.clearBtn}
                                onClick={() => onTimeChange("")}
                            >
                                ✕
                            </button>
                        ) : (
                            <img src={chevronDownIcon} alt="" className={styles.chevronIcon} />
                        )}
                    </div>
                </div>

                {/* Guests */}
                <div className={styles.col}>
                    <div className={styles.guestsField}>
                        <div className={styles.guestsInfo}>
                            <img src={guestsIcon} alt="" className={styles.icon} />
                            <span className={styles.label}>Guests</span>
                        </div>
                        <div className={styles.controls}>
                            <button type="button" onClick={handleGuestsMinus} className={styles.minus}>−</button>
                            <span className={styles.count}>{guests}</span>
                            <button type="button" onClick={handleGuestsPlus} className={styles.plus}>+</button>
                        </div>
                    </div>
                </div>

                <div className={styles.colAction}>
                    <Button type="submit" className={styles.submitBtn}>
                        Find a Table
                    </Button>
                </div>
            </div>
        </form>
    );
}