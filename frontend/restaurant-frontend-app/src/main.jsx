import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import { BrowserRouter } from "react-router-dom";

import "./styles/variables.css";
import "./styles/typography.css";
import "./styles/globals.css";

const baseUrl = import.meta.env.VITE_API_BASE_URL || "";
const useMocks = import.meta.env.DEV && baseUrl.includes("localhost:4010");

if (useMocks) {
    const { worker } = await import("./mocks/browser");
    await worker.start({ onUnhandledRequest: "bypass" });
}

ReactDOM.createRoot(document.getElementById("root")).render(
    <BrowserRouter>
        <App />
    </BrowserRouter>
);