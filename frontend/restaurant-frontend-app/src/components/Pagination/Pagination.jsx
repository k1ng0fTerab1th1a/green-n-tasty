import PageItem from "./PageItem";
import styles from "./Pagination.module.css";

export default function Pagination({ page = 1, totalPages = 1, onChange }) {
    if (totalPages <= 1) return null;

    const pages = Array.from({ length: totalPages }, (_, i) => i + 1);

    return (
        <div className={styles.wrap} aria-label="Pagination">
            {pages.map((p) => (
                <PageItem key={p} active={p === page} onClick={() => onChange?.(p)}>
                    {p}
                </PageItem>
            ))}
        </div>
    );
}