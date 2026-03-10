import { Routes, Route, Navigate } from "react-router-dom";

import Register from "../pages/Register";
import Login from "../pages/Login";
import MainPage from "../pages/MainPage/index.js";
import LocationPage from "../pages/Location/index.js";
import ProfilePage from "../pages/ProfilePage/index.js";
import SearchPage from "../pages/SearchPage/index.js";
import ReservationsPage from "../pages/ReservationsPage/ReservationsPage.jsx";

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<Navigate to="/main" replace />} />
            <Route path="/register" element={<Register />} />
            <Route path="/login" element={<Login />} />
            <Route path="/main" element={<MainPage />} />
            <Route path="/locations/:locationId" element={<LocationPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/search" element={<SearchPage />} />
            <Route path="/reservations" element={<ReservationsPage />} />
        </Routes>
    );
}