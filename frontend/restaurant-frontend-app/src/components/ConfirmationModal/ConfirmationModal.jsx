import Modal from "../Modal/Modal";
import { Button } from "../index.js";
import styles from "./ConfirmationModal.module.css";
import {useNavigate} from "react-router-dom";

export default function ConfirmationModal({ isOpen, onClose, reservationData }) {
    // reservationData: restaurantName, guests, date, timeFrom, timeTo, tableNumber, address
    const navigate = useNavigate();
    const handleCancelRedirect = () => {
        onClose();
        navigate("/reservations");
    };

    const handleEditRedirect = () => {
        onClose();
        navigate("/reservations");
    };

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title="Reservation Confirmed!"
        >
            <div className={styles.container}>
                <div className={styles.content}>
                    <p className="body">
                        Your table reservation at <strong>{reservationData?.restaurantName || 'Green & Tasty'}</strong> for
                        <strong> {reservationData?.guests} people</strong> on <strong>{reservationData?.date}</strong>,
                        from <strong>{reservationData?.timeFrom}</strong> to <strong>{reservationData?.timeTo}</strong> at
                        <strong> Table {reservationData?.tableNumber}</strong> has been successfully made.
                    </p>

                    <p className="body">
                        We look forward to welcoming you at <strong>{reservationData?.address}</strong>.
                    </p>

                    <p className="body">
                        If you need to modify or cancel your reservation, you can do so up to 30 min. before the reservation time.
                    </p>
                </div>

                <div className={styles.actions}>
                    <Button
                        variant="outline"
                        className={styles.cancelBtn}
                        onClick={handleCancelRedirect}
                    >
                        Cancel Reservation
                    </Button>
                    <Button
                        variant="primary"
                        className={styles.editBtn}
                        onClick={handleEditRedirect}
                    >
                        Edit Reservation
                    </Button>
                </div>
            </div>
        </Modal>
    );
}