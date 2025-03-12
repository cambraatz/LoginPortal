/*/////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2023
Update Date: 1/7/2025

*//////////////////////////////////////////////////////////////////////

import { useState, useEffect } from 'react';
import Header from './Header';
import Footer from './Footer';
import { 
    API_URL, 
    COMPANIES,
    MODULES,
    resetStyling,
    flagError,
    errorStyling} from '../scripts/helperFunctions';

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
    // check delivery validity onLoad and after message state change...
    useEffect(() => {
        cleanSlate();
    }, [])

    /* Site state & location processing functions... */

    // state 'driverCredentials' to be passed to next page...
    const [credentials, setCredentials] = useState({
        USERNAME: '',
        PASSWORD: ''
    })

    // state 'header' to maintain collapsible header...
    const [header,setHeader] = useState("open");

    /* Page rendering helper functions... */
    async function cleanSlate() {
        localStorage.clear();
        sessionStorage.clear();

        const response = await fetch(API_URL + "api/Registration/Logout", {
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
        })
        if (!response.ok) {
            //console.log("Cookies have been cleared successfully.");
            console.alert("Cookie removal failed, Logout failure.");
        }
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : openPopup() {
        make popup window visible on screen
        enable on click behavior
    }
    *//////////////////////////////////////////////////////////////////

    const openPopup = () => {
        document.getElementById("popupWindow").style.visibility = "visible";
        document.getElementById("popupWindow").style.opacity = 1;
        document.getElementById("popupWindow").style.pointerEvents = "auto";  
    };

    /*/////////////////////////////////////////////////////////////////
    [void] : closePopup() {
        self explanatory closing of "popupLoginWindow"
        setStatus("") and setMessage(null) - reset state data
    }
    *//////////////////////////////////////////////////////////////////

    const closePopup = () => {
        document.getElementById("popupWindow").style.visibility = "hidden";
        document.getElementById("popupWindow").style.opacity = 0;
        document.getElementById("popupWindow").style.pointerEvents = "none";
        
        // reset driver credentials to default...
        setCredentials({
            USERNAME: "",
            PASSWORD: ""
        });

        setUser(default_user);

        cleanSlate();
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
            resetStyling(["USERNAME","PASSWORD"]);
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

    const default_user = {
        Username: "Sign In",
        Permissions: null,
        Powerunit: null,
        ActiveCompany: null,
        Companies: null,
        Modules: null
    };
    const [user, setUser] = useState(default_user);

    async function handleSubmit(e) {
        // prevent default and reset popup window...
        e.preventDefault();

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
            flagError(elementID,alerts[code])
            return;
        }

        const response = await fetch(API_URL + "api/Registration/Login", {
            body: JSON.stringify(credentials),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            // update state values to curr user...      
            setUser(data.user);
            setCompanies(data.user.Companies);
            setModules(data.user.Modules);
            
            // initialize company/module selection popup...
            setPopupMessage("Select Company");
            setPopup("company");
            openPopup();
        } else {
            // reset state values to null...
            setCompanies([]);
            setModules([]);
            setPopup(null);
            //setUser(default_user);

            // render error flag + set error styling...
            flagError("ff_login_pw", "Invalid user credentials");
            errorStyling(["USERNAME","PASSWORD"]);
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
        setPopupMessage("New User Signin");
        setPopup("new_user");
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

    async function pressButton(e) {
        let company = null;
        switch(e.target.id){
            case "cp1":
                company = companies[0];
                break;
            case "cp2":
                company = companies[1];
                break;
            case "cp3":
                company = companies[2];
                break;
            case "cp4":
                company = companies[3];
                break;
            case "cp5":
                company = companies[4];
                break;
            default:
                console.log(`Clicked ${company}`);
                break;
        }

        if (company) {
            setUser({...user, ActiveCompany: COMPANIES[company]});
            const response = await fetch(API_URL + "api/Registration/SetCompany", {
                body: JSON.stringify({ 
                    username: credentials.USERNAME, 
                    company: Object.keys(COMPANIES).find(key => COMPANIES[key] === COMPANIES[company]) 
                }),
                method: "POST",
                headers: {
                    'Content-Type': 'application/json; charset=UTF-8'
                }
            });

            if (!response.ok) {
                console.alert("Company selection failed");
            }
            //const data = await response.json();
            //console.log("data: ", data);

            setPopupMessage("Select Module");
            setPopup("module");
            //console.log(modules);
            return;
        }

        let mod = null;
        switch(e.target.id){
            case "md1":
                console.log(`Clicked ${modules[0]}`);
                mod = modules[0];
                break;
            case "md2":
                console.log(`Clicked ${modules[1]}`);
                mod = modules[1];
                break;
            case "md3":
                console.log(`Clicked ${modules[2]}`);
                mod = modules[2];
                break;
            case "md4":
                console.log(`Clicked ${modules[3]}`);
                mod = modules[3];
                break;
            case "md5":
                console.log(`Clicked ${modules[4]}`);
                mod = modules[4];
                break;
            default:
                break;
        }

        if (mod) {
            if (mod === "DLVYCHKOFF") {
                window.location.href = `https://www.deliverymanager.tcsservices.com/`;
            } else if (mod === "ADMIN") {
                window.location.href = `https://www.admin.tcsservices.com/`;
            } else {
                closePopup();
            }
            return;
        }

        closePopup();
    }

    const [popup, setPopup] = useState("company");
    const [popupMessage, setPopupMessage] = useState(null);

    const renderPopup = (type) => {
        if (type == "company") {
            if (companies.length > 0) {
                return companies.map((company,i) => {
                    if (company === "") { return null; }
                    return (
                        <button id={"cp"+(i+1)} key={company+1} type="button" onClick={pressButton}>
                            {COMPANIES[company]}
                        </button>
                    );
                });
            } else {
                return(
                    <div className="error_placeholder">No Companies Available</div>
                );
            }
            
        } else if (type == "module") {
            if (modules.length > 0) {
                return modules.map((module,i) => {
                    if (module === "") { return null; }
                    return (
                        <button id={"md"+(i+1)} key={module+1} type="button" onClick={pressButton}>
                            {MODULES[module]}
                        </button>
                    );
                });
            } else {
                return(
                    <div className="error_placeholder">No Modules Available</div>
                );
            }
        } else if (type == "new_user") {
            return (
                <>
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input type="text" id="username" value={credentials.USERNAME ? credentials.USERNAME : ""} className="input_form" onChange={updateNewUser}/>
                        <div className="fail_flag" id="ff_login_nu">
                            <p>Username was not found!</p>
                        </div>
                    </div>
                    <div id="set_password">
                        <button id="set_password" className="popup_button" onClick={submitNewUser}>Authorize</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={cancelDriver}>Cancel</button>
                    </div>
                </>
            );
        }
    }

    // render template...
    return(
        <div id="webpage">
            <Header 
                company="Transportation Computer Support, LLC."
                title="Login Portal"
                alt="Enter your login credentials"
                status="Off"
                currUser={user.Username}
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
            <div id="popupWindow" className="overlay">
                <div className="popup">
                    <div id="popupExit" className="content">
                        <h1 id="close" className="popupWindow" onClick={closePopup}>&times;</h1>
                    </div>
                    <div id="popupPrompt" className="content">
                        <p>{popupMessage}</p>
                    </div>

                    <div id="login_div" className="popupContent">
                        {renderPopup(popup)}
                    </div>
                </div>
            </div>

            <Footer id="footer" />
        </div>
    )
};

export default LoginPortal;