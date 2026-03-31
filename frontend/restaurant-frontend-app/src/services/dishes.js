import { api } from "./api";

export async function getPopularDishes() {
    const res = await api.get("/dishes/popular");
    return res.data.data;
}

export const getMenuDishes = async (type = "", sort = "price,asc") => {
    try {
        const response = await api.get("/dishes/menu", {
            params: { type, sort }
        });
        return response.data;
    } catch (error) {
        console.error("Error fetching menu:", error);
        throw error;
    }
};

export const getDishById = async (id) => {
    try {
        const response = await api.get(`/dishes/${id}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching dish details:", error);
        throw error;
    }
};

export const getMenuDownloadUrl = async () => {
    try {
        const response = await api.get("/dishes/menu-url");
        return response.data;
    } catch (error) {
        console.error("Error fetching menu URL:", error);
        return { isSuccess: false, message: error.response?.data?.message || error.message };
    }
};

export const downloadMenuFile = async () => {
    try {
        const response = await api.get("/dishes/menu-file", {
            responseType: 'blob',
            headers: {
                'Accept': 'application/pdf'
            }
        });

        const blob = new Blob([response.data], { type: 'application/pdf' });
        const url = window.URL.createObjectURL(blob);

        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'GreenAndTasty_Menu.pdf');

        document.body.appendChild(link);
        link.click();

        link.parentNode.removeChild(link);
        window.URL.revokeObjectURL(url);

        return { isSuccess: true };
    } catch (error) {
        console.error("Error downloading menu file:", error);
        throw error;
    }
};