import { useMemo } from "react";
import { Button, Dropdown } from "../index.js";
import styles from "./SearchPanel.module.css";

import locationIcon from "../../assets/icons/pin_bl.svg";
import calendarIcon from "../../assets/icons/calendar_bl.svg";
import timeIcon from "../../assets/icons/clock_bl.svg";
import guestsIcon from "../../assets/icons/people_bl.svg";

export default function SearchPanel({
                                        locations = [],
                                        dates = [],
                                        times = [],
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

    return (
        <form className={`${styles.searchPanel} ${className}`} onSubmit={handleSubmit}>
            <div className={styles.row}>
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

                <div className={styles.col}>
                    <Dropdown
                        value={selectedDate}
                        onChange={onDateChange}
                        options={dates}
                        placeholder="Date"
                        leftIcon={<img src={calendarIcon} alt="" />}
                        className={styles.dropdownWrap}
                    />
                </div>

                <div className={styles.col}>
                    <Dropdown
                        value={selectedTime}
                        onChange={onTimeChange}
                        options={times}
                        placeholder="Time"
                        leftIcon={<img src={timeIcon} alt="" />}
                        className={styles.dropdownWrap}
                    />
                </div>

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