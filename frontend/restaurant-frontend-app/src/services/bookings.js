import { api } from "./api";

export const getAvailableTables = async (params) => {
    try {
        const response = await api.get("/bookings/tables", { params });
        return response.data;
    } catch (error) {
        console.error("Error fetching available tables:", error);
        throw error;
    }
};