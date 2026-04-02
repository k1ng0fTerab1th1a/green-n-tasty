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


export async function createVisitorFeedback(secretCode, payload) {
    try {
        const body = {
            reservationId: payload.reservationId,
        };

        if (payload.serviceRating && payload.serviceRating > 0) {
            body.serviceRating = payload.serviceRating;
        }
        if (payload.serviceComment && payload.serviceComment.trim()) {
            body.serviceComment = payload.serviceComment.trim();
        }
        if (payload.culinaryRating && payload.culinaryRating > 0) {
            body.cuisineRating = payload.culinaryRating;
        }
        if (payload.cuisineComment && payload.cuisineComment.trim()) {
            body.cuisineComment = payload.cuisineComment.trim();
        }

        const response = await api.post("/feedbacks/visitor", body, {
            params: { secretCode },
        });

        return {
            isSuccess: response.data?.isSuccess ?? true,
            data: response.data?.data ?? response.data,
            message: response.data?.message,
        };
    } catch (error) {
        return {
            isSuccess: false,
            message:
                error?.response?.data?.message ||
                error?.message ||
                "Failed to submit visitor feedback",
        };
    }
}

export async function getFeedbackShortData(reservationId) {
    try {
        const response = await api.get("/feedbacks/feedback-short-data", {
            params: { reservationId },
        });
        const body = response.data;

        return {
            isSuccess: body?.isSuccess ?? true,
            data: body?.data ?? body,
            message: body?.message,
        };
    } catch (error) {
        return {
            isSuccess: false,
            message:
                error?.response?.data?.message ||
                error?.message ||
                "Failed to load feedback data",
        };
    }
}

export async function getFeedbackShortUpdateData(reservationId) {
    try {
        const response = await api.get("/feedbacks/feedback-short-update-data", {
            params: { reservationId },
        });

        const body = response.data;

        return {
            isSuccess: body?.isSuccess ?? true,
            data: body?.data ?? body,
            message: body?.message,
        };
    } catch (error) {
        return {
            isSuccess: false,
            message:
                error?.response?.data?.message ||
                error?.message ||
                "Failed to load feedback update data",
        };
    }
}

export async function updateAuthorisedFeedback(feedbackData) {
    try {
        const body = { reservationId: feedbackData.reservationId };

        if (feedbackData.serviceRating && feedbackData.serviceRating > 0) {
            body.serviceRating = feedbackData.serviceRating;
        }
        if (feedbackData.serviceComment && feedbackData.serviceComment.trim()) {
            body.serviceComment = feedbackData.serviceComment.trim();
        }
        if (feedbackData.cuisineRating && feedbackData.cuisineRating > 0) {
            body.cuisineRating = feedbackData.cuisineRating;
        }
        if (feedbackData.cuisineComment && feedbackData.cuisineComment.trim()) {
            body.cuisineComment = feedbackData.cuisineComment.trim();
        }

        const response = await api.put("/feedbacks/update-feedback", body);

        const resBody = response.data;
        return {
            isSuccess: resBody?.isSuccess ?? true,
            data: resBody?.data ?? resBody,
            message: resBody?.message,
        };
    } catch (error) {
        return {
            isSuccess: false,
            message:
                error?.response?.data?.message ||
                error?.message ||
                "Failed to update feedback",
        };
    }
}