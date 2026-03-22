import { useState, useEffect } from "react";
import {
    BookingCard,
    MainLayout,
    PageBanner,
    Toast,
    ReservationForm
} from "../../components/index.js";
import { getClientReservations, deleteReservation } from "../../services/reservations";
import { getAvailableTables } from "../../services/bookings";
import { useAuth } from "../../auth/AuthContext.jsx";
import styles from "./ReservationsPage.module.css";

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

    const formatTimeFromISO = (isoString) => {
        if (!isoString) return "";
        const date = new Date(isoString);
        return date.toLocaleTimeString(undefined, {
            hour: "2-digit",
            minute: "2-digit",
            timeZone: "Asia/Tbilisi",
            hour12: false
        });
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

            let slotsForForm = [];
            const currentUserSlot = {
                startOffset: res.startDateTime,
                endOffset: res.endDateTime
            };

            if (currentTableData && Array.isArray(currentTableData.availableSlots)) {
                slotsForForm = [...currentTableData.availableSlots, currentUserSlot];
            } else {
                slotsForForm = [currentUserSlot];
            }

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
                                const startTime = formatTimeFromISO(res.startDateTime);
                                const endTime = formatTimeFromISO(res.endDateTime);

                                return (
                                    <BookingCard
                                        key={res.id}
                                        booking={{
                                            ...res,
                                            address: res.locationAddress || res.locationId,
                                            date: new Date(res.startDateTime).toLocaleDateString("en-US", {
                                                month: "short", day: "numeric", year: "numeric",
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