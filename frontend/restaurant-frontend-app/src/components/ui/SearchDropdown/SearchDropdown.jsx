import { useState, useEffect, useRef } from "react";
import { Input } from "../../index.js";
import styles from "./SearchDropdown.module.css";

export default function SearchDropdown({
                                           items = [],
                                           searchKey = "name",
                                           placeholder = "Search...",
                                           label = "Search",
                                           hint = "",
                                           onSelect,
                                           renderItem
                                       }) {
    const [query, setQuery] = useState("");
    const [filteredItems, setFilteredItems] = useState([]);
    const [isOpen, setIsOpen] = useState(false);
    const wrapperRef = useRef(null);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (wrapperRef.current && !wrapperRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    useEffect(() => {
        if (query.trim().length > 0) {
            const filtered = items.filter(item =>
                item[searchKey]?.toLowerCase().includes(query.toLowerCase())
            );
            setFilteredItems(filtered);
            setIsOpen(true);
        } else {
            setIsOpen(false);
        }
    }, [query, items, searchKey]);

    const handleItemClick = (item) => {
        onSelect(item);
        setQuery("");
        setIsOpen(false);
    };

    return (
        <div className={styles.searchWrapper} ref={wrapperRef}>
            <div className={isOpen && filteredItems.length > 0 ? styles.inputActive : ""}>
                <Input
                    label={label}
                    placeholder={placeholder}
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    hint={hint}
                    onFocus={() => query.length > 0 && setIsOpen(true)}
                />
            </div>

            {isOpen && filteredItems.length > 0 && (
                <div className={styles.dropdown}>
                    {filteredItems.map((item, index) => (
                        <div
                            key={item.id || index}
                            className={`body-bold ${styles.dropdownItem} ${index === 0 ? styles.firstItem : ""}`}
                            onClick={() => handleItemClick(item)}
                        >
                            {renderItem ? renderItem(item) : item[searchKey]}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}