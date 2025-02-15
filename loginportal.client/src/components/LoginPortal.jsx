/*/////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2023
Update Date: 1/7/2025

*//////////////////////////////////////////////////////////////////////

import { useState, useEffect } from 'react';
import { useNavigate } from "react-router-dom";
import Header from './Header';
import Popup from './Popup';
import Footer from './Footer';
import { scrapeDate, 
    renderDate, 
    getDate, 
    API_URL, 
    cacheToken,
    requestAccess,
    isCompanyValid,
    getCompany_DB, 
    showFailFlag,
    COMPANIES} from '../scripts/helperFunctions';

/*/////////////////////////////////////////////////////////////////////

DriveryLogin() - Driver/Delivery Authentication

DriverLogin serves as the home page for the driver application...
Function handles log in credential and delivery validation...
DriverLogin Component takes no parameters and handles the user login
and validation phase.

After preliminary react functionality/state variables are initialized, the component
handles user credentials, validating the provided username and password against what
is currently on file for the driver. Error prompt styling is rendered and dynamically
managed as user input changes. Once credentials are validated a popup window opens 
that prompts the user to provide a date and powerunit to query deliveries.

The driver must be a valid user on file, but are allowed to query delivery data for
any valid powerunit and delivery date pair. The set of functions below manage and
package data for interaction with the .NET backend and MSSQL database.

///////////////////////////////////////////////////////////////////////

BASIC STRUCTURE:
// initialize rendered page...
    initialize date, navigation and states
    useEffect() => 
        check delivery validity onLoad and after message state change
    renderCompany() => 
        retrieve company name from database when not in memory

// page rendering helper functions...
    openPopup() => 
        open popup for delivery confirmation
    closePopup() => 
        close popup for delivery confirmation
    collapseHeader() => 
        open/close collapsible header

// state management functions...
    handleLoginChange() => 
        handle login form changes
    handleDeliveryChange() => 
        handle delivery query form changes

// API requests + functions...
    handleSubmit() => 
        handleClick on initial login button
    validateCredentials() => 
        prompt for correction in fail or open popup in success
    handleUpdate() => 
        validate delivery data + powerunit, navigate to /driverlog on success

    handleNewUser() => 
        open new user initialization menu
    updateDriver() =>
        collect password + powerunit to initialize new driver credentials
    pullDriver() =>
        fetch driver and ensure null password for new driver init
    cancelDriver() =>
        reset driver credentials on popup close
    updateNewUser() =>
        handle updates to new user credentials

// render template + helpers...
    package popup helper functions
    return render template

*//////////////////////////////////////////////////////////////////////

const LoginPortal = () => {
    // Date processing functions ...
    const currDate = getDate();
    const navigate = useNavigate();

    // check delivery validity onLoad and after message state change...
    useEffect(() => {
        const company = isCompanyValid();
        if (!company) {
            renderCompany();
        } else {
            setCompany(company);
        }
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    /* Site state & location processing functions... */

    // initialize company state to null, replace with company on file...
    const [company, setCompany] = useState("");

    // set popup render status...
    const [message, setMessage] = useState(null);

    // state 'driverCredentials' to be passed to next page...
    const [credentials, setCredentials] = useState({
        USERNAME: '',
        PASSWORD: ''
    })

    // state 'header' to maintain collapsible header...
    const [header,setHeader] = useState("open");

    /* Page rendering helper functions... */

    /*/////////////////////////////////////////////////////////////////
    // retrieve company from database when not in memory...
    [void] : renderCompany() {
        fetch company name from database (if present)
        if (company is valid):
            setCompany(company)
        else:
            setCompany to placeholder
    } 
    *//////////////////////////////////////////////////////////////////
    
    async function renderCompany() {
        // getCompany() also caches company...
        //const company = await getCompany_DB();
        const company = "Transportation Computer Support, LLC.";
        // set company state to value or placeholder... 
        if(company) {
            console.log(`renderCompany retrieved ${company} from database...`);
            setCompany(company);
        } else {
            console.log(`renderCompany could not find company...`);
            setCompany("No Company Set");
        }
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : openPopup() {
        make popup window visible on screen
        enable on click behavior
    }
    *//////////////////////////////////////////////////////////////////

    const openPopup = () => {
        document.getElementById("popupLoginWindow").style.visibility = "visible";
        document.getElementById("popupLoginWindow").style.opacity = 1;
        document.getElementById("popupLoginWindow").style.pointerEvents = "auto";  
    };

    /*/////////////////////////////////////////////////////////////////
    [void] : closePopup() {
        self explanatory closing of "popupLoginWindow"
        setStatus("") and setMessage(null) - reset state data
    }
    *//////////////////////////////////////////////////////////////////

    const closePopup = () => {
        document.getElementById("popupLoginWindow").style.visibility = "hidden";
        document.getElementById("popupLoginWindow").style.opacity = 0;
        document.getElementById("popupLoginWindow").style.pointerEvents = "none";
        
        // reset driver credentials to default...
        setCredentials({
            USERNAME: "",
            PASSWORD: ""
        });
    };

    /*/////////////////////////////////////////////////////////////////
    // initialize and manage collapsible header behavior...
    initialize header toggle to "open" - default for login screen
    [void] : collapseHeader(event) {
        if (e.target.id === "collapseToggle" or "toggle_dots"):
            open/close header - do opposite of current "header" state
    }
    *//////////////////////////////////////////////////////////////////

    const collapseHeader = (e) => {
        // toggle header only if toggle or dots symbol are clicked...
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            if (header === "open") {
                setHeader("close");
            } else {
                setHeader("open");
            }
        }
    }

    /* Dynamic form/state change functions... */

    /*/////////////////////////////////////////////////////////////////
    // handle login form changes...
    [void] : handleLoginChange(event) {
        if (invalid log in is changed):
            remove USER/PW error styling on change

        if (e.target.id === "USERNAME"):
            update driverCredentials with new username
        if (e.target.id === "PASSWORD"):
            update driverCredentials with new password
    } 
    *//////////////////////////////////////////////////////////////////

    const handleLoginChange = (e) => {
        // reset styling to default...
        if(document.getElementById(e.target.id).classList.contains("invalid_input")){
            document.getElementById("USERNAME").classList.remove("invalid_input");
            document.getElementById("PASSWORD").classList.remove("invalid_input");
        }

        // handle username + password field changes...
        let val = e.target.value;
        switch(e.target.id) {
            case 'USERNAME':
                setCredentials({
                    ...credentials,
                    USERNAME: val
                });
                break;
            case 'PASSWORD':
                setCredentials({
                    ...credentials,
                    PASSWORD: val
                });
                break;
            default:
                break;
        }
    };
    
    /* API Calls and Functionality... */

    /*/////////////////////////////////////////////////////////////////
    // handleClick on initial Login button...
    [void] : handleSubmit(event) {
        prevent default submit behavior
        define input field alert status

        if (user is invalid):
            set user field to invalid_input styling
        if (password is invalid):
            set password field to invalid
        if alert status is greater than 1:
            set and render error flag/popup
            return (dont submit)
        
        validateCredentials()
    }
    *//////////////////////////////////////////////////////////////////

    const [user, setUser] = useState({
        Username: null,
        Permissions: null,
        Powerunit: null,
        ActiveCompany: null,
        Companies: null,
        Modules: null
    });

    async function handleSubmit(e) {
        // prevent default and reset popup window...
        e.preventDefault();
        setMessage(null);

        // target username and password fields...
        const user_field = document.getElementById("USERNAME");
        const pass_field = document.getElementById("PASSWORD");
        
        // map empty field cases to messages...
        let code = -1; // case -1...
        let elementID;
        const alerts = {
            0: "Username is required!", // case 0...
            1: "Password is required!", // case 1...
            2: "Both fields are required!" // case 2...
        }
        // flag empty username...
        if (user_field.value === "" || user_field.value == null){
            user_field.classList.add("invalid_input");
            code += 1;
            elementID = "ff_login_un"
        } 
        // flag empty powerunit...
        if (pass_field.value === "" || pass_field.value == null){
            pass_field.classList.add("invalid_input");
            code += 2;
            elementID = "ff_login_pw"
        }

        // catch and alert user to incomplete fields...
        if (code >= 0) {
            // initialize flag contents...
            const flag = document.getElementById(elementID);
            flag.querySelector("p").innerHTML = alerts[code];

            // make visible for 1.5 seconds and hide again...
            flag.classList.add("visible");
            setTimeout(() => {
                flag.classList.remove("visible");
            },1500)
            //showFailFlag(elementID,alerts[code]);
            return;
        }
        //validateCredentials(driverCredentials.USERNAME,driverCredentials.PASSWORD);

        const response = await fetch(API_URL + "api/Registration/Login", {
            body: JSON.stringify(credentials),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })
        const data = await response.json();
        console.log(data);

        if (data.success) {
            localStorage.setItem('user',data.user);

            cacheToken(data.accessToken,data.refreshToken);
            //await getCompanies();
            console.log("user companies:");
            console.log(data.user.Companies);
            console.log(data.user.Modules);
            console.log(data.user);
            
            setUser(data.user);
            setCompanies(data.user.Companies);
            setModules(data.user.Modules);
            
            setPopupMessage("Select Company");
            setPopup("company");
            openPopup();
        } else {
            setCompanies([]);
            setModules([]);
            setPopup(null);

            // trigger red borders for errors...
            document.getElementById("USERNAME").classList.add("invalid_input");
            document.getElementById("PASSWORD").classList.add("invalid_input");
        }
        
    };

    /*/////////////////////////////////////////////////////////////////
    // open new user initialization menu...
    [void] : handleNewUser() {
        reset input field styling
        set user credentials to default
        set popup to new user prompt and open
    }
    *//////////////////////////////////////////////////////////////////

    async function handleNewUser() {
        // reset input field error styling if triggered...
        if(document.getElementById("USERNAME").classList.contains("invalid_input")) {
            document.getElementById("USERNAME").classList.remove("invalid_input");
        }
        if(document.getElementById("PASSWORD").classList.contains("invalid_input")) {
            document.getElementById("PASSWORD").classList.remove("invalid_input");
        }

        // nullify driver credentials...
        const user_data = {
            USERNAME: "",
            PASSWORD: ""
        }
        setCredentials(user_data);

        // set to new user popup and open...
        setMessage("New User Signin");
        openPopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // reset driver credentials on popup close...
    [void] : cancelDriver() {
        reset driver credentials to null
        close pop up
    }
    *//////////////////////////////////////////////////////////////////

    async function cancelDriver() {
        // nullify credentials and close popup...
        setCredentials({
            USERNAME: "",
            PASSWORD: ""
        })
        closePopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // reset driver credentials on popup close...
    [void] : submitNewUser(e) {
        if (target == "edit new user"):
            initialize new driver credentials (updateDriver)
        handle popup button (login functions)
            set_password: fetch username + powerunit for new user
            submit_user: update user table with newly set password
            cancel_user: exit/close popup
    }
    *//////////////////////////////////////////////////////////////////

    async function submitNewUser(e) {
        e.preventDefault();

        // handle new user menu button clicks...
        switch(e.target.parentElement.id){
            case "set_password":
                //pullDriver();
                break;
            case "submit_user":
                //updateDriver();
                break;
            case "cancel_user":
                closePopup();
                break;
            default:
                break;
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // handle updates to new user credentials...
    [void] : updateNewUser(e) {
        clear error styling (if present)
        handle change for new user credentials
    }
    *//////////////////////////////////////////////////////////////////

    async function updateNewUser(e) {
        //if (document.getElementById(e.target.id).className == "invalid_input"){
        if (document.getElementById(e.target.id).classList.contains("invalid_input")){
            // reset styling to default...
            document.getElementById("username").classList.remove("invalid_input");

            // skip password + powerunit when username specific...
            document.getElementById("password").classList.remove("invalid_input");
        }

        // update credentials state on change...
        let val = e.target.value;
        switch(e.target.id){
            case 'username':
                setCredentials({
                    ...credentials,
                    USERNAME: val
                });
                break;
            case 'password':
                setCredentials({
                    ...credentials,
                    PASSWORD: val
                });
                break;
            default:
                break;
        }
    }

    // new edits below...
    const [companies, setCompanies] = useState([]);
    const [modules, setModules] = useState([]);

    async function getCompanies() {
        // request token from memory, refresh as needed...
        const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            navigate('/');
            return;
        }

        const response = await fetch(API_URL + "api/Registration/GetCompanies", {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
            }
        })
    
        const data = await response.json();
        console.log(data);

        setCompanies(data.companies);
        console.log(data.companies)
        return data.companies
    }

    async function pressButton(e) {
        localStorage.removeItem('DB');
        localStorage.removeItem('company');

        let company = null;
        switch(e.target.id){
            case "cp1":
                console.log(`Clicked ${companies[0]}`);
                company = companies[0];
                break;
            case "cp2":
                console.log(`Clicked ${companies[1]}`);
                company = companies[1];
                break;
            case "cp3":
                console.log(`Clicked ${companies[2]}`);
                company = companies[2];
                break;
            case "cp4":
                console.log(`Clicked ${companies[3]}`);
                company = companies[3];
                break;
            case "cp5":
                console.log(`Clicked ${companies[4]}`);
                company = companies[4];
                break;
            default:
                break;
        }

        setUser({...user, ActiveCompany: COMPANIES[company]});
        console.log(`Company clicked: ${COMPANIES[company]}`);

        setPopupMessage("Select Module");
        setPopup("module");
        console.log(modules);

        /*// request token from memory, refresh as needed...
        const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            navigate('/');
            return;
        }

        const response = await fetch(API_URL + "api/Registration/SelectCompany", {
            method: "POST",
            body: JSON.stringify({
                Company: company.COMPANYKEY,
                AccessToken: token
            }),
            headers: {
                "Authorization": `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        })

        if (response.ok) {
            const data = await response.json();
            console.log(data);

            localStorage.setItem('accessToken', data.AccessToken);
            localStorage.setItem('refreshToken', data.RefreshToken);

            window.location.href = `http://www.deliverymanager.tcsservices.com:40730?access_token=${data.AccessToken}/`;
        } else {
            console.error("Failed to refresh token");
            return null;
        }*/

        //closePopup();
    }

    const [popup, setPopup] = useState("company");
    const [popupMessage, setPopupMessage] = useState(null);

    const renderPopup = (type) => {
        if (type == "company") {
            //setPopupMessage("Select Company");
            if (companies.length > 0) {
                return companies.map((company,i) => {
                    return (
                        <button id={"cp"+(i+1)} key={company+1} type="button" onClick={pressButton}>
                            {COMPANIES[company]}
                        </button>
                    );
                });
            } else {
                //setPopupMessage("No Available Companies, Contact Admin.");
                return(
                    <div className="error_placeholder">No Companies Available</div>
                );
            }
            
        } else if (type == "module") {
            //setPopupMessage("Select Module");
            if (modules.length > 0) {
                return modules.map((module,i) => {
                    return (
                        <button id={"md"+(i+1)} key={module+1} type="button" onClick={pressButton}>
                            {module}
                        </button>
                    );
                });
            } else {
                //setPopupMessage("No Available Modules, Contact Admin.");
                return(
                    <div className="error_placeholder">No Modules Available</div>
                );
            }
        }
    }

    // render template...
    return(
        <div id="webpage">
            <Header 
                company={company}
                title="Login Portal"
                alt="Enter your login credentials"
                status="Off"
                currUser="Sign In"
                MFSTDATE={null}
                POWERUNIT={null}
                STOP = {null}
                PRONUMBER = {null}
                MFSTKEY = {null}
                toggle={header}
                onClick={collapseHeader}
            />
            <div id="Delivery_Login_Div">
                <form id="loginForm" onSubmit={handleSubmit}>
                    <div className="input_wrapper">
                        <label htmlFor="USERNAME">Username:</label>
                        <input type="text" id="USERNAME" value={credentials.USERNAME} onChange={handleLoginChange}/>
                        <div className="fail_flag" id="ff_login_un">
                            <p>Username is required!</p>
                        </div>
                    </div>
                    <div className="input_wrapper">
                        <label htmlFor="PASSWORD">Password:</label>
                        <input type="password" id="PASSWORD" value={credentials.PASSWORD} onChange={handleLoginChange}/>
                        <div className="fail_flag" id="ff_login_pw">
                            <p>Password is required!</p>
                        </div>
                    </div>
                    <h4 id="new_user" onClick={handleNewUser}>New User Sign-in</h4>
                    <button type="submit">Login</button>
                </form>
            </div>

            {/* Popup Logic Below... */}
            <div id="popupLoginWindow" className="overlay">
                <div className="popupLogin">
                    <div id="popupLoginExit" className="content">
                        <h1 id="close" className="popupLoginWindow" onClick={closePopup}>&times;</h1>
                    </div>
                    <div id="popupLoginPrompt" className="content">
                        <p>{popupMessage}</p>
                    </div>

                    <div id="login_div">
                        {renderPopup(popup)}
                        {/*companies.map((company,i) => {
                            if (company){
                                return (
                                    <button id={"cp"+(i+1)} key={COMPANIES[company]} type="button" onClick={pressButton}>
                                        {COMPANIES[company]}
                                    </button>
                                );
                            }
                            return null;
                        })*/}
                    </div>
                </div>
            </div>

            <Footer id="footer" />
        </div>
    )
};

export default LoginPortal;