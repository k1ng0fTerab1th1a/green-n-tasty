import { api } from "./api";

function mapError(error, fallbackMessage) {
    return (
        error?.response?.data?.message ||
        error?.message ||
        fallbackMessage
    );
}

export async function createOrder(reservationId, dishes) {
    try {
        const payload = {
            reservationId,
            dishes: (dishes || []).map(d => ({
                dishId: d.id,
                quantity: d.quantity,
            })),
        };

        const response = await api.post("/orders", payload);
        return { isSuccess: true, data: response.data };
    } catch (error) {
        return {
            isSuccess: false,
            message: mapError(error, "Failed to create order"),
        };
    }
}

export async function getOrderByReservation(reservationId) {
    try {
        const response = await api.get(`/orders/reservations/${reservationId}`);
        // Очікуємо структуру { isSuccess, message, data }
        const body = response.data;
        return {
            isSuccess: body?.isSuccess ?? true,
            data: body?.data ?? body,
            message: body?.message,
        };
    } catch (error) {
        return {
            isSuccess: false,
            message: mapError(error, "Failed to load order"),
        };
    }
}

export async function addDishToOrder(reservationId, { operationId, dishId, quantity }) {
    try {
        const payload = { operationId, dishId, quantity };
        const response = await api.post(
            `/orders/reservations/${reservationId}/dishes`,
            payload
        );
        return { isSuccess: true, data: response.data };
    } catch (error) {
        return {
            isSuccess: false,
            message: mapError(error, "Failed to add dish to order"),
        };
    }
}

export async function removeDishFromOrder(reservationId, { operationId, dishId, quantity }) {
    try {
        const payload = { operationId, dishId, quantity };
        const response = await api.post(
            `/orders/reservations/${reservationId}/dishes/remove`,
            payload
        );
        return { isSuccess: true, data: response.data };
    } catch (error) {
        return {
            isSuccess: false,
            message: mapError(error, "Failed to remove dish from order"),
        };
    }
}

export async function completeOrder(reservationId, operationId) {
    try {
        const payload = { operationId };
        const response = await api.post(
            `/orders/reservations/${reservationId}/complete`,
            payload
        );
        return { isSuccess: true, data: response.data };
    } catch (error) {
        return {
            isSuccess: false,
            message: mapError(error, "Failed to complete order"),
        };
    }
}
