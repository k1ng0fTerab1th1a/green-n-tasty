import { MainLayout, Tab } from "../../components/index.js";
import styles from "./ProfileLayout.module.css";

export default function ProfileLayout({
                                          title = "My Profile",
                                          sections = [],
                                          activeSection,
                                          onSectionChange,
                                          children,
                                      }) {
    return (
        <MainLayout>
            <section className={styles.hero}>
                <h1 className={styles.pageTitle}>{title}</h1>
            </section>

            <section className={styles.content}>
                <aside className={styles.sidebar} aria-label="Profile sections">
                    <div className={styles.tabs}>
                        {sections.map((section) => (
                            <Tab
                                key={section.value}
                                active={activeSection === section.value}
                                onClick={() => onSectionChange(section.value)}
                                className={styles.tab}
                            >
                                {section.label}
                            </Tab>
                        ))}
                    </div>
                </aside>

                <div className={styles.main}>
                    <div className={styles.mainInner}>
                        {children}
                    </div>
                </div>
            </section>
        </MainLayout>
    );
}