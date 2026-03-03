export const passwordRules = [
    { key: "upper", label: "At least one uppercase letter required", test: (v) => /[A-Z]/.test(v) },
    { key: "lower", label: "At least one lowercase letter required", test: (v) => /[a-z]/.test(v) },
    { key: "number", label: "At least one number required", test: (v) => /\d/.test(v) },
    { key: "special", label: "At least one special character required", test: (v) => /[^A-Za-z0-9]/.test(v) },
    { key: "length", label: "Password must be 8–16 characters long", test: (v) => v.length >= 8 && v.length <= 16 }
];

export function checkPasswordAllRules(value, rules = passwordRules) {
    const v = String(value || "");
    const checks = {};
    for (const r of rules) checks[r.key] = r.test(v);

    const isValid = rules.every((r) => checks[r.key] === true);
    return { isValid, checks };
}