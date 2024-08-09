Object.defineProperty(exports, "__esModule", { value: true });
exports.employees=[
    { 
        "Name": "Andrew Fuller", 
        "EmployeeID": "7", 
        "Designation": "Team Lead", 
        "Country": "England" 
    },
    { 
        "Name": "Anne Dodsworth", 
        "EmployeeID": "1", 
        "Designation": "Developer", 
        "Country": "USA" 
    },
    { 
        "Name": "Janet Leverling", 
        "EmployeeID": "3", 
        "Designation": "HR", 
        "Country": "USA" 
    },
    { 
        "Name": "Laura Callahan", 
        "EmployeeID": "2", 
        "Designation": "Product Manager", 
        "Country": "USA" 
    },
    { 
        "Name": "Margaret Peacock", 
        "EmployeeID": "6", 
        "Designation": "Developer", 
        "Country": "USA" 
    },
    { 
        "Name": "Michael Suyama", 
        "EmployeeID": "9", 
        "Designation": "Team Lead", 
        "Country": "USA" 
    },
    { 
        "Name": "Nancy Davolio", 
        "EmployeeID": "4", 
        "Designation": "Product Manager", 
        "Country": "USA" 
    },
    { 
        "Name": "Robert King", 
        "EmployeeID": "8", 
        "Designation": "Developer ", 
        "Country": "England" 
    },
    { 
        "Name": "Steven Buchanan", 
        "EmployeeID": "10", 
        "Designation": "CEO", 
        "Country": "England" 
    }
]

exports.employees1=[];
exports.socialMedia =[
    { "Class": "facebook", "SocialMedia": "Facebook", "Id": "media1" },
    { "Class": "google-plus", "SocialMedia": "Google Plus", "Id": "media2" },
    { "Class": "instagram", "SocialMedia": "Instagram", "Id": "media3" },
    { "Class": "linkedin", "SocialMedia": "LinkedIn", "Id": "media4" },
    { "Class": "skype", "SocialMedia": "Skype", "Id": "media5" },
    { "Class": "tumblr", "SocialMedia": "Tumblr", "Id": "media6" },
    { "Class": "twitter", "SocialMedia": "Twitter", "Id": "media7" },
    { "Class": "vimeo", "SocialMedia": "Vimeo", "Id": "media8" },
    { "Class": "whatsapp", "SocialMedia": "WhatsApp", "Id": "media9" },
    { "Class": "youtube", "SocialMedia": "YouTube", "Id": "media10" }
];

exports.productDetails = [{
    ID: 1,
    Title: 'San Francisco',
    Content: 'San Francisco, officially the City and County of San Francisco, is a cultural, commercial, and financial center in the U.S. state of California. Located in Northern California, San Francisco is the 17th most populous city proper in the United States, and the fourth most populous in California.',
    ImgPath: 'https://ej2.syncfusion.com/react/demos/src/carousel/images/san-francisco.jpg',
    URL: 'https://en.wikipedia.org/wiki/San_Francisco'
  }, {
    ID: 2,
    Title: 'London',
    Content: 'London, the capital of England and the United Kingdom, is a 21st-century city with history stretching back to Roman times. At its centre stand the imposing Houses of Parliament, the iconic ‘Big Ben’ clock tower and Westminster Abbey, site of British monarch coronations.',
    ImgPath: 'https://ej2.syncfusion.com/react/demos/src/carousel/images/london.jpg',
    URL: 'https://en.wikipedia.org/wiki/London'
  }, {
    ID: 3,
    Title: 'Tokyo',
    Content: 'Tokyo, Japan’s busy capital, mixes the ultramodern and the traditional, from neon-lit skyscrapers to historic temples. The opulent Meiji Shinto Shrine is known for its towering gate and surrounding woods. The Imperial Palace sits amid large public gardens.',
    ImgPath: 'https://ej2.syncfusion.com/react/demos/src/carousel/images/tokyo.jpg',
    URL: 'https://en.wikipedia.org/wiki/Tokyo'
  }, {
    ID: 4,
    Title: 'Moscow',
    Content: 'Moscow, on the Moskva River in western Russia, is the nation’s cosmopolitan capital. In its historic core is the Kremlin, a complex that’s home to the president and tsarist treasures in the Armoury. Outside its walls is Red Square, Russia`s symbolic center.',
    ImgPath: 'https://ej2.syncfusion.com/react/demos/src/carousel/images/moscow.jpg',
    URL: 'https://en.wikipedia.org/wiki/Moscow'
  }]; 

  exports.summaryData = [
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
            { taskID: 2, taskName: 'Plan timeline', startDate: new Date('02/03/2017'),
                endDate: new Date('02/07/2017'), duration: 5, progress: 100, priority: 'Normal', approved: false },
            { taskID: 3, taskName: 'Plan budget', startDate: new Date('02/03/2017'),
                endDate: new Date('02/07/2017'), duration: 5, progress: 100, priority: 'Low', approved: true },
            { taskID: 4, taskName: 'Allocate resources', startDate: new Date('02/03/2017'),
                endDate: new Date('02/07/2017'), duration: 5, progress: 100, priority: 'Critical', approved: false },
            { taskID: 5, taskName: 'Planning complete', startDate: new Date('02/07/2017'),
                endDate: new Date('02/07/2017'), duration: 0, progress: 0, priority: 'Low', approved: true }
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
            { taskID: 7, taskName: 'Software Specification', startDate: new Date('02/10/2017'),
                endDate: new Date('02/12/2017'), duration: 3, progress: 60, priority: 'Normal', approved: false },
            { taskID: 8, taskName: 'Develop prototype', startDate: new Date('02/10/2017'),
                endDate: new Date('02/12/2017'), duration: 3, progress: 100, priority: 'Critical', approved: false },
            { taskID: 9, taskName: 'Get approval from customer', startDate: new Date('02/13/2017'),
                endDate: new Date('02/14/2017'), duration: 2, progress: 100, priority: 'Low', approved: true },
            { taskID: 10, taskName: 'Design Documentation', startDate: new Date('02/13/2017'),
                endDate: new Date('02/14/2017'), duration: 2, progress: 100, priority: 'High', approved: true },
            { taskID: 11, taskName: 'Design complete', startDate: new Date('02/14/2017'),
                endDate: new Date('02/14/2017'), duration: 0, progress: 0, priority: 'Normal', approved: true }
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
                            { taskID: 15, taskName: 'Development Task 1', startDate: new Date('02/17/2017'),
                                endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'High', approved: false },
                            { taskID: 16, taskName: 'Development Task 2', startDate: new Date('02/17/2017'),
                                endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'Low', approved: true },
                            { taskID: 17, taskName: 'Testing', startDate: new Date('02/20/2017'),
                                endDate: new Date('02/21/2017'), duration: 2, progress: '0', priority: 'Normal', approved: true },
                            { taskID: 18, taskName: 'Bug fix', startDate: new Date('02/24/2017'),
                                endDate: new Date('02/25/2017'), duration: 2, progress: '0', priority: 'Critical', approved: false },
                            { taskID: 19, taskName: 'Customer review meeting', startDate: new Date('02/26/2017'),
                                endDate: new Date('02/27/2017'), duration: 2, progress: '0', priority: 'High', approved: false },
                            { taskID: 20, taskName: 'Phase 1 complete', startDate: new Date('02/27/2017'),
                                endDate: new Date('02/27/2017'), duration: 0, progress: '50', priority: 'Low', approved: true }
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
                            { taskID: 23, taskName: 'Development Task 1', startDate: new Date('02/17/2017'),
                                endDate: new Date('02/20/2017'), duration: 4, progress: '50', priority: 'Normal', approved: true },
                            { taskID: 24, taskName: 'Development Task 2', startDate: new Date('02/17/2017'),
                                endDate: new Date('02/20/2017'), duration: 4, progress: '50', priority: 'Critical', approved: true },
                            { taskID: 25, taskName: 'Testing', startDate: new Date('02/21/2017'),
                                endDate: new Date('02/24/2017'), duration: 2, progress: '0', priority: 'High', approved: false },
                            { taskID: 26, taskName: 'Bug fix', startDate: new Date('02/25/2017'),
                                endDate: new Date('02/26/2017'), duration: 2, progress: '0', priority: 'Low', approved: false },
                            { taskID: 27, taskName: 'Customer review meeting', startDate: new Date('02/27/2017'),
                                endDate: new Date('02/28/2017'), duration: 2, progress: '0', priority: 'Critical', approved: true },
                            { taskID: 28, taskName: 'Phase 2 complete', startDate: new Date('02/28/2017'),
                                endDate: new Date('02/28/2017'), duration: 0, progress: '50', priority: 'Normal', approved: false }
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
                            { taskID: 31, taskName: 'Development Task 1', startDate: new Date('02/17/2017'),
                                endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'Low', approved: true },
                            { taskID: 32, taskName: 'Development Task 2', startDate: new Date('02/17/2017'),
                                endDate: new Date('02/19/2017'), duration: 3, progress: '50', priority: 'Normal', approved: false },
                            { taskID: 33, taskName: 'Testing', startDate: new Date('02/20/2017'),
                                endDate: new Date('02/21/2017'), duration: 2, progress: '0', priority: 'Critical', approved: true },
                            { taskID: 34, taskName: 'Bug fix', startDate: new Date('02/24/2017'),
                                endDate: new Date('02/25/2017'), duration: 2, progress: '0', priority: 'High', approved: false },
                            { taskID: 35, taskName: 'Customer review meeting', startDate: new Date('02/26/2017'),
                                endDate: new Date('02/27/2017'), duration: 2, progress: '0', priority: 'Normal', approved: true },
                            { taskID: 36, taskName: 'Phase 3 complete', startDate: new Date('02/27/2017'),
                                endDate: new Date('02/27/2017'), duration: 0, progress: '50', priority: 'Critical', approved: false },
                        ]
                    }]
            }
        ]
    }
];

exports.kanbanDatasource = [
    {
      "Id": "Task 1",
      "Title": "Task - 29001",
      "Status": "Open",
      "Summary": "Analyze the new requirements gathered from the customer.",
      "Type": "Story",
      "Priority": "Low",
      "Estimate": 3.5,
      "Assignee": "Nancy Davloio"
  },
  {
      "Id": "Task 2",
      "Title": "Task - 29002",
      "Status": "In Progress",
      "Summary": "Improve application performance",
      "Type": "Improvement",
      "Priority": "Normal",
      "Estimate": 6,
      "Assignee": "Andrew Fuller"
  },
  {
      "Id": "Task 3",
      "Title": "Task - 29003",
      "Status": "Open",
      "Summary": "Arrange a web meeting with the customer to get new requirements.",
      "Type": "Others",
      "Priority": "Critical",
      "Estimate": 5.5,
      "Assignee": "Janet Leverling"
  },
  {
      "Id": "Task 4",
      "Title": "Task - 29004",
      "Status": "In Progress",
      "Summary": "Fix the issues reported in the IE browser.",
      "Type": "Bug",
      "Priority": "Critical",
      "Estimate": 2.5,
      "Assignee": "Janet Leverling"
  },
  {
      "Id": "Task 5",
      "Title": "Task - 29005",
      "Status": "Review",
      "Summary": "Fix the issues reported by the customer.",
      "Type": "Bug",
      "Priority": "Low",
      "Estimate": "3.5",
      "Assignee": "Steven walker"
  },
  {
      "Id": "Task 6",
      "Title": "Task - 29007",
      "Status": "Testing",
      "Summary": "Testing new requirements",
      "Type": "Improvement",
      "Priority": "Low",
      "Estimate": 1.5,
      "Assignee": "Robert King"
  },
  {
      "Id": "Task 7",
      "Title": "Task - 29009",
      "Status": "Review",
      "Summary": "Fix the issues reported in Safari browser.",
      "Type": "Bug",
      "Priority": "Critical",
      "Estimate": 1.5,
      "Assignee": "Nancy Davloio"
  },
  {
      "Id": "Task 8",
      "Title": "Task - 29010",
      "Status": "Close",
      "Summary": "Test the application in the IE browser.",
      "Type": "Story",
      "Priority": "Low",
      "Estimate": 5.5,
      "Assignee": "Margaret hamilt"
  },
  {
      "Id": "Task 9",
      "Title": "Task - 29011",
      "Status": "Testing",
      "Summary": "Testing the issues reported by the customer.",
      "Type": "Story",
      "Priority": "High",
      "Estimate": 1,
      "Assignee": "Steven walker"
  },
  {
      "Id": "Task 10",
      "Title": "Task - 29015",
      "Status": "Open",
      "Summary": "Show the retrieved data from the server in grid control.",
      "Type": "Story",
      "Priority": "High",
      "Estimate": 5.5,
      "Assignee": "Margaret hamilt"
  },
  {
      "Id": "Task 11",
      "Title": "Task - 29016",
      "Status": "In Progress",
      "Summary": "Fix cannot open user’s default database SQL error.",
      "Priority": "Critical",
      "Type": "Bug",
      "Estimate": 2.5,
      "Assignee": "Janet Leverling"
  },
  {
      "Id": "Task 12",
      "Title": "Task - 29017",
      "Status": "Review",
      "Summary": "Fix the issues reported in data binding.",
      "Type": "Story",
      "Priority": "Normal",
      "Estimate": "3.5",
      "Assignee": "Janet Leverling"
  },
  {
      "Id": "Task 13",
      "Title": "Task - 29018",
      "Status": "Close",
      "Summary": "Analyze SQL server 2008 connection.",
      "Type": "Story",
      "Priority": "Critical",
      "Estimate": 2,
      "Assignee": "Andrew Fuller"
  },
  {
      "Id": "Task 14",
      "Title": "Task - 29019",
      "Status": "Testing",
      "Summary": "Testing databinding issues.",
      "Type": "Story",
      "Priority": "Low",
      "Estimate": 1.5,
      "Assignee": "Margaret hamilt"
  },
  {
      "Id": "Task 15",
      "Title": "Task - 29020",
      "Status": "Close",
      "Summary": "Analyze grid control.",
      "Type": "Story",
      "Priority": "High",
      "Estimate": 2.5,
      "Assignee": "Margaret hamilt"
  },
  {
      "Id": "Task 16",
      "Title": "Task - 29021",
      "Status": "Close",
      "Summary": "Stored procedure for initial data binding of the grid.",
      "Type": "Others",
      "Priority": "Critical",
      "Estimate": 1.5,
      "Assignee": "Steven walker"
  },
  {
      "Id": "Task 17",
      "Title": "Task - 29022",
      "Status": "Close",
      "Summary": "Analyze stored procedures.",
      "Type": "Story",
      "Priority": "Critical",
      "Estimate": 5.5,
      "Assignee": "Janet Leverling"
  },
  {
      "Id": "Task 18",
      "Title": "Task - 29023",
      "Status": "Testing",
      "Summary": "Testing editing issues.",
      "Type": "Story",
      "Priority": "Critical",
      "Estimate": 1,
      "Assignee": "Nancy Davloio"
  },
  {
      "Id": "Task 19",
      "Title": "Task - 29024",
      "Status": "Review",
      "Summary": "Test editing functionality.",
      "Type": "Story",
      "Priority": "Normal",
      "Estimate": 0.5,
      "Assignee": "Nancy Davloio"
  },
  {
      "Id": "Task 20",
      "Title": "Task - 29025",
      "Status": "Open",
      "Summary": "Enhance editing functionality.",
      "Type": "Improvement",
      "Priority": "Low",
      "Estimate": 3.5,
      "Assignee": "Andrew Fuller"
  }
  ];