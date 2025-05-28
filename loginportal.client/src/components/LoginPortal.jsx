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
    resetStyling,
    flagError,
    errorStyling,
    showFailFlag
} from '../scripts/helperFunctions';

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
    // validate user; uses stored creds otherwise clear cookies...
    useEffect(() => {
        pullCredentials();
    }, [])

    /* Site state & location processing functions... */

    // driver credentials to manage input form...
    const [credentials, setCredentials] = useState({
        USERNAME: '',
        PASSWORD: ''
    })

    // header toggle status for collapsible header...
    const [header,setHeader] = useState("open");

    // leverage cookies to fetch current user credentials...
    async function pullCredentials() {
        // fetch credentials
        const response = await fetch(API_URL + "v1/sessions/credentials", {
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: "include",
        });

        // if valid response, 
        if (response.ok) {
            const data = await response.json();

            // update state values to curr user...      
            setUser(data.user);
            setCompanies(data.user.Companies);
            setModules(data.user.Modules);
            
            // initialize company/module selection popup...
            setPopupMessage("Select Module");
            setPopup("module");
            openPopup();
        } else {
            // incomplete cookies, clean slate and start fresh...
            console.log("Cookie access failed, response was valid.");
            await cleanSlate();
        }        
    }

    /* Page rendering helper functions... */
    async function cleanSlate() {
        localStorage.clear();
        sessionStorage.clear();

        const response = await fetch(API_URL + "v1/sessions/logout", {
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
        })
        if (!response.ok) {
            //console.log("Cookies have been cleared successfully.");
            console.alert("Cookie removal failed, Logout failure.");
        } else {
            fetchMappings();
        }
    }

    async function fetchMappings() {
        //const mapping_response = await fetch(`${API_URL}api/Login/FetchMappings`, {
        const mapping_response = await fetch(`${API_URL}v1/mappings?type=all`, {
            method: "GET",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })

        if(mapping_response.ok) {
            const mappings = await mapping_response.json();
            sessionStorage.setItem("companies_map", JSON.stringify(mappings.companies));
            console.log("companies_map: ", mappings.companies);
            
            sessionStorage.setItem("modules_map", JSON.stringify(mappings.modules));
            console.log("modules_map: ", mappings.modules);                
        } else {
            console.error("Error setting mapping cookies.");
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

        /*const response = await fetch(API_URL + "api/Login/Login", {
            body: JSON.stringify(credentials),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })*/

        const response = await fetch(API_URL + "v1/sessions/login", {
            body: JSON.stringify(credentials),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })

        //const data = await response.json();
        //console.log(data);

        //if (data.success) {
        if (response.ok) {
            const data = await response.json();

            // update state values to curr user...      
            setUser(data.user);
            setCompanies(data.user.Companies);
            setModules(data.user.Modules);
            
            // initialize company/module selection popup...
            setPopupMessage("Select Company");
            setPopup("company");

            openPopup();
            return;
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

    async function newUserChange(e) {
        e.preventDefault();

        // handle new user menu button clicks...
        switch(e.target.parentElement.id){
            case "set_password":
                pullDriver();
                break;
            case "submit_user":
                updateDriver();
                break;
            case "cancel_user":
                closePopup();
                break;
            default:
                break;
        }
    }

        /*/////////////////////////////////////////////////////////////////
    // collect password + powerunit to initialize new driver credentials...
    [void] : updateDriver() {
        handle input field error styling
        package driver credentials
        update new user in DB to have valid credentials
        verify and render success status
    }
    *//////////////////////////////////////////////////////////////////

    async function updateDriver() {
        // target password and powerunit fields...
        const pass_field = document.getElementById("password");
        const pow_field = document.getElementById("powerunit");

        // map empty field cases to messages...
        let code = -1; // case -1...
        let elementID;
        const alerts = {
            0: "Password is required!", // case 0...
            1: "Powerunit is required!", // case 1...
            2: "Password and Powerunit are required!" // case 2...
        }

        // flag empty password and powerunit fields...
        if(!(pass_field.value)) {
            pass_field.classList.add("invalid_input");
            elementID = "ff_admin_enu_pw";
            code += 1;
        }
        if (!(pow_field.value)) {
            pow_field.classList.add("invalid_input");
            elementID = "ff_admin_enu_pu";
            code += 2;
        }

         // catch and alert user to incomplete fields...
        if (code >= 0) {
            showFailFlag(elementID, alerts[code]);
            return;
        }

        // package credentials and attempt updating records...
        const body_data = {
            USERNAME: credentials.USERNAME,
            PASSWORD: credentials.PASSWORD,
            POWERUNIT: credentials.POWERUNIT // this is the field that may change...
        }
        const response = await fetch(API_URL + "api/Login/InitializeDriver", {
            body: JSON.stringify(body_data),
            method: "PUT",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        const data = await response.json();
        //console.log(data);

        // signal update status on screen...
        if (data.success) {
            setPopup("Update Success");
            setTimeout(() => {
                closePopup();
            },1000)
        } else {
            setPopup("Fail");
            setTimeout(() => {
                closePopup();
            },1000)
        }        
    }

    async function pullDriver() {
        // handle and signal empty username field...
        if (credentials.USERNAME == "") {
            //document.getElementById("username").className = "invalid_input";
            document.getElementById("username").classList.add("invalid_input");
            showFailFlag("ff_login_nu", "Username is required!");
            return;
        }

        // package credentials and admin status and attempt driver query...
        const body_data = {
            USERNAME: credentials.USERNAME,
            PASSWORD: null,
            POWERUNIT: null
        }
        const response = await fetch(API_URL + "api/Login/PullDriver", {
            body: JSON.stringify(body_data),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        });
        
        const data = await response.json()
        //console.log(data);

        // catch failed request and prevent behavior...
        if (!data.success) {
            //document.getElementById("username").className = "invalid_input";
            document.getElementById("username").classList.add("invalid_input");
            showFailFlag("ff_login_nu", "Username not found!");
        }
        else {
            // if password exists, fail out...
            if (data.password){
                document.getElementById("username").classList.add("invalid_input");
                showFailFlag("ff_login_nu", "Username already exists!");
            } else {
                // initialize new user fields and open popup to collect password...
                setCredentials({
                    USERNAME: data.username,
                    PASSWORD: "",
                    POWERUNIT: data.powerunit
                })
                setPopup("edit_new_user");
            }
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // handle updates to new user credentials...
    [void] : newUserChange(e) {
        clear error styling (if present)
        handle change for new user credentials
    }
    *//////////////////////////////////////////////////////////////////

    async function newUsernameChange(e) {
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
        console.log(`targeting e.target.id: ${e.target.id}`);
        console.log("company options: ", companies);
        console.log("mdules options: ", modules);

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
            const COMPANIES = JSON.parse(sessionStorage.getItem("companies_map") || "{}");
            //const COMPANIES = sessionStorage.getItem("companies_map") || "{}";
            setUser({...user, ActiveCompany: COMPANIES[company]});

            // select company to work under...
            const response = await fetch(API_URL + "api/Login/SetCompany", {
                body: JSON.stringify({ 
                    username: credentials.USERNAME,
                    company: Object.keys(COMPANIES).find(key => COMPANIES[key] === COMPANIES[company]) 
                }),
                method: "POST",
                headers: {
                    'Content-Type': 'application/json; charset=UTF-8'
                }
            }); if (!response.ok) {
                console.alert("Company selection failed");
            }

            // initialize popup for module selection, RETURN...
            setPopupMessage("Select Module");
            setPopup("module");
            return;
        }

        // match clicked module and assign to mod...
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

        // navigates to selected module using MODULEURL...
        if (mod) {
            console.log(`mod: ${mod}`);
            if (mod === "deliverymanager" || mod === "admin") {
                window.location.href = `https://${mod}.tcsservices.com/`;
            } else {
                closePopup();
            }
            return;
        }

        closePopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // handle updates to new user credentials...
    [void] : editNewUser(e) {
        clear error styling (if present)
        handle change for new user credentials
    }
    *//////////////////////////////////////////////////////////////////

    async function handleNewUserChange(e) {
        //if (document.getElementById(e.target.id).className == "invalid_input"){
        if (document.getElementById(e.target.id).classList.contains("invalid_input")){
            //document.getElementById("username").className = "";
            //document.getElementById("password").className = "";
            //document.getElementById("powerunit").className = "";

            // reset styling to default...
            document.getElementById("username").classList.remove("invalid_input");

            // skip password + powerunit when username specific...
            if (document.getElementById("password")){
                document.getElementById("password").classList.remove("invalid_input");
            }
            if (document.getElementById("powerunit")){
                document.getElementById("powerunit").classList.remove("invalid_input");
            }
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
            case 'powerunit':
                setCredentials({
                    ...credentials,
                    POWERUNIT: val
                });
                break;
            default:
                break;
        }
    }

    const [popup, setPopup] = useState("company");
    const [popupMessage, setPopupMessage] = useState(null);

    const renderPopup = (type) => {
        const COMPANIES = JSON.parse(sessionStorage.getItem("companies_map") || "{}");
        const MODULES = JSON.parse(sessionStorage.getItem("modules_map") || "{}");

        if (type == "company") {
            if (companies.length > 0) {
                return companies.map((company,i) => {
                    if (company === "") { return null; }
                    console.log(`company: ${company}`);
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
                return (
                    <>
                    {modules.map((module,i) => {
                        if (module === "") { return null; }
                        return (
                            <button id={"md"+(i+1)} key={module+1} type="button" onClick={pressButton}>
                                {MODULES[module]}
                            </button>
                        );
                    })}
                    </>
                )
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
                        <input type="text" id="username" value={credentials.USERNAME ? credentials.USERNAME : ""} className="input_form" onChange={newUsernameChange}/>
                        <div className="fail_flag" id="ff_login_nu">
                            <p>Username was not found!</p>
                        </div>
                    </div>
                    <div id="set_password">
                        <button id="set_password" className="popup_button" onClick={newUserChange}>Set Password</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={cancelDriver}>Cancel</button>
                    </div>
                </>
            );
        } else if (type == "edit_new_user") {
            return (
                <>
                <div className="input_wrapper">
                    <label>Username</label>
                    <input type="text" id="username" value={credentials.USERNAME ? credentials.USERNAME : ""} className="input_form" onChange={newUsernameChange} disabled/>
                    <div className="fail_flag" id="ff_login_nu">
                        <p>Username was not found!</p>
                    </div>
                </div>
                <div className="input_wrapper">
                    <label>Password</label>
                    <input type="text" id="password" value={credentials.PASSWORD} className="input_form" onChange={handleNewUserChange} required/>
                    <div className="fail_flag" id="ff_admin_enu_pw">
                        <p>Password is required!</p>
                    </div>
                </div>
                <div className="input_wrapper">
                    <label>Power Unit</label>
                    <input type="text" id="powerunit" value={credentials.POWERUNIT} className="input_form" onChange={handleNewUserChange} required/>
                    <div className="fail_flag" id="ff_admin_enu_pu">
                        <p>Powerunit is required!</p>
                    </div>
                </div>
                <div id="submit_user">
                    <button className="popup_button" onClick={updateDriver}>Update User</button>
                </div>
                <div id="cancel_user">
                    <button type="button" className="popup_button" onClick={cancelDriver}>Cancel</button>
                </div>
                </>
            );
        }
    }

    // render template...
    return(
        <div id="webpage">
            <Header 
                company="Transportation Computer Support, LLC"
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