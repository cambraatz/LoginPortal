import { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import userIcon from "../images/userIcon.png";
import { useNavigate } from "react-router-dom";
import { translateDate, logout } from '../scripts/helperFunctions';
import toggleDots from '../images/Toggle_Dots.svg';

const UserWidget = ({ driver, status, company, toggle, header, MFSTDATE, POWERUNIT }) => {
    const [user, setUser] = useState(driver);
    //const [status, setStatus] = useState(props.status);

    useEffect(() => {        
        if (status === "Off"){
            document.getElementById("Logout").style.display = "none";
        }
        else {
            document.getElementById("Logout").style.display = "flex";
        }

        if (toggle === "close") {
            document.getElementById("main_title").style.display = "none";
            document.getElementById("title_div").style.display = "none";
            document.getElementById("buffer").style.height = "10px";
            setHeaderStatus("close");
        } else {
            document.getElementById("main_title").style.display = "flex";
            document.getElementById("title_div").style.display = "flex";
            document.getElementById("buffer").style.height = "20px";
            setHeaderStatus("open");
        }
    });

    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        if (localStorage.getItem('accessToken') == null && localStorage.getItem('refreshToken') == null) {
            console.log("Successful log out operation!");
        }
        setUser("Signed Out");
        navigate('/', {state: company});
    }
    
    const [headerStatus,setHeaderStatus] = useState(toggle);

    const collapseHeader = (e) => {
        //console.log(e.target.id);
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            if (headerStatus === "open") {
                document.getElementById("main_title").style.display = "none";
                document.getElementById("title_div").style.display = "none";
                document.getElementById("buffer").style.height = "10px";
                setHeaderStatus("close");
                //e.target.id = "openToggle";
            } else {
                document.getElementById("main_title").style.display = "flex";
                document.getElementById("title_div").style.display = "flex";
                document.getElementById("buffer").style.height = "20px";
                setHeaderStatus("open");
                //e.target.id = "collapseToggle";
            }
        }
    } 
    
    return(
        <>
            <div id="collapseToggle" onClick={collapseHeader}><img id="toggle_dots" src={toggleDots} alt="toggle dots" /></div>
            <div id="AccountTab" onClick={collapseHeader}>
                <div id="sticky_MDPU">
                {(header === "Full" || header === "Manifest") && (
                    <>
                        <div>
                            <h4>Manifest Date:</h4>
                            <h4 className="weak">{MFSTDATE ? translateDate(MFSTDATE) : "00/00/0000"}</h4>
                        </div>
                        <div>
                            <h4>Power Unit:</h4>
                            <h4 className="weak">{POWERUNIT}</h4>
                        </div>
                    </>
                )}
                {(header === "Admin") && (
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
        </>
    );
};

export default UserWidget;

UserWidget.propTypes = {
    driver: PropTypes.string.isRequired, 
    status: PropTypes.string.isRequired,
    toggle: PropTypes.string.isRequired,
    company: PropTypes.string.isRequired,
    header: PropTypes.string,
    MFSTDATE: PropTypes.string,
    POWERUNIT: PropTypes.string
};