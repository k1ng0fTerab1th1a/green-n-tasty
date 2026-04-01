import { api } from "./api";

export const getClientReservations = async () => {
    const response = await api.get("/reservations/customer"); //
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

// ❗ Приводимо до такого ж формату isSuccess, як інші waiter‑методи
export const updateReservation = async (reservationData) => {
    try {
        const response = await api.put("/reservations", reservationData);
        return response.data; // очікується { isSuccess, message?, ... }
    } catch (error) {
        console.error("Error updating reservation:", error);
        return {
            isSuccess: false,
            message: error.response?.data?.message || error.message
        };
    }
};

export const getWaiterReservations = async (date) => {
    try {
        const response = await api.get('/reservations/waiter', {
            params: { date }
        });
        return response.data;
    } catch (error) {
        return {
            isSuccess: false,
            message: error.response?.data?.message || error.message
        };
    }
};

export const createWaiterReservation = async (reservationData) => {
    try {
        const response = await api.post('/reservations/waiter', reservationData);
        return response.data;
    } catch (error) {
        return {
            isSuccess: false,
            message: error.response?.data?.message || error.message
        };
    }
};

export const getWaiterCustomers = async (query = "") => {
    try {
        const response = await api.get('/reservations/waiter/customers', {
            params: { query }
        });
        return response.data;
    } catch (error) {
        return { isSuccess: false, message: error.message };
    }
};

export async function getReservationById(id) {
    try {
        const response = await api.get(`/reservations/${id}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching reservation by id:", error);
        return {
            isSuccess: false,
            message: "Failed to load reservation details",
        };
    }
}

export const startReservation = async (id) => {
    try {
        const response = await api.post(`/reservations/${id}/start`);
        return response.data;         // очікуємо { isSuccess, message, data? }
    } catch (error) {
        console.error("Error starting reservation:", error);
        return {
            isSuccess: false,
            message: error.response?.data?.message || error.message,
        };
    }
};

export const setMealsServed = async (id) => {
    try {
        const response = await api.post(`/reservations/${id}/meals-served`);
        return response.data;
    } catch (error) {
        console.error("Error setting meals served:", error);
        return {
            isSuccess: false,
            message: error.response?.data?.message || error.message,
        };
    }
};

export const finishReservation = async (id) => {
    try {
        const response = await api.post(`/reservations/${id}/finish`);
        return response.data;
    } catch (error) {
        console.error("Error finishing reservation:", error);
        return {
            isSuccess: false,
            message: error.response?.data?.message || error.message,
        };
    }
};

// export const getReservationReceipt = async (reservationId) => {
//     try {
//         const response = await api.get(`/reservations/${reservationId}/receipt`, {
//             responseType: "text",       // бекенд віддає text/plain
//         });
//         // очікуємо просто рядок з текстом чеку
//         return {
//             isSuccess: true,
//             data: response.data,
//         };
//     } catch (error) {
//         console.error("Error fetching reservation receipt:", error);
//         return {
//             isSuccess: false,
//             message: error.response?.data?.message || error.message,
//         };
//     }
// };