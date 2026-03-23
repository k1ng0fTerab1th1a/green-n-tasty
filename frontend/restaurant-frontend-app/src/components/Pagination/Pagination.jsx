import React, { useMemo } from "react";
import styles from "./Pagination.module.css";
import PageItem from "./PageItem";

import arrowLeftIcon from "../../assets/icons/arrow-left.svg";
import arrowRightIcon from "../../assets/icons/arrow-right.svg";

export default function Pagination({ page, totalPages, onChange }) {
    if (totalPages <= 1) return null;

    const pageNumbers = useMemo(() => {
        const pages = [];
        const maxButtons = 3;
        let startPage, endPage;

        if (totalPages <= maxButtons) {
            startPage = 1;
            endPage = totalPages;
        } else {
            if (page <= 2) {
                startPage = 1;
                endPage = 3;
            } else if (page + 1 >= totalPages) {
                startPage = totalPages - 2;
                endPage = totalPages;
            } else {
                startPage = page - 1;
                endPage = page + 1;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            pages.push(i);
        }
        return pages;
    }, [page, totalPages]);

    return (
        <div className={styles.container}>
            <button
                className={styles.arrowBtn}
                onClick={() => onChange(page - 1)}
                disabled={page === 1}
            >
                <img src={arrowLeftIcon} alt="Previous" className={styles.arrowIcon} />
            </button>

            {pageNumbers.map((p) => (
                <PageItem
                    key={p}
                    active={p === page}
                    onClick={() => onChange(p)}
                >
                    {p}
                </PageItem>
            ))}

            <button
                className={styles.arrowBtn}
                onClick={() => onChange(page + 1)}
                disabled={page === totalPages}
            >
                <img src={arrowRightIcon} alt="Next" className={styles.arrowIcon} />
            </button>
        </div>
    );
}