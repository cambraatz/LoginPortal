import { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import userIcon from "../images/userIcon.png";
import toggleDots from '../images/Toggle_Dots.svg';

const UserWidget = ({ driver, toggle }) => {
    const [user, setUser] = useState(driver);

    useEffect(() => {        
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
        setUser(driver);
    }, [toggle,driver]);
    
    const [headerStatus,setHeaderStatus] = useState(toggle);

    const collapseHeader = (e) => {
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            if (headerStatus === "open") {
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
        }
    } 
    
    return(
        <>
            <div id="collapseToggle" onClick={collapseHeader}><img id="toggle_dots" src={toggleDots} alt="toggle dots" /></div>
            <div id="AccountTab" onClick={collapseHeader}>
                <div id="sticky_creds">
                    <div id="UserWidget">
                        <img id="UserIcon" src={userIcon} alt="User Icon"/>
                        <p>{user}</p>
                    </div>
                </div>
            </div>
        </>
    );
};

export default UserWidget;

UserWidget.propTypes = {
    driver: PropTypes.string.isRequired, 
    toggle: PropTypes.string.isRequired
};