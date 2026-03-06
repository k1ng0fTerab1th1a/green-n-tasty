import { Routes, Route, Navigate } from "react-router-dom";

import Register from "../pages/Register";
import Login from "../pages/Login";
import MainPage from "../pages/MainPage/MainPage.jsx";
import LocationPage from "../pages/Location/index.js";

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<Navigate to="/main" replace />} />
            <Route path="/register" element={<Register />} />
            <Route path="/login" element={<Login />} />
            <Route path="/main" element={<MainPage />} />
            <Route path="/locations/:locationId" element={<LocationPage />} />
        </Routes>
    );
}