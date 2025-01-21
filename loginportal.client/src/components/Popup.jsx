import Success from '../assets/success.svg';
import Fail from '../assets/error.svg';

/* eslint-disable react/prop-types */

const Popup = (props) => {
    if(props.message === null){
        return(
            <>
                <div id="popupLoginPrompt" className="content">
                    <p>Select Company</p>
                </div>
                <div id="login_div">
                    <button type="button" onClick={props.pressButton}>Add New User</button>
                    <button type="button" onClick={props.pressButton}>Change/Remove User</button>
                    <button type="button" onClick={props.pressButton}>Edit Company Name</button>
                </div>
            </>
        )
    }
    else if(props.message === "Invalid Delivery Information"){
        return(
            <>
                <div id="popupLoginPrompt" className="content">
                    <p>{props.message}</p>
                </div>
                <div className="popupLoginContent">
                    <div>
                        <label>Delivery Date</label>
                        <input type="date" id="dlvdate" value={props.date} className="input_form invalid_input" onChange={props.handleDeliveryChange} />
                    </div>
                    <div>
                        <label>Power Unit</label>
                        <input type="text" id="powerunit" value={props.powerunit} className="input_form invalid_input" onChange={props.handleDeliveryChange} />
                    </div>
                    <div id="popupLoginInner">
                        <button onClick={props.handleUpdate}>Continue</button>
                    </div>
                </div>
            </>
        )
    }
    else if(props.message === "Delivery Information Found"){
        return(
            <>
                <div id="popupLoginPrompt" className="content">
                    <p>{props.message}</p>
                </div>
            </>
        )
    }
    else if(props.message === "Signature"){
        return(
            <>
                <div id="popupLoginPrompt" className="content">
                    <p>Confirm Delivery Info</p>
                </div>
                <div className="popupLoginContent">
                    <div>
                        <label>Delivery Date</label>
                        <input type="date" id="dlvdate" value={props.date} className="input_form" onChange={props.handleDeliveryChange} />
                    </div>
                    <div>
                        <label>Power Unit</label>
                        <input type="text" id="powerunit" value={props.powerunit} className="input_form" onChange={props.handleDeliveryChange} />
                    </div>
                    <div id="popupLoginInner">
                        <button onClick={props.handleUpdate}>Validate</button>
                    </div>
                </div>
            </>
        )
    }
    else if(props.message === "Edit User"){
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_eu_un">
                            <p>Username is required!</p>
                        </div>
                    </div>
                    <div>
                        <label>Password</label>
                        <input type="text" id="password" value={props.credentials.PASSWORD ? props.credentials.PASSWORD : ""} className="input_form" onChange={props.handleUpdate}/>
                    </div>
                    <div className="input_wrapper">
                        <label>Power Unit</label>
                        <input type="text" id="powerunit" value={props.credentials.POWERUNIT} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_eu_pu">
                            <p>Powerunit is required!</p>
                        </div>
                        <div className="fail_flag" id="ff_admin_eu_up">
                            <p>Username and Powerunit are required!</p>
                        </div>
                    </div>
                    <div id="submit_user">
                        <button className="popup_button" onClick={props.onPressFunc.updateDriver}>Update User</button>
                    </div>
                    <div id="remove_user">
                        <button className="popup_button" onClick={props.onPressFunc.removeDriver}>Remove User</button>
                    </div>
                    <div id="cancel_user" >
                        <button className="popup_button" onClick={props.onPressFunc.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    else if(props.message === "Edit New User"){
        return(
            <div className="popupLoginContent">
                <div id="popupLoginPrompt" className="content">
                    <p>Set Password</p>
                </div>
                <div>
                    <label>Username</label>
                    <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.updateNew} disabled/>
                </div>
                <div className="input_wrapper">
                    <label>Password</label>
                    <input type="text" id="password" value={props.credentials.PASSWORD} className="input_form" onChange={props.updateNew} required/>
                    <div className="fail_flag" id="ff_admin_enu_pw">
                        <p>Password is required!</p>
                    </div>
                </div>
                <div className="input_wrapper">
                    <label>Power Unit</label>
                    <input type="text" id="powerunit" value={props.credentials.POWERUNIT} className="input_form" onChange={props.updateNew} required/>
                    <div className="fail_flag" id="ff_admin_enu_pu">
                        <p>Powerunit is required!</p>
                    </div>
                </div>
                <div id="submit_user">
                    <button className="popup_button" onClick={props.onPressFunc.updateDriver}>Update User</button>
                </div>
                <div id="cancel_user">
                    <button type="button" className="popup_button" onClick={props.onPressFunc.cancelDriver}>Cancel</button>
                </div>
            </div>
        )
    }
    else if(props.message === "Add User"){
        return(
            <div className="popupLoginContent">
                <div className="input_wrapper">
                    <label>Username</label>
                    <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.handleUpdate} required/>
                    <div className="fail_flag" id="ff_admin_au_un">
                        <p>Username is required!</p>
                    </div>
                </div>
                <div className="input_wrapper">
                    <label>Power Unit</label>
                    <input type="text" id="powerunit" value={props.credentials.POWERUNIT} className="input_form" onChange={props.handleUpdate} required/>
                    <div className="fail_flag" id="ff_admin_au_pu">
                        <p>Powerunit is required!</p>
                    </div>
                    <div className="fail_flag" id="ff_admin_au_up">
                        <p>Username and Powerunit are required!</p>
                    </div>
                </div>
                <div id="add_user">
                    <button type="button" className="popup_button" onClick={props.onPressFunc.addDriver}>Add User</button>
                </div>
                <div id="cancel_user">
                    <button type="button" className="popup_button" onClick={props.onPressFunc.cancelDriver}>Cancel</button>
                </div>
            </div>
        )
    }
    // admin logic...
    else if(props.message === "Find User"){
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_fu">
                            <p>Username was not found!</p>
                        </div>
                    </div>
                    <div id="find_user">
                        <button id="find_user" className="popup_button" onClick={props.onPressFunc.pullDriver}>Find User</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={props.onPressFunc.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    // login logic...
    else if(props.message === "New User Signin"){
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input type="text" id="username" value={props.credentials.USERNAME ? props.credentials.USERNAME : ""} className="input_form" onChange={props.updateNew}/>
                        <div className="fail_flag" id="ff_login_nu">
                            <p>Username was not found!</p>
                        </div>
                    </div>
                    <div id="set_password">
                        <button id="set_password" className="popup_button" onClick={props.pressButton}>Authorize</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={props.onPressFunc.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    else if(props.message === "Change Company"){
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Company Name</label>
                        <input type="text" id="company" value={props.company} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_cc">
                            <p>Company name is required!</p>
                        </div>
                    </div>
                    <div id="submit_company">
                        <button className="popup_button" onClick={props.onPressFunc.updateCompany}>Update Company</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={props.onPressFunc.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    else if(props.message === "Add Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="succss"/>
                    <p>{user} was added successfully!</p>
                </div>
            </>
        )
    }
    else if(props.message === "Update Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="succss"/>
                    <p>{user} successfully updated!</p>
                </div>
            </>
        )
    }
    else if(props.message === "Delete Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="succss"/>
                    <p>{user} successfully deleted!</p>
                </div>
            </>
        )
    }
    else if(props.message === "Company Success"){
        //const company = props.company;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="succss"/>
                    <p>Company successfully updated!</p>
                </div>
            </>
        )
    }
    else if(props.message === "Success"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="succss"/>
                    <p>Delivery successfully updated!</p>
                </div>
            </>
        )
    }
    else if(props.message === "Fail"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Oops! Something went wrong, please try again.</p>
                </div>
            </>
        )
    }
    else if(props.message === "Admin_Add Fail"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Oops! User already exists, please try again.</p>
                </div>
            </>
        )
    }
};

export default Popup;