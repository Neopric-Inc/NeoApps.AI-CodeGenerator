import React, { useEffect, useState } from 'react';
import { AccordionComponent } from '@syncfusion/ej2-react-navigations';

const Data = [
    { header: 'Header 1', content: 'Content 1' },
    { header: 'Header 2', content: 'Content 2' },
    { header: 'Header 3', content: 'Content 3' },
];

export const Accordian = () => {
    const [data, setData] = useState([]);
    const [res, setRes] = useState([]);
    const [accordionData, setAccordionData] = useState(Data);
    const result = [];
    
    function getItemTemplate(props) {
        return (
            <div>
                <div className="content">{props.content}</div>
            </div>
        );
    }

    return (
        <div>
            <h1>Accordian</h1>
            <AccordionComponent dataSource={res} itemTemplate={getItemTemplate} expandMode='Single' width='250px' >
            </AccordionComponent >
            <hr />
        </div>

    );
}


// import { AccordionComponent, AccordionItemDirective, AccordionItemsDirective } from '@syncfusion/ej2-react-navigations';
// import { useEffect, useState } from 'react';
// import { getAllBackend_Stacks } from 'services/backend_stacksService';

// export const Accordian = () => {
//     const data = [
//         { header: 'Header 1', content: "Content 1" },
//         { header: 'Header 2', content: "Content 2" },
//         { header: 'Header 3', content: "Content 3" },
//     ];

//     // const [data, setData] = useState([]);
//     // useEffect(() => {
//     //     getAllBackend_Stacks(1, 100).then(
//     //         data => {
//     //             setData(data.data.document.records);
//     //            //console.log(data.data.document.records);
//     //         }
//     //     )
//     // }, [])
//     const content0: string = "Microsoft ASP.NET"


//     // function content0() {
//     //     return <div>
//     //         Microsoft ASP.NET is a set of technologies in the Microsoft .NET Framework for building Web applications and XML Web services.
//     //     </div>;
//     // }
//     function content1() {
//         return <div>
//             The Model-View-Controller (MVC) architectural pattern separates an application into three main components: the model, the view, and the controller.
//         </div>;
//     }
//     function content2() {
//         return <div>
//             JavaScript (JS) is an interpreted computer programming language.It was originally implemented as part of web browsers so that client-side scripts could interact with the user, control the browser, communicate asynchronously, and alter the document content that was displayed.
//         </div>;
//     }
//     return (
//         <AccordionComponent dataSource={data}>
//             {/* <AccordionItemsDirective>
//                 <AccordionItemDirective expanded={true} header='ASP.NET' content={content0} />
//                 <AccordionItemDirective header='ASP.NET MVC' content={content1} />
//                 <AccordionItemDirective header='JavaScript' content={content2} />
//             </AccordionItemsDirective> */}

//             <AccordionItemsDirective>
//                 {data.map((column, index) => (
//                     < AccordionItemDirective key={index} header={column.header} content={content0} />
//                 ))}
//             </AccordionItemsDirective>
//         </AccordionComponent>
//     );
// }


// import { AccordionComponent, AccordionItemDirective, AccordionItemsDirective } from '@syncfusion/ej2-react-navigations';
// import * as React from 'react';
// import * as ReactDOM from "react-dom";

// export const Accordian = () => {
//     const content0: string = "Microsoft ASP.NET"
//     // function content0() {
//     //     return (<div>
//     //         Microsoft ASP.NET is a set of technologies in the Microsoft .NET Framework for building Web applications and XML Web services.
//     //     </div>);
//     // }
//     function content1() {
//         return <div>
//             The Model-View-Controller (MVC) architectural pattern separates an application into three main components: the model, the view, and the controller.
//         </div>;
//     }
//     function content2() {
//         return (<div>
//             JavaScript (JS) is an interpreted computer programming language.It was originally implemented as part of web browsers so that client-side scripts could interact with the user, control the browser, communicate asynchronously, and alter the document content that was displayed.
//         </div>);
//     }
//     return (
//         <AccordionComponent>
//             <AccordionItemsDirective>
//                 <AccordionItemDirective header='ASP.NET' content={content0} />
//                 <AccordionItemDirective header='ASP.NET MVC' content={content1} />
//                 <AccordionItemDirective header='JavaScript' content={content2} />
//             </AccordionItemsDirective>
//         </AccordionComponent>
//     );
// }