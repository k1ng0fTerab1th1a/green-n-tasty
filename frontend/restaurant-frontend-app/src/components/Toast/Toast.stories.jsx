import Toast from "./Toast";

export default {
    title: "Components/Toast",
    component: Toast,
};

export const Success = () => (
    <Toast
        open={true}
        type="success"
        title="Success"
        message="Operation completed"
    />
);

export const Error = () => (
    <Toast
        open={true}
        type="error"
        title="Error"
        message="Something went wrong"
    />
);