import * as React from 'react';
import { useEffect, useRef, useState } from 'react';
import { SidebarComponent } from '@syncfusion/ej2-react-navigations';
import { ButtonComponent } from '@syncfusion/ej2-react-buttons';
import { TextBoxComponent } from '@syncfusion/ej2-react-inputs';
import { registerLicense } from '@syncfusion/ej2-base';
registerLicense('Mgo+DSMBaFt/QHRqVVhlXFpHaV5LQmFJfFBmRGlcfFRzcEU3HVdTRHRcQl9iQX5Sc0VhWHdWeXA=;Mgo+DSMBPh8sVXJ0S0J+XE9BdlRBQmJAYVF2R2BJflR1d19FZEwgOX1dQl9gSXxSfkViXH9ccX1VRGQ=;ORg4AjUWIQA/Gnt2VVhkQlFac19JXnxId0x0RWFab196cVZMY1hBNQtUQF1hSn5Rd01jWHpecnVcR2ZV;MTE1MzkzNkAzMjMwMmUzNDJlMzBGTWdkM2pidVlJUk5mdTM3TDcyd3JObitGMEdObjNqT3hVTTN2aUxMWVg0PQ==;MTE1MzkzN0AzMjMwMmUzNDJlMzBtY2FVUzZnbEJSdFpzOHVHWG1ocjlsY1BkZkhINkIvL2VOd1M3dEdHbmdRPQ==;NRAiBiAaIQQuGjN/V0Z+WE9EaFpCVmBWf1ppR2NbfE5xflBFal9VVAciSV9jS31TdERrWX5bcHZUT2ddUg==;MTE1MzkzOUAzMjMwMmUzNDJlMzBneTgxVUV3ZG1MSitWaDJocjQ3am41RVBFWU81ZXJoTmVHOUw0U1dreVBnPQ==;MTE1Mzk0MEAzMjMwMmUzNDJlMzBXa3M3Vm9YZ2wwaXM4L2pnbjlrakVXNFptbi8zQkxhN3JxZHhHWlhFWWFnPQ==;Mgo+DSMBMAY9C3t2VVhkQlFac19JXnxId0x0RWFab196cVZMY1hBNQtUQF1hSn5Rd01jWHpecnZVRGRf;MTE1Mzk0MkAzMjMwMmUzNDJlMzBmNGtXcEN1YnQ1ODFNQjZpZFArV3NCd05HNm5uQXVGa3NhdmphQThITUhRPQ==;MTE1Mzk0M0AzMjMwMmUzNDJlMzBmVTF5Y0VCV2ZheHdlQzN6dVN3K3lUWDF0VURUZzVaUUdubWhYMmJCN3VJPQ==;MTE1Mzk0NEAzMjMwMmUzNDJlMzBneTgxVUV3ZG1MSitWaDJocjQ3am41RVBFWU81ZXJoTmVHOUw0U1dreVBnPQ==');

const Sidebar = ({ sidbarLinks, onLinkClick, handleLinkAdd }) => {
    let sidebarObj: SidebarComponent;
    function onCreate(): void {
        sidebarObj.element.style.visibility = '';
    }

    const [links, setLinks] = useState(sidbarLinks);
    const nameInputRef = useRef(null);

    const saveLink = () => {
        const name = nameInputRef.current.value;
        if (name) {
            handleLinkAdd([...links, { id: links.length, name: name, active: false }])
            setLinks([...links, { id: links.length, name: name, active: false }]);
            nameInputRef.current.value = '';
            // props.handleLinkAdd(links);
        }
    };

    useEffect(() => {
        setLinks(sidbarLinks);
        ////console.log("Links for Sidebar : ", links);
    })

    return (
        
        <SidebarComponent id="default-sidebar" style={{ width: "175px", backgroundColor: "#D3D3D3", justifyContent: "center" }} >
            <h3>Add Links</h3>
            <div>
                <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', margin: "auto", marginBottom: "20px" }}>
                    <TextBoxComponent ref={nameInputRef} placeholder='Enter Page Name' />
                    <ButtonComponent onClick={saveLink} cssClass='e-primary' style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', margin: "auto" }}>Add Page</ButtonComponent>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', margin: "auto" }}>
                    {links.map((link, index) => (
                        <ButtonComponent style={{ margin: "10px 20px" }}
                            key={index}
                            cssClass={link.active ? 'active' : ''}
                            onClick={() => {
                                const updatedLinks = [...links];
                                updatedLinks.forEach(l => l.active = false);
                                updatedLinks[index].active = true;
                                setLinks(updatedLinks);
                                onLinkClick(link.id);
                            }}
                        >
                            {link.name}
                        </ButtonComponent>
                    ))}
                    <hr />
                </div>
            </div>
        </SidebarComponent >
        // </div >
        // </div >
    )
}

export default Sidebar;