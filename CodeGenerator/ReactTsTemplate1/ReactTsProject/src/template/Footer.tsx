import React from "react";


const Footer: React.FC = () => {
    return (
        <footer className="sticky-footer bg-white">
            <div className="container my-auto">
                <div className="copyright text-center my-auto">
                    <span>Copyright &copy; {projectName} 2022</span>
                </div>
            </div>
        </footer>
    );
};

export default Footer;
