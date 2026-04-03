import { useSearchParams, useNavigate } from "react-router-dom";
import { useState, useEffect } from "react";
import { MainLayout, FeedbackModal, Toast } from "../../components/index.js";
import { createVisitorFeedback, getFeedbackShortData } from "../../services/feedbacks";

export default function VisitorFeedbackPage() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const reservationId = searchParams.get("reservationId");
    const secretCode = searchParams.get("secretCode");

    const [feedbackData, setFeedbackData] = useState(null);
    const [waiter, setWaiter] = useState(null);
    const [loading, setLoading] = useState(true);

    const [toast, setToast] = useState({
        open: false, type: "success", title: "", message: "",
    });

    useEffect(() => {
        const fetchInitialInfo = async () => {
            if (!reservationId) return;

            try {
                setLoading(true);
                const result = await getFeedbackShortData(reservationId);

                if (result.isSuccess && result.data) {
                    const d = result.data;

                    if (d.waiterName) {
                        setWaiter({
                            name: d.waiterName,
                            role: d.waiterRole || "Waiter",
                            rating: d.waiterRating || 5,
                            avatar: d.waiterImageUrl || ""
                        });
                    }

                    setFeedbackData({
                        serviceRating: 0,
                        serviceComment: d.serviceComment || "",
                        culinaryRating: 0,
                        cuisineComment: d.cuisineComment || "",
                    });
                }
            } catch (err) {
                console.error("Помилка при отриманні даних:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchInitialInfo();
    }, [reservationId]);

    const handleClose = () => navigate("/");

    const handleSubmit = async (formData) => {
        if (!secretCode || !reservationId) {
            setToast({ open: true, type: "error", title: "Error", message: "Invalid link." });
            return;
        }

        const cleanData = { reservationId: formData.reservationId };

        if (formData.serviceRating > 0) {
            cleanData.serviceRating = formData.serviceRating;
            cleanData.serviceComment = formData.serviceComment;
        }

        if (formData.culinaryRating > 0) {
            cleanData.cuisineRating = formData.culinaryRating;
            cleanData.cuisineComment = formData.cuisineComment;
        }

        const result = await createVisitorFeedback(secretCode, cleanData);
        if (result.isSuccess) {
            setToast({ open: true, type: "success", title: "Success", message: "Feedback sent!" });
            setTimeout(handleClose, 2000);
        } else {
            setToast({ open: true, type: "error", title: "Failed", message: result.message });
        }
    };

    if (loading) return <MainLayout><div>Loading...</div></MainLayout>;

    return (
        <MainLayout>
            <FeedbackModal
                isOpen={true}
                onClose={handleClose}
                onSubmit={handleSubmit}
                reservationId={reservationId}
                bookingStatus="Finished"
                waiter={waiter}
                initialData={feedbackData}
            />
            <Toast {...toast} onClose={() => setToast(p => ({ ...p, open: false }))} />
        </MainLayout>
    );
}