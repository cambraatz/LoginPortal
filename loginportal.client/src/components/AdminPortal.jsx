/*/////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2023
Update Date: 1/8/2025

*//////////////////////////////////////////////////////////////////////

import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from "react-router-dom";
import Header from './Header';
import Popup from './Popup';
import Footer from './Footer';
import { API_URL, 
    isCompanyValid, 
    getToken,  
    logout, 
    isTokenValid,
    requestAccess,
    showFailFlag } from '../Scripts/helperFunctions';

/*/////////////////////////////////////////////////////////////////////
 
AdminPortal() - Administrative menu page and functionality

The following React functional component structures and renders the 
administrative menu user interface accessed only from successful 
login using admin credentials. The page allows users to add, edit and 
remove users from the driver database. Additionally, the page allows 
the admin user to change the name of the company to be dynamically 
rendered across the application.

///////////////////////////////////////////////////////////////////////

BASIC STRUCTURE:
// initialize rendered page...
    initialize date, navigation and states
    verify company name for rendering
    verify location.state to catch improper navigation

    useEffect() => 
        check delivery validity onLoad and after message state change

// page rendering helper functions...
    renderCompany() => 
        retrieve company name from database when not in memory
    collapseHeader() => 
        open/close collapsible header
    openPopup() => 
        open popup for delivery confirmation
    closePopup() => 
        close popup for delivery confirmation
    clearStyling() =>
        remove error styling from all input fields (if present)

// state management functions...
    handleUpdate() => 
        handle input field changes

// API requests + functions...
    getCompany() =>
        retrieve the active company name from DB
    updateCompany() =>
        update active company name with provided user input

    addDriver() =>
        adds a new driver and conveys errors that occur
    pullDriver() => 
        retrieve user credentials for a given username (edit/remove)
    updateDriver() =>
        collect credentials/prev user (scraped) and update in DB
    removeDriver() =>
        remove record from DB that matches credentials.USERNAME
    cancelDriver() => 
        handle cancel, clear credentials and close popup

    pressButton() =>
        handle button clicks to render respective popup selections

    [inactive] dumpUsers() =>
        debug function to dump all active users to console.log()

// render template + helpers...
    package popup helper functions
    return render template

*//////////////////////////////////////////////////////////////////////

const AdminPortal = () => {
    /* Page rendering, navigation and state initialization... */

    // location state and navigation calls...
    const location = useLocation();
    const navigate = useNavigate();

    // header toggle state...
    const [header,setHeader] = useState(location.state ? location.state.header : "open");

    // driver credentials state...
    const [credentials, setCredentials] = useState({
        USERNAME: "",
        PASSWORD: "",
        POWERUNIT: ""
    }) 
    const [previousUser, setPreviousUser] = useState("");

    // "Edit User", "Find User", "Change Company"...
    const [popup, setPopup] = useState("Edit User");

    //const [company, setCompany] = useState(location.state ? location.state.company : "contact system admin");
    const name = isCompanyValid();
    const [company, setCompany] = useState(name ? name : "No Company Set");
    const [activeCompany, setActiveCompany] = useState(company);

    // flag invalid navigation with null location.state...
    const VALID = location.state ? location.state.valid : false;

    // ensure company, token and navigation validity onLoad...
    useEffect(() => {
        // fetch company name...
        //const company = isCompanyValid();
        setCompany(isCompanyValid());
        if (!company) {
            renderCompany();
        } else {
            setCompany(company);
        }
        // validate token...
        const token = getToken();
        if(!isTokenValid(token)){
            logout();
            navigate('/');
            return;
        }
        // validate proper navigation...
        if(!VALID) {
            logout();
            navigate('/');
            return;
        }
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [activeCompany])

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
        const company = getCompany();
        if(!company) {
            setCompany("No Company Set");
        } else {
            setCompany(company);
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // initialize and manage collapsible header behavior...
    [void] : collapseHeader(event) {
        if (e.target.id === "collapseToggle" or "toggle_dots"):
            open/close header - do opposite of current "header" state
    }
    *//////////////////////////////////////////////////////////////////

    const collapseHeader = (e) => {
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            setHeader(prev => (prev === "open" ? "close" : "open"));
        }
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : openPopup() {
        make popup window visible on screen
        enable on click behavior
    }
    *//////////////////////////////////////////////////////////////////

    const openPopup = () => {
        document.getElementById("popupAddWindow").style.visibility = "visible";
        document.getElementById("popupAddWindow").style.opacity = 1;
        document.getElementById("popupAddWindow").style.pointerEvents = "auto";  
    };

    /*/////////////////////////////////////////////////////////////////
    [void] : closePopup() {
        self explanatory closing of "popupLoginWindow"
        setStatus("") and setMessage(null) - reset state data
    }
    *//////////////////////////////////////////////////////////////////

    const closePopup = () => {
        document.getElementById("popupAddWindow").style.visibility = "hidden";
        document.getElementById("popupAddWindow").style.opacity = 0;
        document.getElementById("popupAddWindow").style.pointerEvents = "none";
        clearStyling();
    };

    /*/////////////////////////////////////////////////////////////////
    [void] : clearStyling() {
        helper script to remove error styling from all standard fields
    }
    *//////////////////////////////////////////////////////////////////

    const clearStyling = () => {
        if (document.getElementById("username") && document.getElementById("username").classList.contains("invalid_input")) {
            document.getElementById("username").classList.remove("invalid_input");
        }
        if (document.getElementById("password") && document.getElementById("password").classList.contains("invalid_input")) {
            document.getElementById("password").classList.remove("invalid_input");
        }
        if (document.getElementById("powerunit") && document.getElementById("powerunit").classList.contains("invalid_input")) {
            document.getElementById("powerunit").classList.remove("invalid_input");
        }
        if (document.getElementById("company") && document.getElementById("company").classList.contains("invalid_input")) {
            document.getElementById("company").classList.remove("invalid_input");
        }
    }

    /* State management functions... */

    /*/////////////////////////////////////////////////////////////////
    // handle changes to admin input fields...
    [void] : handleUpdate() {
        clear error styling on input fields
        handle input field changes
    }
    *//////////////////////////////////////////////////////////////////

    const handleUpdate = (e) => {
        clearStyling();
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
            case "company":
                setCompany(val);
                break;
            default:
                break;
        }
    }

    /* API Calls and Functionality... */

    /*/////////////////////////////////////////////////////////////////
    // handle unique behavior of collection of admin buttons...
    [void] : getCompany() {
        verify valid token credentials
        fetch company name on file
        if (success):
            setCompany to name on file
        else:
            setCompany to placeholder
    }

    NOTE: helper `getCompany_DB` does nearly the same, replace this?

    *//////////////////////////////////////////////////////////////////

    async function getCompany() {
        // request token from memory, refresh as needed...
        const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            navigate('/');
            return;
        }

        const response = await fetch(API_URL + "api/Registration/GetCompany?COMPANYKEY=c01", {
            method: "GET",
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            setCompany(data["COMPANYNAME"]);
        }
        else {
            setCompany("Your Company Here");
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // update active company name with provided user input...
    [void] : updateCompany() {
        trigger input error styling when invalid + render flag
        validate token credentials
        update the company name in DB

        if (!success):
            render error popup + reset popup
        else:
            set company state
            render success popup + close popup
    }
    *//////////////////////////////////////////////////////////////////

    async function updateCompany() {
        const comp_field = document.getElementById("company");
        if (comp_field.value === "" || comp_field.value === "Your Company Here") {
            document.getElementById("company").classList.add("invalid_input");
            showFailFlag("ff_admin_cc", "Company name is required!")
            return;
        }

        // request token from memory, refresh as needed...
        const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            navigate('/');
            return;
        }

        const response = await fetch(API_URL + "api/Registration/SetCompany", {
            body: JSON.stringify(company),
            method: "PUT",
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
            }
        })

        const data = await response.json();
        //console.log("data: ",data);

        if (!data.success) {
            console.trace("company name value mismatch");
            setPopup("Fail");

            setTimeout(() => {
                setPopup("Change Company");
            },2000)
        }
        else {
            // set active, company is updated dynamically...
            setActiveCompany(data.COMPANYNAME);
            setPopup("Company Success");
            
            setTimeout(() => {
                closePopup();
            },1000)
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // adds a new driver, convey any errors in response...
    [void] : addDriver() {
        define input field alert status
        apply error styling and render flag when present

        package driver credentials into form for request body
        add driver credentials to database
        if (success):
            render success popup notification
        else:
            if (pk violation):
                render duplicate driver fail popup
            else:
                render add driver fail popup
    }
    *//////////////////////////////////////////////////////////////////

    async function addDriver() {
        const user_field = document.getElementById("username")
        const power_field = document.getElementById("powerunit")
        
        let code = -1;
        const messages = [
            "Username is required!", 
            "Powerunit is required!", 
            "Username and Powerunit are required!"
        ]
        const alerts = {
            0: "ff_admin_au_un", // Username is required!...
            1: "ff_admin_au_pu", // Powerunit is required!...
            2: "ff_admin_au_up" // Username and Powerunit are required!...
        }

        if (user_field.value === "" || user_field.value == null){
            user_field.classList.add("invalid_input");
            code += 1;

        } 
        
        if (power_field.value === "" || power_field.value == null){
            power_field.classList.add("invalid_input");
            code += 2;
        }

        if (code >= 0) {
            showFailFlag(alerts[code], messages[code])
            return;
        }

        //console.log(`replace with ${credentials}`);
        let formData = new FormData()
        formData.append("USERNAME", credentials.USERNAME);
        formData.append("PASSWORD", null);
        formData.append("POWERUNIT", credentials.POWERUNIT);

        //const form = ["USERNAME","PASSWORD","POWERUNIT","PREVUSER"];
        //form.forEach(key => console.log(`${key}: ${formData.get(key)}`));

        // eslint-disable-next-line no-unused-vars
        const response = await fetch(API_URL + "api/Registration/AddDriver", {
            body: formData,
            method: "PUT",
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            setPopup("Add Success");
            setTimeout(() => {
                closePopup();
            },1000)
        }
        else {
            if (data.error.includes("Violation of PRIMARY KEY")) {
                setPopup("Admin_Add Fail");
            } else {
                setPopup("Fail");
            }
            setTimeout(() => {
                closePopup();
            },1500)
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // adds a new driver, convey any errors in response...
    [void] : pullDriver() {
        define input field alert status
        apply error styling and render flag when present

        package driver credentials into form for request body
        add driver credentials to database
        if (success):
            render success popup notification
        else:
            if (pk violation):
                render duplicate driver fail popup
            else:
                render add driver fail popup
    }
    *//////////////////////////////////////////////////////////////////

    async function pullDriver() {
        if (document.getElementById("username").value === "") {
            document.getElementById("username").classList.add("invalid_input");

            showFailFlag("ff_admin_fu","Username is required!");
            return;
        }

        const body_data ={
            USERNAME: credentials.USERNAME,
            PASSWORD: null,
            POWERUNIT: null,
            admin: true
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
        if (data.success) {
            setPreviousUser(credentials.USERNAME);
            setCredentials({
                USERNAME: data.username,
                PASSWORD: data.password,
                POWERUNIT: data.powerunit
            })
            setPopup("Edit User");
        }
        else {
            document.getElementById("username").className = "invalid_input";
            document.getElementById("ff_admin_fu").classList.add("visible");
            setTimeout(() => {
                document.getElementById("ff_admin_fu").classList.remove("visible");
            },1500)
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // collect credentials/prev user (scraped) and update in DB...
    [void] : updateDriver() {
        define input field alert status
        apply error styling and render flag when present
        validate token and refresh as needed

        package driver credentials into form for request body
        add driver credentials to database
        if (success):
            render success popup notification
        else:
            render failure popup notification
    }
    *//////////////////////////////////////////////////////////////////

    async function updateDriver() {
        const user_field = document.getElementById("username")
        const power_field = document.getElementById("powerunit")
        
        // map empty field cases to messages...
        let code = -1;
        const messages = [
            "Username is required!", 
            "Powerunit is required!", 
            "Username and Powerunit are required!"
        ]
        const alerts = {
            0: "ff_admin_eu_un", // Username is required!...
            1: "ff_admin_eu_pu", // Powerunit is required!...
            2: "ff_admin_eu_up" // Username and Powerunit are required!...
        }
        // flag empty username...
        if (user_field.value === "" || user_field.value == null){
            user_field.classList.add("invalid_input");
            code += 1;
        } 
        // flag empty powerunit...
        if (power_field.value === "" || power_field.value == null){
            power_field.classList.add("invalid_input");
            code += 2;
        }

        // catch and alert user to incomplete fields...
        if (code >= 0) {
            showFailFlag(alerts[code],messages[code]);
            return;
        }

        // request token from memory, refresh as needed...
        const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            navigate('/');
            return;
        }

        // package driver credentials and attempt to replace...
        const body_data = {
            ...credentials,
            PREVUSER: previousUser
        }
        const response = await fetch(API_URL + "api/Registration/ReplaceDriver", {
            body: JSON.stringify(body_data),
            method: "PUT",
            headers: {
                "Authorization": `Bearer ${token}`,
                'Content-Type': 'application/json; charset=UTF-8'
            }
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            setPreviousUser("add new");
            setPopup("Update Success");
        }
        else {
            console.trace("update driver failed");
            setPopup("Fail");
        }

        setTimeout(() => {
            closePopup();
        },1000)
    }

    /*/////////////////////////////////////////////////////////////////
    // remove record from DB that matches credentials.USERNAME...
    [void] : removeDriver() {
        validate token and refresh as needed

        remove driver from database
        if (success):
            render success popup notification
        else:
            render failure popup notification
    }
    *//////////////////////////////////////////////////////////////////

    async function removeDriver() {
        // request token from memory, refresh as needed...
        const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            navigate('/');
            return;
        }

        // eslint-disable-next-line no-unused-vars
        const response = await fetch(API_URL + "api/Registration/DeleteDriver?USERNAME=" + credentials.USERNAME, {
            method: "DELETE",
            headers: {
                "Authorization": `Bearer ${token}`,
            }
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            setPopup("Delete Success");
            setTimeout(() => {
                closePopup();
            },1000)
        }
        else {
            console.trace("delete driver failed");
            setPopup("Fail");
            setTimeout(() => {
                closePopup();
            },2000)
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // handle cancel, clear credentials and close popup...
    [void] : cancelDriver() {
        reset driver credentials to null
        close pop up
    }
    *//////////////////////////////////////////////////////////////////

    async function cancelDriver() {
        setCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
        })
        closePopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // handle unique behavior of collection of admin buttons...
    [void] : pressButton() {
        reset driver credentials to null
        handle admin button clicks to set popup state
        opens popup
    }
    *//////////////////////////////////////////////////////////////////

    async function pressButton(e) {
        setCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
        })

        // handle main admin button popup generation
        switch(e.target.innerText){
            case "Add New User":
                setPopup("Add User");
                setPreviousUser("add new");
                break;
            case "Change/Remove User":
                setPopup("Find User");
                break;
            case "Edit Company Name":
                setPopup("Change Company");
                getCompany();
                break;
            default:
                break;
        }
        openPopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // debug function to dump all active users to console.log()...
    [void] : dumpUsers() {
        fetch all drivers
        print to console.log()
    }
    *//////////////////////////////////////////////////////////////////

    // eslint-disable-next-line no-unused-vars
    async function dumpUsers() {
        const response = await fetch(API_URL + "api/Registration/GetAllDrivers", {
            method: "GET",
        })

        const data = await response.json();
        console.log("data: ",data);
    }

    // package helper functions to organize popup functions...
    const onPress_functions = {
        "addDriver": addDriver,
        "pullDriver": pullDriver,
        "updateDriver": updateDriver,
        "removeDriver": removeDriver,
        "cancelDriver": cancelDriver,
        "updateCompany": updateCompany
    };

    // render template...
    return(
        <div id="webpage">
            <Header
                company={activeCompany}
                title="Admin Portal"
                alt="What would you like to do?"
                status="admin"
                currUser="admin"
                MFSTDATE={null} 
                POWERUNIT={null}
                STOP = {null}
                PRONUMBER = {null}
                MFSTKEY = {null}
                toggle={header}
                onClick={collapseHeader}
            />
            <div id="admin_div">
                <button type="button" onClick={pressButton}>Add New User</button>
                <button type="button" onClick={pressButton}>Change/Remove User</button>
                <button type="button" onClick={pressButton}>Edit Company Name</button>
            </div>
            <div id="popupAddWindow" className="overlay">
                <div className="popupLogin">
                    <div id="popupAddExit" className="content">
                        <h1 id="close_add" className="popupLoginWindow" onClick={closePopup}>&times;</h1>
                    </div>
                    <Popup 
                        message={popup}
                        date={null}
                        powerunit={null}
                        closePopup={closePopup}
                        handleDeliveryChange={null}
                        handleUpdate={handleUpdate}
                        pressButton={pressButton}
                        credentials={credentials}
                        company={company}
                        onPressFunc={onPress_functions}
                    />
                </div>
            </div>
            {/*<button onClick={dumpUsers}>dump users</button>*/}
            <Footer id="footer"/>
        </div>
    )
};

export default AdminPortal;