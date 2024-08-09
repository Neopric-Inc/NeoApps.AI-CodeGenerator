import React from "react";
import { useNavigate } from "react-router";
import { Button, Nav } from "react-bootstrap";
import { useSelector } from "react-redux";
import { activateTopMenu, sidebarToggle } from "redux/actions";
import { RootState } from "redux/reducers";
import { useAppDispatch } from "redux/store";
const Topbar: React.FC = () => {
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const handleActiveDropdown = (menuName: string) => {
        dispatch(activateTopMenu(menuName))
    }
    const isTopActiveMenu = useSelector((state: RootState) => state.template.topActiveMenu);
    const userProfile = useSelector((state: RootState) => state.template.userProfile);
    const handleLogout = () => {
        localStorage.clear();
        navigate('/', { replace: true });
    }
    return (
        <Nav className="navbar navbar-expand navbar-light bg-white topbar mb-4 static-top shadow">


            <button id="sidebarToggleTop" className="btn btn-link text-dark d-md-none rounded-circle mr-3" onClick={() => dispatch(sidebarToggle())}>
                <i className="fa fa-bars"></i>
            </button>
            <ul className="navbar-nav ml-auto">
                <li className={isTopActiveMenu === "Notification" ? "nav-item dropdown no-arrow mx-1 show" : "nav-item dropdown no-arrow mx-1"} >

                    <Nav.Link className="nav-link dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded={isTopActiveMenu === "Notification" ? true : false} onClick={() => handleActiveDropdown("Notification")}>
                        <i className="fas fa-bell fa-fw"></i>

                        <span className="badge badge-danger badge-counter">3+</span>
                    </Nav.Link>

                    <div className={isTopActiveMenu === "Notification" ? "dropdown-menu dropdown-menu-right p-3 shadow animated--grow-in show" : "dropdown-menu dropdown-menu-right p-3 shadow animated--grow-in"}
                        aria-labelledby="alertsDropdown">
                        <h6 className="dropdown-header">
                            Alerts Center
                        </h6>
                        <a className="dropdown-item d-flex align-items-center">
                            <div className="mr-3">
                                <div className="icon-circle bg-primary">
                                    <i className="fas fa-file-alt text-white"></i>
                                </div>
                            </div>
                            <div>
                                <div className="small text-gray-500">December 12, 2019</div>
                                <span className="font-weight-bold">A new monthly report is ready to download!</span>
                            </div>
                        </a>
                        <a className="dropdown-item d-flex align-items-center">
                            <div className="mr-3">
                                <div className="icon-circle bg-success">
                                    <i className="fas fa-donate text-white"></i>
                                </div>
                            </div>
                            <div>
                                <div className="small text-gray-500">December 7, 2019</div>
                                $290.29 has been deposited into your account!
                            </div>
                        </a>
                        <a className="dropdown-item d-flex align-items-center">
                            <div className="mr-3">
                                <div className="icon-circle bg-warning">
                                    <i className="fas fa-exclamation-triangle text-white"></i>
                                </div>
                            </div>
                            <div>
                                <div className="small text-gray-500">December 2, 2019</div>
                                Spending Alert: We've noticed unusually high spending for your account.
                            </div>
                        </a>
                        <a className="dropdown-item text-center small text-gray-500"></a>
                    </div>
                </li>


                <div className="topbar-divider d-none d-sm-block"></div>
                <li className={isTopActiveMenu === "Profile" ? "nav-item dropdown no-arrow show" : "nav-item dropdown no-arrow"} onClick={() => handleActiveDropdown("Profile")}>
                    <a className="nav-link dropdown-toggle" id="userDropdown" role="button"
                        data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                        <span className="mr-2 d-none d-lg-inline text-gray-600 small">{userProfile? Object.values(userProfile)[1] : 'NA'}</span>
                        <img className="img-profile rounded-circle"
                            src="https://picsum.photos/50/50" />
                    </a>
                    <div className={isTopActiveMenu === "Profile" ? "dropdown-menu dropdown-menu-right shadow animated--grow-in show" : "dropdown-menu dropdown-menu-right shadow animated--grow-in"}
                        aria-labelledby="userDropdown">
                        <Button variant="link" className="dropdown-item">
                            <i className="fas fa-user fa-sm fa-fw mr-2 text-gray-400"></i>
                            {userProfile? Object.values(userProfile)[1]: 'NA'}
                        </Button>
                        <Button variant="link" className="dropdown-item">
                            <i className="fas fa-cogs fa-sm fa-fw mr-2 text-gray-400"></i>
                            Settings
                        </Button>
                        <Button variant="link" className="dropdown-item">
                            <i className="fas fa-list fa-sm fa-fw mr-2 text-gray-400"></i>
                            Activity Log
                        </Button>
                        <div className="dropdown-divider"></div>
                        <Button variant="link" className="dropdown-item" onClick={handleLogout}>
                            <i className="fas fa-sign-out-alt fa-sm fa-fw mr-2 text-gray-400"></i>
                            Logout
                        </Button>
                    </div>
                </li>

            </ul>

        </Nav>
    );
};

export default Topbar;
