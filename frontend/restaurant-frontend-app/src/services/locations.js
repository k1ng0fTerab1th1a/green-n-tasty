import { api } from "./api";

export async function getLocations() {
    const res = await api.get("/locations");
    return res.data;
}