import { MainLayout, Tab, PageBanner } from "../../index.js";
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
            <div className={styles.page}>
                <PageBanner title={title} />

                <div className={styles.container}>
                    <section className={styles.content}>
                        <aside className={styles.sidebar}>
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

                        <main className={styles.main}>
                            <div className={styles.mainInner}>
                                {children}
                            </div>
                        </main>
                    </section>
                </div>
            </div>
        </MainLayout>
    );
}