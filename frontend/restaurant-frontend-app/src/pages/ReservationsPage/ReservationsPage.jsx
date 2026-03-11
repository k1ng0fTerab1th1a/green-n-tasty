import { useState, useEffect } from "react";
import {
    BookingCard,
    MainLayout,
    FeedbackModal,
    PageBanner,
    Toast
} from "../../components/index.js";
import styles from "./ReservationsPage.module.css";
import { getClientReservations, deleteReservation } from "../../services/reservations";
import { useAuth } from "../../auth/AuthContext.jsx";

export default function ReservationsPage() {
    const { auth } = useAuth(); // Отримуємо дані авторизації з контексту
    const [reservations, setReservations] = useState([]);
    const [loading, setLoading] = useState(true);

    const [toast, setToast] = useState({ open: false, type: "success", title: "", message: "" });

    const welcomeTitle = `Hello, ${auth.username || "Guest"}`;

    const loadData = async () => {
        try {
            setLoading(true);
            const result = await getClientReservations();
            if (result.isSuccess) {
                setReservations(result.data || []);
            }
        } catch (error) {
            showToast("error", "Error", "Failed to load reservations");
        } finally {
            setLoading(false);
        }
    };

    const showToast = (type, title, message) => {
        setToast({ open: true, type, title, message });
    };

    const handleCancel = async (id) => {
        try {
            const result = await deleteReservation(id);

            if (result.isSuccess) {
                showToast("success", "Success", "Reservation cancelled");
                await loadData();
            } else {
                showToast("error", "Cancellation Failed", result.message);
            }
        } catch (error) {
            const errorMessage = error.response?.data?.message || "Something went wrong";
            showToast("error", "Error", errorMessage);
        }
    };

    useEffect(() => { loadData(); }, []);

    return (
        <MainLayout>
            <div className={styles.page}>
                {/* Передаємо динамічний заголовок у PageBanner */}
                <PageBanner title={welcomeTitle} />
                <div className={styles.contentContainer}>
                    {loading ? (
                        <div className={styles.stateMessage}>Loading...</div>
                    ) : reservations.length === 0 ? (
                        <div className={styles.stateMessage}>You don't have any reservations.</div>
                    ) : (
                        <div className={styles.grid}>
                            {reservations.map((res) => (
                                <BookingCard
                                    key={res.id}
                                    booking={{
                                        ...res,
                                        address: res.locationId,
                                        date: new Date(res.startDateTime).toLocaleDateString(),
                                        time: `${new Date(res.startDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - ${new Date(res.endDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`,
                                        guests: res.guestsCount,
                                        status: res.status
                                    }}
                                    onCancel={() => handleCancel(res.id)}
                                />
                            ))}
                        </div>
                    )}
                </div>
            </div>

            <Toast
                {...toast}
                onClose={() => setToast(prev => ({ ...prev, open: false }))}
            />
        </MainLayout>
    );
}