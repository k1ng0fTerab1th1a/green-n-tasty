import { Modal,  } from "../index.js";
import styles from "./AvailableSlotsModal.module.css";
import clockIcon from "../../assets/icons/clock.svg";

export default function AvailableSlotsModal({
                                                isOpen,
                                                onClose,
                                                slots = [],
                                                onSlotSelect,
                                                tableInfo
                                            }) {
    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title="Available slots"
            subtitle={(
                <>
                    There are <strong>{slots.length} slots</strong> available at <strong>{tableInfo?.location}</strong>, <strong>Table {tableInfo?.tableNumber}</strong>, for <strong>{tableInfo?.date}</strong>
                </>
            )}
        >
            <div className={styles.grid}>
                {slots.map((slot, index) => (
                    <button
                        key={index}
                        className={styles.slotButton}
                        onClick={() => onSlotSelect(slot)}
                    >
                        <img src={clockIcon} alt="" className={styles.icon} />
                        {slot}
                    </button>
                ))}
            </div>
        </Modal>
    );
}