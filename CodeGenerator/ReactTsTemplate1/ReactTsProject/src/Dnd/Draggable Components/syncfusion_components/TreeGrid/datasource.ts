
export let sortData: Object[] = [
    {
        orderID: '1',
        orderName: 'Order 1',
        orderDate: new Date('02/03/2017'),
        shippedDate: new Date('02/09/2017'),
        units: '1395',
        unitPrice: '47.42',
        price: 134,
        Category: 'Seafoods',
        subtasks: [
            {
                orderID: '1.1', orderName: 'Mackerel', Category: 'Frozen seafood', units: '235',
                orderDate: new Date('03/03/2017'), shippedDate: new Date('03/10/2017'), unitPrice: '12.35', price: 28
            },
            {
                orderID: '1.2', orderName: 'Yellowfin Tuna', Category: 'Frozen seafood', units: '324',
                orderDate: new Date('04/05/2017'), shippedDate: new Date('04/12/2017'), unitPrice: '18.56', price: 25
            },
            {
                orderID: '1.3', orderName: 'Herrings', Category: 'Frozen seafood', units: '488',
                orderDate: new Date('05/08/2017'), shippedDate: new Date('05/15/2017'), unitPrice: '11.45', price: 52
            },
            {
                orderID: '1.4', orderName: 'Preserved Olives', Category: 'Edible', units: '125',
                orderDate: new Date('06/10/2017'), shippedDate: new Date('06/17/2017'), unitPrice: '19.26', price: 11
            },
            {
                orderID: '1.5', orderName: 'Sweet corn Frozen', Category: 'Edible', units: '223',
                orderDate: new Date('07/12/2017'), shippedDate: new Date('07/19/2019'), unitPrice: '17.54', price: 15
            }
        ]
    },
    {
        orderID: '2',
        orderName: 'Order 2',
        orderDate: new Date('01/10/2018'),
        shippedDate: new Date('01/16/2018'),
        units: '1944',
        unitPrice: '58.45',
        price: 212,
        Category: 'products',
        subtasks: [
            {
                orderID: '2.1', orderName: 'Tilapias', Category: 'Frozen seafood',
                orderDate: new Date('02/05/2018'), shippedDate: new Date('02/12/2018'), units: '278', unitPrice: '15.26', price: 41
            },
            {
                orderID: '2.2', orderName: 'White Shrimp', Category: 'Frozen seafood', units: '560',
                orderDate: new Date('05/22/2018'), shippedDate: new Date('05/29/2018'), unitPrice: '17.26', price: 39
            },
            {
                orderID: '2.3', orderName: 'Fresh Cheese', Category: 'Dairy', units: '323', unitPrice: '12.67',
                orderDate: new Date('06/08/2018'), shippedDate: new Date('06/15/2018'), price: 38
            },
            {
                orderID: '2.4', orderName: 'Blue Veined Cheese', Category: 'Dairy', units: '370', unitPrice: '15.25',
                orderDate: new Date('07/10/2018'), shippedDate: new Date('07/17/2018'), price: 55
            },
            {
                orderID: '2.5', orderName: 'Butter', Category: 'Dairy', units: '413', unitPrice: '19.25',
                orderDate: new Date('09/18/2018'), shippedDate: new Date('09/25/2018'), price: 37.17
            }
        ]
    },
    {
        orderID: '3',
        orderName: 'Order 3',
        orderDate: new Date('09/10/2018'),
        shippedDate: new Date('09/20/2018'),
        units: '1120',
        unitPrice: '33.45',
        price: 109,
        Category: 'Crystals',
        subtasks: [
            {
                orderID: '3.1', orderName: 'Lead glassware', Category: 'Solid crystals',
                orderDate: new Date('02/07/2018'), shippedDate: new Date('02/14/2018'), units: '542', unitPrice: '16.45', price: 32
            },
            {
                orderID: '3.2', orderName: 'Pharmaceutical Glassware', Category: 'Solid crystals',
                orderDate: new Date('04/19/2018'), shippedDate: new Date('04/26/2018'), units: '324', unitPrice: '11.45', price: 35
            },
            {
                orderID: '3.3', orderName: 'Glass beads', Category: 'Solid crystals', units: '254',
                orderDate: new Date('05/22/2018'), shippedDate: new Date('03/22/2018'), unitPrice: '16.23', price: 40
            }
        ]
    }

];
export let sampleData: Object[] = [
    {
        taskID: 1,
        taskName: 'Planning',
        startDate: new Date('02/03/2017'),
        endDate: new Date('02/07/2017'),
        progress: 100,
        duration: 5,
        priority: 'Normal',
        approved: false,
        subtasks: [
            {
                taskID: 2, taskName: 'Plan timeline', startDate: new Date('02/03/2017'),
                endDate: new Date('02/07/2017'), duration: 5, progress: 100, priority: 'Normal', approved: false
            },
            {
                taskID: 3, taskName: 'Plan budget', startDate: new Date('02/03/2017'),
                endDate: new Date('02/07/2017'), duration: 5, progress: 100, priority: 'Low', approved: true
            },
            {
                taskID: 4, taskName: 'Allocate resources', startDate: new Date('02/03/2017'),
                endDate: new Date('02/07/2017'), duration: 5, progress: 100, priority: 'Critical', approved: false
            },
            {
                taskID: 5, taskName: 'Planning complete', startDate: new Date('02/07/2017'),
                endDate: new Date('02/07/2017'), duration: 0, progress: 0, priority: 'Low', approved: true
            }
        ]
    },
    {
        taskID: 6,
        taskName: 'Design',
        startDate: new Date('02/10/2017'),
        endDate: new Date('02/14/2017'),
        duration: 3,
        progress: 86,
        priority: 'High',
        approved: false,
        subtasks: [
            {
                taskID: 7, taskName: 'Software Specification', startDate: new Date('02/10/2017'),
                endDate: new Date('02/12/2017'), duration: 3, progress: 60, priority: 'Normal', approved: false
            },
            {
                taskID: 8, taskName: 'Develop prototype', startDate: new Date('02/10/2017'),
                endDate: new Date('02/12/2017'), duration: 3, progress: 100, priority: 'Critical', approved: false
            },
            {
                taskID: 9, taskName: 'Get approval from customer', startDate: new Date('02/13/2017'),
                endDate: new Date('02/14/2017'), duration: 2, progress: 100, priority: 'Low', approved: true
            },
            {
                taskID: 10, taskName: 'Design Documentation', startDate: new Date('02/13/2017'),
                endDate: new Date('02/14/2017'), duration: 2, progress: 100, priority: 'High', approved: true
            },
            {
                taskID: 11, taskName: 'Design complete', startDate: new Date('02/14/2017'),
                endDate: new Date('02/14/2017'), duration: 0, progress: 0, priority: 'Normal', approved: true
            }
        ]
    },
    {
        taskID: 12,
        taskName: 'Implementation Phase',
        startDate: new Date('02/17/2017'),
        endDate: new Date('02/27/2017'),
        priority: 'Normal',
        approved: false,
        duration: 11,
        progress: 66,
        subtasks: [
            {
                taskID: 13,
                taskName: 'Phase 1',
                startDate: new Date('02/17/2017'),
                endDate: new Date('02/27/2017'),
                priority: 'High',
                approved: false,
                progress: 50,
                duration: 11,
                subtasks: [{
                    taskID: 14,
                    taskName: 'Implementation Module 1',
                    startDate: new Date('02/17/2017'),
                    endDate: new Date('02/27/2017'),
                    priority: 'Normal',
                    duration: 11,
                    progress: 10,
                    approved: false,
                    subtasks: [
                        {
                            taskID: 15, taskName: 'Development Task 1', startDate: new Date('02/17/2017'),
                            endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'High', approved: false
                        },
                        {
                            taskID: 16, taskName: 'Development Task 2', startDate: new Date('02/17/2017'),
                            endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'Low', approved: true
                        },
                        {
                            taskID: 17, taskName: 'Testing', startDate: new Date('02/20/2017'),
                            endDate: new Date('02/21/2017'), duration: 2, progress: '0', priority: 'Normal', approved: true
                        },
                        {
                            taskID: 18, taskName: 'Bug fix', startDate: new Date('02/24/2017'),
                            endDate: new Date('02/25/2017'), duration: 2, progress: '0', priority: 'Critical', approved: false
                        },
                        {
                            taskID: 19, taskName: 'Customer review meeting', startDate: new Date('02/26/2017'),
                            endDate: new Date('02/27/2017'), duration: 2, progress: '0', priority: 'High', approved: false
                        },
                        {
                            taskID: 20, taskName: 'Phase 1 complete', startDate: new Date('02/27/2017'),
                            endDate: new Date('02/27/2017'), duration: 0, progress: '50', priority: 'Low', approved: true
                        }

                    ]
                }]
            },
            {
                taskID: 21,
                taskName: 'Phase 2',
                startDate: new Date('02/17/2017'),
                endDate: new Date('02/28/2017'),
                priority: 'High',
                approved: false,
                duration: 12,
                progress: 60,
                subtasks: [{
                    taskID: 22,
                    taskName: 'Implementation Module 2',
                    startDate: new Date('02/17/2017'),
                    endDate: new Date('02/28/2017'),
                    priority: 'Critical',
                    approved: false,
                    duration: 12,
                    progress: 90,
                    subtasks: [
                        {
                            taskID: 23, taskName: 'Development Task 1', startDate: new Date('02/17/2017'),
                            endDate: new Date('02/20/2017'), duration: 4, progress: '50', priority: 'Normal', approved: true
                        },
                        {
                            taskID: 24, taskName: 'Development Task 2', startDate: new Date('02/17/2017'),
                            endDate: new Date('02/20/2017'), duration: 4, progress: '50', priority: 'Critical', approved: true
                        },
                        {
                            taskID: 25, taskName: 'Testing', startDate: new Date('02/21/2017'),
                            endDate: new Date('02/24/2017'), duration: 2, progress: '0', priority: 'High', approved: false
                        },
                        {
                            taskID: 26, taskName: 'Bug fix', startDate: new Date('02/25/2017'),
                            endDate: new Date('02/26/2017'), duration: 2, progress: '0', priority: 'Low', approved: false
                        },
                        {
                            taskID: 27, taskName: 'Customer review meeting', startDate: new Date('02/27/2017'),
                            endDate: new Date('02/28/2017'), duration: 2, progress: '0', priority: 'Critical', approved: true
                        },
                        {
                            taskID: 28, taskName: 'Phase 2 complete', startDate: new Date('02/28/2017'),
                            endDate: new Date('02/28/2017'), duration: 0, progress: '50', priority: 'Normal', approved: false
                        }

                    ]
                }]
            },

            {
                taskID: 29,
                taskName: 'Phase 3',
                startDate: new Date('02/17/2017'),
                endDate: new Date('02/27/2017'),
                priority: 'Normal',
                approved: false,
                duration: 11,
                progress: 30,
                subtasks: [{
                    taskID: 30,
                    taskName: 'Implementation Module 3',
                    startDate: new Date('02/17/2017'),
                    endDate: new Date('02/27/2017'),
                    priority: 'High',
                    approved: false,
                    duration: 11,
                    progress: 60,
                    subtasks: [
                        {
                            taskID: 31, taskName: 'Development Task 1', startDate: new Date('02/17/2017'),
                            endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'Low', approved: true
                        },
                        {
                            taskID: 32, taskName: 'Development Task 2', startDate: new Date('02/17/2017'),
                            endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'Normal', approved: false
                        },
                        {
                            taskID: 33, taskName: 'Testing', startDate: new Date('02/20/2017'),
                            endDate: new Date('02/21/2017'), duration: 2, progress: '0', priority: 'Critical', approved: true
                        },
                        {
                            taskID: 34, taskName: 'Bug fix', startDate: new Date('02/24/2017'),
                            endDate: new Date('02/25/2017'), duration: 2, progress: '0', priority: 'High', approved: false
                        },
                        {
                            taskID: 35, taskName: 'Customer review meeting', startDate: new Date('02/26/2017'),
                            endDate: new Date('02/27/2017'), duration: 2, progress: '0', priority: 'Normal', approved: true
                        },
                        {
                            taskID: 36, taskName: 'Phase 3 complete', startDate: new Date('02/27/2017'),
                            endDate: new Date('02/27/2017'), duration: 0, progress: '50', priority: 'Critical', approved: false
                        },
                    ]
                }]
            }
        ]
    }
];


export let Projects = [
    {
        "project_id": 1,
        "project_name": "car-rental",
        "tenant_id": 2,
        "environment_id": 1,
        "createdBy": "sam",
        "modifiedBy": "roma",
        "createdAt": "11/01/2023 5:07:09 pm",
        "modifiedAt": "11/01/2023 5:07:09 pm",
        "isActive": 1
    },
    {
        "project_id": 2,
        "project_name": "Webchat",
        "tenant_id": 3,
        "environment_id": 2,
        "createdBy": "hinata",
        "modifiedBy": "kaido",
        "createdAt": "11/01/2023 5:07:09 pm",
        "modifiedAt": "11/01/2023 5:07:09 pm",
        "isActive": 1
    },
    {
        "project_id": 3,
        "project_name": "stock-shastri",
        "tenant_id": 5,
        "environment_id": 4,
        "createdBy": "sam",
        "modifiedBy": "kaido",
        "createdAt": "11/01/2023 5:07:09 pm",
        "modifiedAt": "11/01/2023 5:07:09 pm",
        "isActive": 1
    },
    {
        "project_id": 4,
        "project_name": "snaptube",
        "tenant_id": 8,
        "environment_id": 3,
        "createdBy": "roma",
        "modifiedBy": "sam",
        "createdAt": "11/01/2023 5:07:09 pm",
        "modifiedAt": "11/01/2023 5:07:09 pm",
        "isActive": 1
    },
    {
        "project_id": 5,
        "project_name": "advisor",
        "tenant_id": 8,
        "environment_id": 6,
        "createdBy": "ramesh",
        "modifiedBy": "ali",
        "createdAt": "11/01/2023 5:10:11 pm",
        "modifiedAt": "11/01/2023 5:10:11 pm",
        "isActive": 1
    },
    {
        "project_id": 6,
        "project_name": "resume_checker",
        "tenant_id": 4,
        "environment_id": 6,
        "createdBy": "raj",
        "modifiedBy": "simran",
        "createdAt": "11/01/2023 5:10:11 pm",
        "modifiedAt": "11/01/2023 5:10:11 pm",
        "isActive": 1
    },
    {
        "project_id": 24,
        "project_name": "Dev3-test1",
        "tenant_id": 1,
        "environment_id": 1,
        "createdBy": "aute",
        "modifiedBy": "dolor id",
        "createdAt": "05/03/1958 3:52:22 am",
        "modifiedAt": "19/05/1984 2:04:02 pm",
        "isActive": 1
    },
    {
        "project_id": 25,
        "project_name": "Dev3-test1",
        "tenant_id": 1,
        "environment_id": 1,
        "createdBy": "aute",
        "modifiedBy": "dolor id",
        "createdAt": "05/03/1958 3:52:22 am",
        "modifiedAt": "19/05/1984 2:04:02 pm",
        "isActive": 1
    },
    {
        "project_id": 26,
        "project_name": "Dev3-test1",
        "tenant_id": 1,
        "environment_id": 1,
        "createdBy": "aute",
        "modifiedBy": "dolor id",
        "createdAt": "05/03/1958 3:52:22 am",
        "modifiedAt": "19/05/1984 2:04:02 pm",
        "isActive": 1
    },
    {
        "project_id": 27,
        "project_name": "Dev3-test2",
        "tenant_id": 1,
        "environment_id": 1,
        "createdBy": "aute",
        "modifiedBy": "dolor id",
        "createdAt": "05/03/1958 3:52:22 am",
        "modifiedAt": "19/05/1984 2:04:02 pm",
        "isActive": 1
    }
]

export let Tenents = [
    {
        "tenant_id": 1,
        "user_id": 20,
        "tenant_name": "shitij",
        "tenant_contact_info": "shitij@inc.com",
        "modifiedBy": "ben",
        "createdBy": "smith",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 2,
        "user_id": 20,
        "tenant_name": "ryuken",
        "tenant_contact_info": "ruken@drg.com",
        "modifiedBy": "lily",
        "createdBy": "ryusuke",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 3,
        "user_id": 20,
        "tenant_name": "rena",
        "tenant_contact_info": "rena@inc.com",
        "modifiedBy": "tobio",
        "createdBy": "hina",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 4,
        "user_id": 20,
        "tenant_name": "shusui",
        "tenant_contact_info": "shusui@drg.com",
        "modifiedBy": "ram",
        "createdBy": "lila",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 5,
        "user_id": 20,
        "tenant_name": "shitij",
        "tenant_contact_info": "naksha@inc.com",
        "modifiedBy": "rohit",
        "createdBy": "kalis",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 6,
        "user_id": 20,
        "tenant_name": "ryuken",
        "tenant_contact_info": "inosti@drg.com",
        "modifiedBy": "remi",
        "createdBy": "angel",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 7,
        "user_id": 20,
        "tenant_name": "rohan",
        "tenant_contact_info": "rihaan@inc.com",
        "modifiedBy": "rim",
        "createdBy": "diablo",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    },
    {
        "tenant_id": 8,
        "user_id": 20,
        "tenant_name": "ryuken",
        "tenant_contact_info": "aries@drg.com",
        "modifiedBy": "pablo",
        "createdBy": "diego",
        "modifiedAt": "11/01/2023 4:51:19 pm",
        "createdAt": "11/01/2023 4:51:19 pm",
        "isActive": 1
    }
]

