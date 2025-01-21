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
    showFailFlag} from '../Scripts/helperFunctions';

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

const DriverLogin = () => {
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
    const [driverCredentials, setDriverCredentials] = useState({
        USERNAME: '',
        PASSWORD: '',
        POWERUNIT: ''
    })

    // state 'formData' for rendering forms on page...
    const [formData, setFormData] = useState({
        deliveryDate: currDate,
        powerUnit: driverCredentials.POWERUNIT
    });

    // state 'updateData' to be passed to next page...
    const [updateData, setUpdateData] = useState({
        MFSTDATE: scrapeDate(currDate),
        POWERUNIT: "000"
    });

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
        const company = await getCompany_DB();
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
        setDriverCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
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
                setDriverCredentials({
                    ...driverCredentials,
                    USERNAME: val
                });
                break;
            case 'PASSWORD':
                setDriverCredentials({
                    ...driverCredentials,
                    PASSWORD: val
                });
                break;
            default:
                break;
        }
    };

    /*/////////////////////////////////////////////////////////////////
    // handle delivery query form changes...
    [void] : handleDeliveryChange(event) {
        if (e.target.id === "dlvddate"):
            update formData with renderDate(date)
            update updateData with scrapeDate(date)
        if (e.target.id === "powerunit"):
            update formData with new date val
            update updateData with new date val
        
        if (e.target.id background color != "white"):
            remove date/powerunit error styling (class 'invalid_input) on change
    } 
    *//////////////////////////////////////////////////////////////////

    const handleDeliveryChange = (e) => {
        // reset styling to default...
        if( document.getElementById(e.target.id).classList.contains("invalid_input")){
            document.getElementById("USERNAME").classList.remove("invalid_input");
            document.getElementById("PASSWORD").classList.remove("invalid_input");
            document.getElementById("dlvdate").classList.add("input_form");
            document.getElementById("powerunit").classList.add("input_form");
        }

        // handle delivery date + powerunit input field changes...
        let val = e.target.value;
        switch(e.target.id) {
            case 'dlvdate':
                setFormData({
                    ...formData,
                    deliveryDate: renderDate(val)
                });
                setUpdateData({
                    ...updateData,
                    MFSTDATE: scrapeDate(val)
                });
                break;
            case 'powerunit':
                setFormData({
                    ...formData,
                    powerUnit: val
                });
                setUpdateData({
                    ...updateData,
                    POWERUNIT: val
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

    const handleSubmit = (e) => {
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
        
        validateCredentials(driverCredentials.USERNAME,driverCredentials.PASSWORD);
    };

    /*/////////////////////////////////////////////////////////////////
    // validate credentials, prompt for correction in fail or open popup in success...
    [void] : validateCredentials(username, password) {
        package snapshot of user credentials
        post user data to database for validation
        parse response to JSON

        if (data.success):
            cache tokens from preliminary API request
            if (task = "driver"):
                set POWERUNIT in states
                reset and open input popup
                reset input field styling
            else if (task = "admin"):
                package admin rendering data
                navigate to /admin
        else:
            set username/password error styling
            return
    }
    *//////////////////////////////////////////////////////////////////

    async function validateCredentials(username, password){
        // package credentials and attempt login...
        const user_data = {
            USERNAME: username,
            PASSWORD: password,
            POWERUNIT: null
        }
        const response = await fetch(API_URL + "api/Registration/Login", {
            body: JSON.stringify(user_data),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })
        const data = await response.json();
        //console.log(data);

        if (data.success) {
            // stash tokens in storage...
            cacheToken(data.accessToken,data.refreshToken)

            if (data.task === "driver") {
                // update state variables with latest powerunit...
                setDriverCredentials({
                    ...driverCredentials,
                    POWERUNIT: data.powerunit
                });
                setUpdateData({
                    ...updateData,
                    POWERUNIT: data.powerunit
                });

                // reset popup window and open...
                setMessage(null);
                openPopup();
                //alert("Dev Reminder: Use 02/16/2024 for Delivery Date")

                // reset styling to default...
                document.getElementById("USERNAME").classList.remove("visible");
                document.getElementById("PASSWORD").classList.remove("visible");
                //document.getElementById("USERNAME").className = "";
                //document.getElementById("PASSWORD").className = "";
            }
            else if (data.task === "admin") {
                // package admin data and nav to admin page...
                const adminData = {
                    header: header,
                    company: company,
                    valid: true
                };
                navigate('/admin', {state: adminData});
            }
        }
        else {
            // trigger red borders for errors...
            document.getElementById("USERNAME").classList.add("invalid_input");
            document.getElementById("PASSWORD").classList.add("invalid_input");
            //document.getElementById("USERNAME").className = "invalid_input";
            //document.getElementById("PASSWORD").className = "invalid_input";
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // validate credentials, prompt for correction in fail or open popup in success...
    [void] : handleUpdate() {
        handleEdit(driverCredentials: username, password, powerunit) - *** save a API call by checking for new PU? ***
        handle errors

        validateDeliveries(updateData: MFSTDATE, POWERUNIT)
        handle errors


        handle error codes to provide error styling on invalid inputs
        ensure valid token + update is needed + return on fail

        update driverCredentials with latest powerunit
        parse response to JSON
        if (success):
            package /driverlog data + navigate
        else:
            reset input field styling
    }
    *//////////////////////////////////////////////////////////////////

    async function handleUpdate() {
        // target date and powerunit fields...
        const deliver_field = document.getElementById("dlvdate");
        const power_field = document.getElementById("powerunit");
        
        // map empty field cases to messages...
        let code = -1; // case -1...
        let elementID;
        const alerts = {
            0: "Delivery Date is invalid!", // case 0...
            1: "Powerunit is required!", // case 1...
            2: "Date and Powerunit are both required" // case 2...
        }
        // flag empty username...
        if (!(deliver_field.value instanceof Date) && !isNaN(deliver_field.value)){
            deliver_field.classList.add("invalid_input");
            elementID = "ff_admin_dl_un";
            code += 1;
        } 
        // flag empty powerunit...
        if (power_field.value === "" || power_field.value == null){
            power_field.classList.add("invalid_input");
            elementID = "ff_admin_dl_pu";
            code += 2;
        }

        // catch and alert user to incomplete fields...
        if (code >= 0) {
            //alert(alerts[code]);
            showFailFlag(elementID, alerts[code]);
            return;
        }

        // request token from memory, refresh as needed...
        const token = await requestAccess(driverCredentials.USERNAME);

        // handle invalid token on login...
        if (!token) {
            closePopup();
            return;
        }
        
        // update driver credentials state...
        setDriverCredentials({
            ...driverCredentials,
            POWERUNIT: updateData.POWERUNIT
        });

        // package driver/delivery credentials and validate...
        const body_data = {
            USERNAME: driverCredentials.USERNAME,
            PASSWORD: driverCredentials.PASSWORD,
            POWERUNIT: updateData.POWERUNIT,
            MFSTDATE: updateData.MFSTDATE,
        }
        const response = await fetch(API_URL + "api/Registration/VerifyPowerunit", {
            body: JSON.stringify(body_data),
            method: "POST",
            headers: {
                "Authorization": `Bearer ${token}`,
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })

        const data = await response.json();

        // set message state according to validity of delivery information...
        if(data.success){           
            // package delivery/driver information and nav to /driverlog...
            const deliveryData = {
                delivery: updateData,
                driver: driverCredentials,
                header: header,
                company: company,
                valid: true
            };
            navigate(`/driverlog`, {state: deliveryData});
        }
        else {
            //setMessage("Invalid Delivery Information");
            document.getElementById('dlvdate').classList.add("invalid_input");
            document.getElementById('powerunit').classList.add("invalid_input");
        }
    }

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
            PASSWORD: "",
            POWERUNIT: ""
        }
        setDriverCredentials(user_data);

        // set to new user popup and open...
        setMessage("New User Signin");
        openPopup();
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
            USERNAME: driverCredentials.USERNAME,
            PASSWORD: driverCredentials.PASSWORD,
            POWERUNIT: driverCredentials.POWERUNIT // this is the field that may change...
        }
        const response = await fetch(API_URL + "api/Registration/InitializeDriver", {
            body: JSON.stringify(body_data),
            method: "PUT",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })
        const data = await response.json();
        //console.log(data);

        // signal update status on screen...
        if (data.success) {
            setMessage("Update Success");
            setTimeout(() => {
                closePopup();
            },1000)
        } else {
            setMessage("Fail");
            setTimeout(() => {
                closePopup();
            },1000)
        }        
    }

    /*/////////////////////////////////////////////////////////////////
    // fetch driver and ensure null password for new driver init...
    [void] : pullDriver() {
        reset input field styling
        fetch driver credentials using curr username

        if (!success) {
            set error styling + render error flag
            return (do nothing)
        }
        else {
            cache tokens from request
            if (data.PASSWORD):
                trigger error styling + render error flag
            else:
                set credentials to username and powerunit
                prompt for first password
        }
    }
    *//////////////////////////////////////////////////////////////////

    async function pullDriver() {
        // handle and signal empty username field...
        if (driverCredentials.USERNAME == "") {
            //document.getElementById("username").className = "invalid_input";
            document.getElementById("username").classList.add("invalid_input");
            showFailFlag("ff_login_nu", "Username is required!");
            return;
        }

        // package credentials and admin status and attempt driver query...
        const body_data = {
            USERNAME: driverCredentials.USERNAME,
            PASSWORD: null,
            POWERUNIT: null,
            admin: false
        }
        const response = await fetch(API_URL + "api/Registration/PullDriver", {
            body: JSON.stringify(body_data),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })
        const data = await response.json()
        //console.log(data);

        // catch failed request and prevent behavior...
        if (!data.success) {
            //document.getElementById("username").className = "invalid_input";
            document.getElementById("username").classList.add("invalid_input");
            showFailFlag("ff_login_nu", "Username not found!");
        }
        else {
            // stash tokens in storage...
            cacheToken(data.accessToken,data.refreshToken)

            // if password exists, fail out...
            if (data.password){
                document.getElementById("username").classList.add("invalid_input");
                showFailFlag("ff_login_nu", "Username already exists!");
            } else {
                // initialize new user fields and open popup to collect password...
                setDriverCredentials({
                    USERNAME: data.username,
                    PASSWORD: "",
                    POWERUNIT: data.powerunit
                })
                setMessage("Edit New User");
            }
        }
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
        setDriverCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
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

        /* this may be inactive... 
        if (e.target.id == "edit_new_user") {
            updateDriver();
            return;
        } */

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
    // handle updates to new user credentials...
    [void] : updateNewUser(e) {
        clear error styling (if present)
        handle change for new user credentials
    }
    *//////////////////////////////////////////////////////////////////

    async function updateNewUser(e) {
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
                setDriverCredentials({
                    ...driverCredentials,
                    USERNAME: val
                });
                break;
            case 'password':
                setDriverCredentials({
                    ...driverCredentials,
                    PASSWORD: val
                });
                break;
            case 'powerunit':
                setDriverCredentials({
                    ...driverCredentials,
                    POWERUNIT: val
                });
                break;
            default:
                break;
        }
    }

    // package helper functions to organize popup functions...
    const onPress_functions = {
        "pullDriver": pullDriver,
        "updateDriver": updateDriver,
        "cancelDriver": cancelDriver
    };

    // render template...
    return(
        <div id="webpage">
            <Header 
                company={company}
                title="Driver Login"
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
                        <input type="text" id="USERNAME" value={driverCredentials.USERNAME} onChange={handleLoginChange}/>
                        <div className="fail_flag" id="ff_login_un">
                            <p>Username is required!</p>
                        </div>
                    </div>        
                    <div className="input_wrapper">
                        <label htmlFor="PASSWORD">Password:</label>
                        <input type="password" id="PASSWORD" value={driverCredentials.PASSWORD} onChange={handleLoginChange}/>
                        <div className="fail_flag" id="ff_login_pw">
                            <p>Password is required!</p>
                        </div>
                    </div>
                    <h4 id="new_user" onClick={handleNewUser}>New User Sign-in</h4>
                    <button type="submit">Login</button>
                </form>
            </div>
            <div id="popupLoginWindow" className="overlay">
                <div className="popupLogin">
                    <div id="popupLoginExit" className="content">
                        <h1 id="close" className="popupLoginWindow" onClick={closePopup}>&times;</h1>
                    </div>
                    <Popup 
                        message={message}
                        date={formData.deliveryDate}
                        powerunit={updateData.POWERUNIT}
                        closePopup={closePopup}
                        handleDeliveryChange={handleDeliveryChange}
                        handleUpdate={handleUpdate}
                        updateData={updateData}
                        driverCredentials={driverCredentials}
                        credentials={driverCredentials}
                        pressButton={submitNewUser}
                        updateNew={updateNewUser}
                        onPressFunc={onPress_functions}
                    />
                </div>
            </div>
            <Footer id="footer" />
        </div>
    )
};

export default DriverLogin;
