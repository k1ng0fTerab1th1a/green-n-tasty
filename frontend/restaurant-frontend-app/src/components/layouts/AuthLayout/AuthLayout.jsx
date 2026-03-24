import styles from "./AuthLayout.module.css";

export default function AuthLayout({
                                       kicker,
                                       title,
                                       children,
                                       heroTitle,
                                       heroImage,
                                       heroAlt = "",
                                   }) {
    return (
        <div className={styles.page}>
            <div className={styles.card}>
                {/* LEFT */}
                <div className={styles.left}>
                    {kicker ? <div className={styles.kicker}>{kicker}</div> : null}
                    {title ? <h1 className="h2">{title}</h1> : null}

                    <div className={styles.content}>{children}</div>
                </div>

                {/* RIGHT */}
                <div className={styles.right}>
                    {heroTitle ? <div className={styles.heroTitle}>{heroTitle}</div> : null}

                    {heroImage ? (
                        <img className={styles.heroImage} src={heroImage} alt={heroAlt} />
                    ) : null}
                </div>
            </div>
        </div>
    );
}