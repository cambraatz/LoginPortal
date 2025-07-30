import { SUCCESS_WAIT, FAIL_WAIT } from "../scripts/helperFunctions";
const API_URL = import.meta.env.VITE_API_URL;

export async function Logout() {
    localStorage.clear();
    sessionStorage.clear();

    const response = await fetch(`${API_URL}v1/sessions/logout`, {
        method: "POST",
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        },
        credentials: "include",
    })
    if (!response.ok) {
        console.error("Cookie removal failed, Logout failure.");
        setTimeout(() => {
            //console.log("Logged Out... [dev]");
            //window.location.href = `https://login.tcsservices.com`;
            console.error("Cookie removal failed, Logout failure.");
        },FAIL_WAIT);
    }

    return response;
}