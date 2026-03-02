import { Header } from "../../components/index.js";

export default function MainPage() {
    return (
        <>
            <Header isAuth={true} role={"admin"} />
            <div style={{ padding: "40px" }}>
                <h1>Main Page</h1>
                <p>You are successfully logged in</p>
            </div>
        </>
    );
}