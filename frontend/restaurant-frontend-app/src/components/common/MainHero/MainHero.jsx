import styles from "./MainHero.module.css";
import { Button } from "../../index.js";

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

            <div className={styles.container}>
                <div className={styles.content}>
                    <h1 className={`${styles.title} h1`}>{title}</h1>

                    <div className={styles.text}>
                        {desc.map((t, i) => (
                            <p key={i} className="body">{t}</p>
                        ))}
                    </div>

                    <Button
                        variant="primary"
                        size="xl"
                        onClick={onViewMenu}
                        className={styles.btn}
                    >
                        View Menu
                    </Button>
                </div>
            </div>
        </section>
    );
}