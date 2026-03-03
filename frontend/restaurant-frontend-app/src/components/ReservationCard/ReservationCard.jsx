import styles from "./ReservationCard.module.css";
import { Button, Dropdown } from "../index.js";

/**
 * ReservationCard
 *
 * Props:
 * - role: "customer" | "waiter"
 *
 * Common:
 * - location: string
 * - date: string
 * - timeRange: string   (e.g. "10:30 a.m. – 11:30 a.m.")
 * - guests: number | string
 * - status: "reserved" | "in_progress" | "finished" | "canceled"
 *
 * Pre-order:
 * - preOrderCount?: number (if > 0 shows "Pre-order: X dishes")
 * - onPreOrder?: () => void (button)
 *
 * Customer actions:
 * - onCancel?: () => void
 * - onEdit?: () => void
 * - onFeedback?: () => void   (Leave feedback / View-Update feedback)
 *
 * Waiter fields:
 * - customerName?: string
 * - tableValue?: string|number|null
 * - tableOptions?: Array<{value,label}>
 * - onTableChange?: (val) => void
 */
export default function ReservationCard({
                                            role = "customer",

                                            location = "Location 1",
                                            date = "",
                                            timeRange = "",
                                            guests = 0,
                                            status = "reserved",

                                            preOrderCount = 0,
                                            onPreOrder,

                                            onCancel,
                                            onEdit,
                                            onFeedback,

                                            customerName = "",
                                            tableValue = null,
                                            tableOptions = [],
                                            onTableChange,
                                        }) {
    const hasPreOrder = Number(preOrderCount) > 0;

    const badge = getStatusBadge(status);

    // Customer: which main action button?
    const customerAction = (() => {
        if (status === "in_progress") return { label: "Leave Feedback", onClick: onFeedback, variant: "primary" };
        if (status === "finished") return { label: "View / Update feedback", onClick: onFeedback, variant: "primary" };
        return null;
    })();

    const showBottomRow = role === "customer"
        ? status === "reserved" // cancel/edit + maybe preorder
        : status !== "canceled"; // waiter: cancel/edit always except canceled

    return (
        <div className={styles.card}>
            <div className={styles.topRow}>
                <div className={styles.left}>
                    <InfoRow icon="pin" text={location} />
                    <InfoRow icon="calendar" text={date} />
                    <InfoRow icon="clock" text={timeRange} />

                    {role === "waiter" ? (
                        <>
                            <InfoRow icon="user" text={`Customer ${customerName}`} />
                            <InfoRow icon="users" text={`${guests} Guests`} />
                            {hasPreOrder ? <InfoRow icon="preorder" text={`Pre-order: ${preOrderCount} dishes`} /> : null}
                        </>
                    ) : (
                        <>
                            {hasPreOrder ? <InfoRow icon="preorder" text={`Pre-order: ${preOrderCount} dishes`} /> : null}
                            <InfoRow icon="users" text={`${guests} Guests`} />
                        </>
                    )}
                </div>

                <div className={styles.rightTop}>
                    {/* badge */}
                    <div className={`${styles.badge} ${styles[`badge_${badge.key}`]}`}>{badge.label}</div>

                    {/* waiter table dropdown */}
                    {role === "waiter" ? (
                        <div className={styles.tableBox}>
                            <Dropdown
                                value={tableValue}
                                onChange={onTableChange}
                                options={tableOptions}
                                placeholder="Table 1"
                            />
                        </div>
                    ) : null}
                </div>
            </div>

            {/* Customer main single button in progress/finished */}
            {role === "customer" && customerAction ? (
                <div className={styles.singleAction}>
                    <Button variant={customerAction.variant} size="lg" onClick={customerAction.onClick} className={styles.fullBtn}>
                        {customerAction.label}
                    </Button>
                </div>
            ) : null}

            {/* Bottom actions */}
            {showBottomRow ? (
                <div className={styles.bottomRow}>
                    <button type="button" className={styles.cancelLink} onClick={onCancel}>
                        Cancel
                    </button>

                    <div className={styles.actions}>
                        <Button variant="secondary" size="sm" onClick={onEdit}>
                            Edit
                        </Button>

                        {role === "customer" && status === "reserved" && hasPreOrder ? (
                            <Button variant="primary" size="sm" onClick={onPreOrder}>
                                Pre-order
                            </Button>
                        ) : null}
                    </div>
                </div>
            ) : null}
        </div>
    );
}

/* ===== helpers ===== */

function getStatusBadge(status) {
    switch (status) {
        case "reserved":
            return { key: "reserved", label: "Reserved" };
        case "in_progress":
            return { key: "progress", label: "In Progress" };
        case "finished":
            return { key: "finished", label: "Finished" };
        case "canceled":
        default:
            return { key: "canceled", label: "Canceled" };
    }
}

/**
 * InfoRow: поки робимо як текст + зелена "іконка-точка" (в тебе будуть svg — легко заміниш)
 * Якщо хочеш — я одразу зроблю з import svg.
 */
function InfoRow({ icon, text }) {
    return (
        <div className={styles.infoRow}>
            <span className={styles.icon} data-icon={icon} />
            <span className={styles.infoText}>{text}</span>
        </div>
    );
}