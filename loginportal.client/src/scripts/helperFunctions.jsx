export const getToken = () => {
    return sessionStorage.getItem('accessToken');
}

export const cacheToken = (access,refresh) => {
    sessionStorage.setItem('accessToken', access);
    sessionStorage.setItem('refreshToken', refresh);
}

export const logout = () => {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('refreshToken');
    localStorage.removeItem('company');
}

export const isTokenExpiring = (token) => {
    const payload = JSON.parse(atob(token.split(".")[1]));
    const expiration = payload.exp * 1000;
    const currentTime = Date.now();
    const threshold = 2 * 60 * 1000;
    //const threshold = 30 * 1000;

    return expiration - currentTime < threshold;
}

export const isTokenValid = (token) => {
    if (!token) { return false; }

    const payload = JSON.parse(atob(token.split(".")[1]));
    if (!payload) { return false; }

    try {
        const currentTime = Date.now();
        const expiration = payload.exp * 1000;

        //console.log(`Current Time: ${currentTime}`);
        //console.log(`Expiration: ${expiration}`);

        return expiration > currentTime;
    } catch (e) {
        console.error("Failed to decode token payload", e);
        return false;
    }
}

export async function refreshToken(username) {
    const refreshToken = sessionStorage.getItem("refreshToken");

    if (!refreshToken) {
        console.error("No refresh token available. Please log in again.");
        return null;
    }

    const body_data = {
        Username: username,
        RefreshToken: refreshToken
    }

    const response = await fetch(API_URL + "api/Registration/RefreshToken",
        {
            body: JSON.stringify(body_data),
            method: "POST",
            headers: { "Content-Type": "application/json" },
        }
    )

    if (response.ok) {
        const data = await response.json();

        sessionStorage.setItem('accessToken', data.AccessToken);
        sessionStorage.setItem('refreshToken', data.RefreshToken);

        return {access: data.AccessToken, refresh: data.RefreshToken};
    } else {
        console.error("Failed to refresh token");
        return null;
    }
}

export async function requestAccess(username) {
    let token = getToken();

    if (!token) {
        console.error("Invalid authorization token");
        throw new Error("Authorization failed. Please log in.");
    }

    if (isTokenExpiring(token)) {
        //console.log("Token expiring soon. Refreshing...");
        const tokens = await refreshToken(username);
        token = tokens.access
    }

    return token;
}

export const cacheCompany = (company) => {
    localStorage.setItem('company', company);
}

export const isCompanyValid = () => {
    const company = localStorage.getItem('company')
    if(company) {
        return company;
    }

    return null;
}

export async function getCompany_DB() {
    const response = await fetch(API_URL + "api/Registration/GetCompany?COMPANYKEY=c01", {
        method: "GET",
    })

    const data = await response.json();
    if (data.success) {
        cacheCompany(data.COMPANYNAME);
        return data.COMPANYNAME;
    } else {
        return null;
    }
}

export const showFailFlag = (id, message) => {
    const flag = document.getElementById(id);
    flag.querySelector("p").innerHTML = message;
    flag.classList.add("visible");
    setTimeout(() => {
        flag.classList.remove("visible");
    },1500)
}

export const scrapeDate = (date) => {
    const year = date.slice(0,4);
    const month = date.slice(5,7);
    const day = date.slice(8);
    return month + day + year;
};

export const renderDate = (date) => {
    const year = date.slice(0,4);
    const month = date.slice(5,7);
    const day = date.slice(8);
    return year + "-" + month + "-" + day;
};

export const renderTime = (time) => {
    const hour = time.slice(0,2);
    const minute = time.slice(2);
    const new_time = hour + ":" + minute;
    return new_time
};

export const translateDate = (date) => {
    const month = date.slice(0,2);
    const day = date.slice(2,4);
    const year = date.slice(4);
    return year + "-" + month + "-" + day
};

export const getDate = () => {
    const now = new Date();
    const year = now.getFullYear();
    var month = now.getMonth() + 1;

    if (month < 10) {month = "0" + month;}
    var day = now.getDate();

    if (day < 10) {day = "0" + day;}

    const currDate = year + "-" + month + "-" + day;
    return currDate;
};

export const getTime = () => {
    const now = new Date();
    var hours = now.getHours();
    if (hours < 10) {
        hours = "0" + hours;
    }
    var minutes = now.getMinutes();
    if (minutes < 10) {
        minutes = "0" + minutes
    }
    const currTime = hours + ":" + minutes;
    return currTime;
};

export const scrapeTime = (time) => {
    const hour = time.slice(0, 2);
    const minute = time.slice(3);
    return hour + minute;
};

export const scrapeFile = (file) => {
    const relLink = file.slice(12);
    return relLink;
};

//export const API_URL = "http://www.tcsservices.com:40730/"
//export const API_URL = "http://www.deliverymanager.tcsservices.com:40730/"
//export const API_URL = "http://localhost:5113/";
export const API_URL = "http://localhost:5171/"