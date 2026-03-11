import { api } from "./api";

export const getClientReservations = async () => {
    try {
        const response = await api.get("/reservations");
        return response.data;
    } catch (error) {
        console.error("Error fetching reservations:", error);
        throw error;
    }
};

export const createReservation = async (reservationData) => {
    try {
        const response = await api.post("/reservations/client", reservationData);
        return response.data;
    } catch (error) {
        console.error("Error creating reservation:", error);
        throw error;
    }
};

export const deleteReservation = async (id) => {
    try {
        const response = await api.delete(`/reservations/${id}`);
        return response.data;
    } catch (error) {
        console.error("Error deleting reservation:", error);
        throw error;
    }
};