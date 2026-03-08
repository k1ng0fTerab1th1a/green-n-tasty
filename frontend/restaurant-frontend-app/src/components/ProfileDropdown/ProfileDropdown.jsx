import styles from "./ProfileDropdown.module.css";
import { useAuth } from "../../auth/AuthContext";

import userOutline from "../../assets/icons/user.svg";
import logoutIcon from "../../assets/icons/logout.svg";

export default function ProfileDropdown({
                                            name = "Johnson Doe",
                                            email = "johnsondoe@nomail.com",
                                            role = "Customer",
                                            onProfile,
                                            onSignOut,
                                        }) {

    const formattedRole =
        role?.charAt(0).toUpperCase() + role?.slice(1).toLowerCase();

    return (
        <div className={styles.card} role="menu" aria-label="Profile menu">
            <div className={styles.head}>
                <div className={styles.name}>
                    {name} ({formattedRole})
                </div>

                <div className={styles.email}>
                    {email}
                </div>
            </div>

            <div className={styles.divider} />

            <button
                type="button"
                className={styles.item}
                onClick={onProfile}
                role="menuitem"
            >
                <img className={styles.itemIcon} src={userOutline} alt="" />
                <span>My Profile</span>
            </button>

            <button
                type="button"
                className={styles.item}
                onClick={onSignOut}
                role="menuitem"
            >
                <img className={styles.itemIcon} src={logoutIcon} alt="" />
                <span>Sign Out</span>
            </button>
        </div>
    );
}