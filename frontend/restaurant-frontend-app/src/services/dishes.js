import { api } from "./api";

export async function getPopularDishes() {
    const res = await api.get("/dishes/popular");
    return res.data;
}