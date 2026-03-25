import { Routes, Route, Navigate } from "react-router-dom";

import { RegisterPage, LoginPage, MainPage, LocationPage, ProfilePage, SearchPage, ReservationsPage, MenuPage } from "../pages/index.js";

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<Navigate to="/main" replace />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/main" element={<MainPage />} />
            <Route path="/locations/:locationId" element={<LocationPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/search" element={<SearchPage />} />
            <Route path="/reservations" element={<ReservationsPage />} />
            <Route path="/menu" element={<MenuPage />} />
        </Routes>
    );
}