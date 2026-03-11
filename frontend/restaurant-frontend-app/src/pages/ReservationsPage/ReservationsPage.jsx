import { useState } from "react";
import {
    BookingCard,
    MainLayout,
    FeedbackModal,
    PageBanner
} from "../../components/index.js";
import styles from "./ReservationsPage.module.css";


const mockReservations = [
    { id: 1, address: "48 Rustaveli Avenue", date: "Oct 14, 2024", time: "12:15 p.m. - 1:45 p.m.", guests: 10, status: "Reserved" },
    { id: 2, address: "14 Baratashvili Street", date: "Oct 16, 2024", time: "10:30 a.m. - 12:00 p.m.", guests: 10, status: "Reserved" },
    { id: 3, address: "14 Baratashvili Street", date: "Sep 14, 2024", time: "10:30 a.m. - 11:30 a.m.", guests: 5, status: "In Progress" },
    { id: 4, address: "14 Baratashvili Street", date: "Jun 6, 2024", time: "10:30 a.m. - 11:30 a.m.", guests: 4, status: "Finished" },
    { id: 5, address: "14 Baratashvili Street", date: "Mar 28, 2024", time: "10:30 a.m. - 11:30 a.m.", guests: 2, status: "Canceled" }
];

const mockStaffData = {
    3: { name: "Mario Jast", role: "Waiter", rating: 4.96, avatar: "/assets/images/waiter1.jpg" },
    4: { name: "Elena Smith", role: "Waiter", rating: 4.85, avatar: "/assets/images/waiter2.jpg" },
};

export default function ReservationsPage() {
    const [isFeedbackOpen, setIsFeedbackOpen] = useState(false);
    const [currentResId, setCurrentResId] = useState(null);
    const [feedbacks, setFeedbacks] = useState({});

    const welcomeTitle = "Hello, Jonson Doe (Customer)";

    const handleOpenFeedback = (id) => {
        setCurrentResId(id);
        setIsFeedbackOpen(true);
    };

    return (
        <MainLayout>
            <div className={styles.page}>
                <PageBanner title={welcomeTitle} />

                <div className={styles.contentContainer}>
                    <div className={styles.grid}>
                        {mockReservations.map((res) => (
                            <BookingCard
                                key={res.id}
                                booking={res}
                                onCancel={() => console.log("Cancel", res.id)}
                                onEdit={() => console.log("Edit", res.id)}
                                onFeedback={() => handleOpenFeedback(res.id)}
                                hasFeedback={!!feedbacks[res.id]}
                            />
                        ))}
                    </div>
                </div>
            </div>

            <FeedbackModal
                isOpen={isFeedbackOpen}
                onClose={() => setIsFeedbackOpen(false)}
                onSubmit={(data) => {
                    setFeedbacks(prev => ({ ...prev, [currentResId]: data }));
                    setIsFeedbackOpen(false);
                }}
                reservationId={currentResId}
                waiter={mockStaffData[currentResId]}
                initialData={feedbacks[currentResId]}
            />
        </MainLayout>
    );
}