import PropTypes from 'prop-types';

const Footer = ({ id }) => {
    return(
        <div id={id}>
            <p id="footer_text">Developed by Transportation Computer Support, LLC.</p>
        </div>
    )
}

export default Footer;

Footer.propTypes = {
    id: PropTypes.string.isRequired, // Ensures `id` is a required string
};