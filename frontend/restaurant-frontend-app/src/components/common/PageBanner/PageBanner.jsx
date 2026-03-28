import styles from "./PageBanner.module.css";
import bannerLogo from "../../../assets/icons/banner-logo.svg";

export default function PageBanner({ title }) {
    return (
        <section className={styles.hero}>
            <div className={styles.heroInner}>
                <h1 className={styles.pageTitle}>{title}</h1>

                <div className={styles.bannerIcon}>
                    <img src={bannerLogo} alt="Logo" className={styles.logoImg} />
                </div>
            </div>
        </section>
    );
}