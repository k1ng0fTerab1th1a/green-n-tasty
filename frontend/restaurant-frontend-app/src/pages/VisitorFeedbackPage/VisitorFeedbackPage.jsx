import { useSearchParams, useNavigate } from "react-router-dom";
import { useState } from "react";
import { MainLayout, FeedbackModal, Toast } from "../../components/index.js";
import { createVisitorFeedback } from "../../services/feedbacks";

export default function VisitorFeedbackPage() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const reservationId = searchParams.get("reservationId");
    const secretCode = searchParams.get("secretCode");

    const [toast, setToast] = useState({
        open: false,
        type: "success",
        title: "",
        message: "",
    });

    const handleClose = () => {
        // куди відправляємо людину після фідбеку
        navigate("/");
    };

    const handleSubmit = async (formData) => {
        if (!secretCode || !reservationId) {
            setToast({
                open: true,
                type: "error",
                title: "Invalid link",
                message: "Feedback link is invalid or incomplete.",
            });
            return;
        }

        const result = await createVisitorFeedback(secretCode, formData);

        if (result.isSuccess) {
            setToast({
                open: true,
                type: "success",
                title: "Thank you!",
                message: "Your feedback has been submitted.",
            });
            handleClose();
        } else {
            setToast({
                open: true,
                type: "error",
                title: "Error",
                message: result.message || "Failed to submit feedback.",
            });
        }
    };

    // якщо посилання битеньке – просто не показуємо форму
    if (!reservationId || !secretCode) {
        return (
            <MainLayout>
                <p style={{ padding: 24 }}>Invalid feedback link.</p>
            </MainLayout>
        );
    }

    return (
        <MainLayout>
            <FeedbackModal
                isOpen={true}
                onClose={handleClose}
                onSubmit={handleSubmit}
                reservationId={reservationId}
                bookingStatus="Finished"
                waiter={null}
            />

            <Toast
                {...toast}
                onClose={() => setToast((prev) => ({ ...prev, open: false }))}
            />
        </MainLayout>
    );
}