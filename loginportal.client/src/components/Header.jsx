import PropTypes from 'prop-types';
import UserWidget from './UserWidget';

//const Header = (props) => {
const Header = ({ company, currUser, title, toggle, alt, onClick }) => {
    const companyName = company !== "" ? company.split(' ') : [""];

    return(
        <>
        <header id="Header">
            <div id="buffer"></div>
            <div id="title_div">
                {companyName.map((word, index) => (<h4 className="TCS_title" key={index}>{word}</h4>))}
            </div>
            <div className="sticky_header" onClick={onClick}>
                <div id="main_title">
                    <h1>Operations Manager</h1>
                    <h2 id="title_dash">-</h2>
                    <h2>{title}</h2>
                </div>
                <UserWidget 
                    driver={currUser}   
                    toggle={toggle}/>
            </div>
        </header>
        <div id="widgetHeader">
            <h4 className="prompt">{alt}</h4>
        </div>
        </>
    )
};

export default Header;

Header.propTypes = {
    company: PropTypes.string.isRequired,
    currUser: PropTypes.string.isRequired,
    title: PropTypes.string.isRequired, 
    toggle: PropTypes.string.isRequired,
    alt: PropTypes.string.isRequired,
    onClick: PropTypes.func.isRequired
};