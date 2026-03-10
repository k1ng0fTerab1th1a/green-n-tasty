import { api } from "./api";

function unwrap(response) {
    return response?.data?.data ?? response?.data ?? null;
}

export async function getLocations() {
    const response = await api.get("/locations");
    return unwrap(response);
}

export async function getLocationById(locationId) {
    const locations = await getLocations();
    if (!Array.isArray(locations)) return null;

    return locations.find((item) => String(item.id) === String(locationId)) || null;
}

export async function getLocationSpecialityDishes(locationId) {
    const response = await api.get(`/locations/${locationId}/speciality-dishes`);
    return unwrap(response);
}

export async function getLocationFeedbacks(locationId, type) {
    const response = await api.get(`/locations/${locationId}/feedbacks`, {
        params: { type }
    });

    return response.data?.data?.content ?? [];
}

export async function getLocationSelectOptions() {
    const response = await api.get("/locations/select-options");
    return unwrap(response);
}