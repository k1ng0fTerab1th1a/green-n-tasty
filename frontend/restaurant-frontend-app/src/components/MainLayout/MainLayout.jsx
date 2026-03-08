import { useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { Header } from "../index.js";
import styles from "./MainLayout.module.css";

export default function MainLayout({ hero = null, children }) {
    const navigate = useNavigate();
    const { auth, signOut } = useAuth();

    return (
        <div className={styles.layout}>
            <Header
                isAuth={auth.isAuth}
                role={auth.role}
                userName={auth.username}
                userEmail={auth.email}
                onSignIn={() => navigate("/login")}
                onProfileClick={() => navigate("/profile")}
                onCartClick={() => navigate("/cart")}
                onBellClick={() => navigate("/notifications")}
                onSignOut={async () => {
                    await signOut();
                }}
            />

            {hero}

            <main className={styles.main}>
                {children}
            </main>
        </div>
    );
}