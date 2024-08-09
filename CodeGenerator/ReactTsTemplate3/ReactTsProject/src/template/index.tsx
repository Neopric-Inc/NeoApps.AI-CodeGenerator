import Footer from "./Footer";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";

const Layout: React.FC = (props) => {
    return (
        <div id="wrapper">
            <Sidebar></Sidebar>
            <div id="content-wrapper" className="d-flex flex-column">
                <div id="content">
                    <Topbar></Topbar>
                    {props.children}
                    

                </div>

                <Footer></Footer>

            </div>

        </div>
    );
};

export default Layout;
