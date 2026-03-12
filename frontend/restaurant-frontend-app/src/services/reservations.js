import { api } from "./api";

export const getClientReservations = async () => {
    const response = await api.get("/reservations"); //
    return response.data;
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
    const response = await api.delete(`/reservations/${id}`); //
    return response.data; // Повертає результат видалення
};

export const updateReservation = async (reservationData) => {
    try {
        const response = await api.put("/reservations", reservationData);
        return response.data;
    } catch (error) {
        console.error("Error updating reservation:", error);
        throw error;
    }
};