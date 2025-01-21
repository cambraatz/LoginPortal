/* eslint-disable react/prop-types */
import { useState, useEffect } from 'react';
import userIcon from "../images/userIcon.png";
import { useNavigate } from "react-router-dom";
import { translateDate, logout } from '../Scripts/helperFunctions';
import toggleDots from '../images/Toggle_Dots.svg';

const UserWidget = (props) => {
    const [user, setUser] = useState(props.driver);
    //const [status, setStatus] = useState(props.status);

    useEffect(() => {        
        if (props.status === "Off"){
            document.getElementById("Logout").style.display = "none";
        }
        else {
            document.getElementById("Logout").style.display = "flex";
        }

        if (props.toggle === "close") {
            document.getElementById("main_title").style.display = "none";
            document.getElementById("title_div").style.display = "none";
            document.getElementById("buffer").style.height = "10px";
            setStatus("close");
        } else {
            document.getElementById("main_title").style.display = "flex";
            document.getElementById("title_div").style.display = "flex";
            document.getElementById("buffer").style.height = "20px";
            setStatus("open");
        }
    });

    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        if (localStorage.getItem('accessToken') == null && localStorage.getItem('refreshToken') == null) {
            console.log("Successful log out operation!");
        }
        setUser("Signed Out");
        navigate('/', {state: props.company});
    }

    /*
    const handleClick = () => {
        document.getElementById("popupLogoutWindow").style.visibility = "visible";
        document.getElementById("popupLogoutWindow").style.opacity = 1;
        document.getElementById("popupLogoutWindow").style.pointerEvents = "auto";
    };

    const handleClose = () => {
        document.getElementById("popupLogoutWindow").style.visibility = "hidden";
        document.getElementById("popupLogoutWindow").style.opacity = 0;
        document.getElementById("popupLogoutWindow").style.pointerEvents = "none";
    };
    */

    //console.log("header: ", props.header)
    
    const [status,setStatus] = useState(props.toggle);

    const collapseHeader = (e) => {
        //console.log(e.target.id);
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            if (status === "open") {
                document.getElementById("main_title").style.display = "none";
                document.getElementById("title_div").style.display = "none";
                document.getElementById("buffer").style.height = "10px";
                setStatus("close");
                //e.target.id = "openToggle";
            } else {
                document.getElementById("main_title").style.display = "flex";
                document.getElementById("title_div").style.display = "flex";
                document.getElementById("buffer").style.height = "20px";
                setStatus("open");
                //e.target.id = "collapseToggle";
            }
        }
    } 
    
    return(
        <>
            <div id="collapseToggle" onClick={collapseHeader}><img id="toggle_dots" src={toggleDots} alt="toggle dots" /></div>
            <div id="AccountTab" onClick={collapseHeader}>
                <div id="sticky_MDPU">
                {(props.header === "Full" || props.header === "Manifest") && (
                    <>
                        <div>
                            <h4>Manifest Date:</h4>
                            <h4 className="weak">{props.MFSTDATE ? translateDate(props.MFSTDATE) : "00/00/0000"}</h4>
                        </div>
                        <div>
                            <h4>Power Unit:</h4>
                            <h4 className="weak">{props.POWERUNIT}</h4>
                        </div>
                    </>
                )}
                {(props.header === "Admin") && (
                    <>
                        
                    </>
                )}
                </div>
                
                <div id="sticky_creds">
                    <div id="UserWidget">
                        <img id="UserIcon" src={userIcon} alt="User Icon"/>
                        <p>{user}</p>
                    </div>
                    <div id="Logout">
                        <button onClick={handleLogout}>Log Out</button>
                    </div>
                </div>
            </div>
            {/*
            <div id="popupLogoutWindow" className="overlay">
                <div className="popupLogout">
                    <div id="popupExit" className="content">
                        <h1 id="close" onClick={handleClose}>&times;</h1>
                    </div>
                    <div id="popupLogoutPrompt" className="content">
                        <p>Are you sure you want to log out? </p>
                    </div>
                    <div className="content">
                        <div id="popupLogoutInner">
                            <button onClick={handleLogout}>Yes</button>
                        </div>
                    </div>
                </div>
            </div>
            */}
        </>
    );
};

export default UserWidget;