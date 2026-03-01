import Input from "./Input";

export default {
    title: "UI/Input",
    component: Input,
};

export const Basic = () => (
    <div style={{ width: 400, padding: 40 }}>
        <Input
            label="Email"
            placeholder="Enter your email"
            hint="e.g. username@domain.com"
        />
    </div>
);