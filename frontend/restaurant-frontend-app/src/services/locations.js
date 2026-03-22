import { api } from "./api";

function unwrap(response) {
    return response?.data?.data ?? response?.data ?? null;
}

export async function getLocations() {
    const response = await api.get("/locations");
    return unwrap(response);
}

export async function getLocationById(locationId) {
    const response = await api.get(`/locations/${locationId}`);
    return unwrap(response);
}

export async function getLocationsSelectOptions() {
    const response = await api.get("/locations/select-options");
    return unwrap(response);
}

export async function getLocationSpecialityDishes(locationId) {
    const response = await api.get(`/locations/${locationId}/speciality-dishes`);
    return unwrap(response);
}

export async function getLocationFeedbacks(locationId, type, sort = [], size = 20, pageToken = null) {
    const response = await api.get(`/locations/${locationId}/feedbacks`, {
        params: {
            type,
            sort,
            size,
            pageToken
        },
        paramsSerializer: {
            indexes: null
        }
    });

    const wrappedData = unwrap(response);
    return {
        items: wrappedData?.content ?? [],
        totalElements: wrappedData?.size ?? 0,
        nextPageToken: wrappedData?.nextPageToken ?? null
    };
}