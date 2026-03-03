import styles from "./MainHero.module.css";
import { Button } from "../index.js";

export default function MainHero({
                                     title = "Green & Tasty",
                                     description = [],
                                     imageSrc,
                                     onViewMenu,
                                 }) {
    const desc = Array.isArray(description) ? description : [description];

    return (
        <section
            className={styles.hero}
            style={{ backgroundImage: `url(${imageSrc})` }}
        >
            <div className={styles.overlay} />

            <div className={styles.content}>
                <div className={styles.title}>{title}</div>

                <div className={styles.text}>
                    {desc.map((t, i) => (
                        <p key={i}>{t}</p>
                    ))}
                </div>

                <Button
                    variant="primary"
                    size="lg"
                    onClick={onViewMenu}
                    className={styles.btn}
                >
                    View Menu
                </Button>
            </div>
        </section>
    );
}