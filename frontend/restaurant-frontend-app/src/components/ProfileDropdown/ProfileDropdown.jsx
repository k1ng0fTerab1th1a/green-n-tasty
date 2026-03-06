import styles from "./ProfileDropdown.module.css";

import userOutline from "../../assets/icons/user.svg";   // если есть отдельные outline-иконки — лучше их
import logoutIcon from "../../assets/icons/logout.svg";  // если нет — оставь временно или замени

export default function ProfileDropdown({
                                            name = "Johnson Doe (Customer)",
                                            email = "johnsondoe@nomail.com",
                                            onProfile,
                                            onSignOut,
                                        }) {
    return (
        <div className={styles.card} role="menu" aria-label="Profile menu">
            <div className={styles.head}>
                <div className={styles.name}>{name}</div>
                <div className={styles.email}>{email}</div>
            </div>

            <div className={styles.divider} />

            <button type="button" className={styles.item} onClick={onProfile} role="menuitem">
                <img className={styles.itemIcon} src={userOutline} alt="" />
                <span>My Profile</span>
            </button>

            <button type="button" className={styles.item} onClick={onSignOut} role="menuitem">
                <img className={styles.itemIcon} src={logoutIcon} alt="" />
                <span>Sign Out</span>
            </button>
        </div>
    );
}