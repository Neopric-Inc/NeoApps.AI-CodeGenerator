// now new Layout would look like 
// {
//     "layout" : {
//         "0" : [

//         ],
//         "1" : [

//         ]
//       }
//   }

// Before it was like this
// {
// "layout": [
//     {},
//     {},
//     {}
// ]
// }


export const Definition = {
    "layout": [
    ],
    "components": {
    }
}

export const LayoutClipBoard = {
    "layout": {
        "0": [
            {
                "type": "row",
                "id": "54Ip2cbIkT",
                "children": [
                    {
                        "type": "column",
                        "id": "pGISY11GTK",
                        "children": [
                            {
                                "id": "cJovOIde_",
                                "type": "component"
                            }
                        ]
                    }
                ]
            }
        ],
        "1": [],
        "2": [
            {
                "type": "row",
                "id": "RL5yJRrBjQ",
                "children": [
                    {
                        "type": "column",
                        "id": "SlCppHdwHO",
                        "children": [
                            {
                                "id": "_iXAP2K7T",
                                "type": "component"
                            }
                        ]
                    }
                ]
            }
        ]
    },
    "pages": [
        {
            "id": 0,
            "name": "Main Page",
            "active": true
        },
        {
            "id": 1,
            "name": "page 2",
            "active": false
        },
        {
            "id": 2,
            "name": "page 3",
            "active": false
        }
    ],
    "components": {
        "cJovOIde_": {
            "id": "cJovOIde_",
            "type": "SyncFusion GRID",
            "icon": {
                "type": {
                    "type": {},
                    "compare": null
                },
                "key": null,
                "ref": null,
                "props": {
                    "className": "dnd sidebarIcon"
                },
                "_owner": null,
                "_store": {}
            },
            "component_name": "Grid",
            "icon_name": "GridViewIcon"
        },
        "_iXAP2K7T": {
            "id": "_iXAP2K7T",
            "type": "List",
            "content": {
                "key": null,
                "ref": null,
                "props": {},
                "_owner": null,
                "_store": {}
            },
            "icon": {
                "type": {
                    "type": {},
                    "compare": null
                },
                "key": null,
                "ref": null,
                "props": {
                    "className": "dnd sidebarIcon"
                },
                "_owner": null,
                "_store": {}
            },
            "component_name": "NestedList",
            "icon_name": "ListIcon"
        }
    }
}

