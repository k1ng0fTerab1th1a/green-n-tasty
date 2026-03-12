import { useState, useEffect } from "react";
import {
    BookingCard,
    MainLayout,
    PageBanner,
    Toast,
    ReservationForm
} from "../../components/index.js";
import styles from "./ReservationsPage.module.css";
import { getClientReservations, deleteReservation } from "../../services/reservations";
import { getAvailableTables } from "../../services/bookings";
import { useAuth } from "../../auth/AuthContext.jsx";

export default function ReservationsPage() {
    const { auth } = useAuth();
    const [reservations, setReservations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [selectedReservation, setSelectedReservation] = useState(null);
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

    const handleEditClick = async (res) => {
        try {
            setLoading(true);

            const reservationDate = res.startDateTime.split('T')[0];
            const response = await getAvailableTables({
                locationId: res.locationId,
                date: reservationDate,
                guests: res.guestsCount,
                time: ""
            });

            const tables = response.data || [];
            const currentTableData = tables.find(t => t.tableNumber === res.tableNumber);

            const formatTimeFromISO = (isoString) => {
                if (!isoString) return "";
                const timePart = isoString.split('T')[1].split('+')[0].split('-')[0];
                let [hours, minutes] = timePart.split(':');
                hours = parseInt(hours, 10);
                const ampm = hours >= 12 ? 'pm' : 'am';
                hours = hours % 12;
                hours = hours ? hours : 12;
                return `${hours}:${minutes} ${ampm}`;
            };

            let slotsForForm = [];

            // ВИПРАВЛЕНО: Додаємо поточний слот користувача до масиву доступних слотів
            const currentUserSlot = {
                startOffset: res.startDateTime,
                endOffset: res.endDateTime
            };

            if (currentTableData && Array.isArray(currentTableData.availableSlots)) {
                // Об'єднуємо отримані вільні слоти з поточним часом користувача
                slotsForForm = [...currentTableData.availableSlots, currentUserSlot];
            } else {
                slotsForForm = [currentUserSlot];
            }

            // Сортуємо об'єднаний пул слотів, щоб знайти справжні межі "Від" і "До"
            const sortedSlots = [...slotsForForm].sort((a, b) =>
                a.startOffset.localeCompare(b.startOffset)
            );

            const workingHoursStart = formatTimeFromISO(sortedSlots[0].startOffset);
            const workingHoursEnd = formatTimeFromISO(sortedSlots[sortedSlots.length - 1].endOffset);

            setSelectedReservation({
                id: res.id,
                location: res.locationAddress || res.locationId,
                locationId: res.locationId,
                tableNumber: res.tableNumber,
                guestsCount: res.guestsCount,
                capacity: currentTableData?.capacity || 4,
                date: reservationDate,
                timeFrom: formatTimeFromISO(res.startDateTime),
                timeTo: formatTimeFromISO(res.endDateTime),
                availableSlots: slotsForForm,
                workingHoursStart,
                workingHoursEnd
            });

            setIsEditModalOpen(true);
        } catch (error) {
            showToast("error", "Error", "Failed to refresh table data");
        } finally {
            setLoading(false);
        }
    };

    const handleUpdateSuccess = () => {
        showToast("success", "Updated", "Reservation updated successfully!");
        loadData();
    };

    useEffect(() => { loadData(); }, []);

    return (
        <MainLayout>
            <div className={styles.page}>
                <PageBanner title={welcomeTitle} />
                <div className={styles.contentContainer}>
                    {loading ? (
                        <div className={styles.stateMessage}>Loading...</div>
                    ) : reservations.length === 0 ? (
                        <div className={styles.stateMessage}>You don't have any reservations.</div>
                    ) : (
                        <div className={styles.grid}>
                            {reservations.map((res) => {
                                const formatTimeFromISO = (isoString) => {
                                    if (!isoString) return "";
                                    const timePart = isoString.split('T')[1].split('+')[0].split('-')[0];
                                    let [hours, minutes] = timePart.split(':');
                                    hours = parseInt(hours, 10);
                                    const ampm = hours >= 12 ? 'pm' : 'am';
                                    hours = hours % 12;
                                    hours = hours ? hours : 12;
                                    return `${hours}:${minutes} ${ampm}`;
                                };

                                const startTime = formatTimeFromISO(res.startDateTime);
                                const endTime = formatTimeFromISO(res.endDateTime);

                                return (
                                    <BookingCard
                                        key={res.id}
                                        booking={{
                                            ...res,
                                            address: res.locationAddress || res.locationId,
                                            date: new Date(res.startDateTime).toLocaleDateString("en-US", {
                                                month: "short",
                                                day: "numeric",
                                                year: "numeric",
                                            }),
                                            time: `${startTime} - ${endTime}`,
                                            guests: res.guestsCount,
                                            status: res.status
                                        }}
                                        onCancel={() => handleCancel(res.id)}
                                        onEdit={() => handleEditClick(res)}
                                    />
                                );
                            })}
                        </div>
                    )}
                </div>
            </div>

            {selectedReservation && (
                <ReservationForm
                    isOpen={isEditModalOpen}
                    onClose={() => setIsEditModalOpen(false)}
                    onSuccess={handleUpdateSuccess}
                    tableInfo={selectedReservation}
                />
            )}

            <Toast
                {...toast}
                onClose={() => setToast(prev => ({ ...prev, open: false }))}
            />
        </MainLayout>
    );
}