import React, { useEffect } from "react";
import { Button, Nav, NavLink } from "react-bootstrap";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router";
import { RootState } from "redux/reducers";
import { sidebarToggle, activateMenu } from "redux/actions";
import { useAppDispatch } from "redux/store";
import { MenuItems } from "./MenuItems";
const Sidebar: React.FC = () => {
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const handleActiveMenu = (menuName: string) => {
        dispatch(activateMenu(menuName))
    }
    const isSidebar = useSelector((state: RootState) => state.template.sidebar);
    const isActiveMenu = useSelector((state: RootState) => state.template.activeMenu);
    const handleMenuClick = (path) => {
        dispatch(activateMenu(path))
        navigate(path, { replace: true });
    }
    const handleSubMenuClick = (path) => {
        navigate(path, { replace: true });
    }
    useEffect(() => {
        document.body.classList.toggle('sidebar-toggled', isSidebar);
    }, [isSidebar])
    return (
        <ul className={isSidebar ? 'navbar-nav bg-gradient-primary sidebar sidebar-dark accordion toggled' : 'navbar-nav bg-gradient-primary sidebar sidebar-dark accordion'} id="accordionSidebar">
            <a className="sidebar-brand d-flex align-items-center justify-content-center">
                <div className="sidebar-brand-icon rotate-n-15">
                    <i className="fas fa-laugh-wink"></i>
                </div>
                <div className="sidebar-brand-text mx-3">{projectName} <sup>2</sup></div>
            </a>
            
            <hr className="sidebar-divider d-none d-md-block" />
            {MenuItems.map((item, i) => {
               return item.subMenu && item.subMenu.length > 0 ?
                    <li className="nav-item" key={`Menu-${i}`}>
                        <NavLink className={isActiveMenu === item.title ? '' : 'collapsed'} data-toggle="collapse"
                            aria-expanded={isActiveMenu === item.title ? true : false} onClick={() => handleActiveMenu(item.title)}>
                            <i className={item.icon}></i>
                            <span>{item.title}</span></NavLink>
                        <div id="collapseTwo" className={isActiveMenu === item.title ? "collapse show" : "collapse"} aria-labelledby="headingTwo" data-parent="#accordionSidebar">
                            <div className="bg-white py-2 collapse-inner rounded">
                                <h6 className="collapse-header">{item.title}:</h6>
                                {
                                    item.subMenu.map((sub, k) => {
                                        return <Button key={`SubMenu-${k}`} variant="link" className="collapse-item pt-0" onClick={() => handleSubMenuClick(sub.path)}>
                                            <i className={sub.icon}></i><span>{sub.title}</span></Button>
                                    })
                                }
                            </div>
                        </div>
						 <hr className="sidebar-divider" />
                    </li>

                    :
                    <li className={isActiveMenu === item.path ? "nav-item active" : "nav-item"} key={`Menu-${i}`} >
                        <Button variant="link" className="nav-link pt-0" onClick={() => handleMenuClick(item.path)}>
                            <i className={item.icon}></i>
                            <span>{item.title}</span></Button>
							<hr className="sidebar-divider" />
                    </li>
            }

            )}

            <div className="text-center d-none d-md-inline">
                <button className="rounded-circle border-0" id="sidebarToggle" onClick={() => dispatch(sidebarToggle())}></button>
            </div>

        </ul>
    );
};

export default Sidebar;

