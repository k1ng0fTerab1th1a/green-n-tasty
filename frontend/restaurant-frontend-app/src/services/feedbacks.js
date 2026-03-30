import { api } from "./api";

export const submitAuthorisedFeedback = async (feedbackData) => {
    try {
        const response = await api.post("/feedbacks/authorised", feedbackData);
        return response.data;
    } catch (error) {
        console.error("Error submitting feedback:", error);
        throw error;
    }
};

export const getFeedbackShortData = async (reservationId) => {
    const response = await api.get(`/feedbacks/feedback-short-data?reservationId=${reservationId}`);
    return response.data;
};
