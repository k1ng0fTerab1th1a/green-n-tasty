import { NavigationLink } from "../../index.js";
import styles from "./Breadcrumbs.module.css";
import arrowIcon from "../../../assets/icons/chevron-right.svg";

export default function Breadcrumbs({ items = [] }) {
    return (
        <div className={styles.breadcrumbs}>
            {items.map((item, index) => (
                <div key={index} className={styles.item}>
                    {item.to ? (
                        <NavigationLink to={item.to} className={`${styles.crumb} body`}>
                            {item.label}
                        </NavigationLink>
                    ) : (
                        <span className={`${styles.crumbActive} body-bold`}>{item.label}</span>
                    )}

                    {index < items.length - 1 && (
                        <img src={arrowIcon} alt="" className={styles.sepIcon} />
                    )}
                </div>
            ))}
        </div>
    );
}