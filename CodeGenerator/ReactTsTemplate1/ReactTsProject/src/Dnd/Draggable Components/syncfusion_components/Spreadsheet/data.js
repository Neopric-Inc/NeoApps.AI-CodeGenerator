"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.freezePaneData = exports.GDPData = exports.conditionalFormatting = exports.orderDetails = exports.sortingAndFiltering = exports.numberFormatData = exports.protectSheetData = exports.formulaData = exports.hyperlinkStock = exports.hyperlinkCart = exports.grossPay = exports.defaultData = void 0;
exports.defaultData = [
    {
        "Customer Name": "Romona Heaslip",
        "Model": "Taurus",
        "Color": "Aquamarine",
        "Payment Mode": "Debit Card",
        "Delivery Date": "07/11/2015",
        "Amount": "8529.22"
    },
    {
        "Customer Name": "Clare Batterton",
        "Model": "Sparrow",
        "Color": "Pink",
        "Payment Mode": "Cash On Delivery",
        "Delivery Date": "7/13/2016",
        "Amount": "17866.19"
    },
    {
        "Customer Name": "Eamon Traise",
        "Model": "Grand Cherokee",
        "Color": "Blue",
        "Payment Mode": "Net Banking",
        "Delivery Date": "09/04/2015",
        "Amount": "13853.09"
    },
    {
        "Customer Name": "Julius Gorner",
        "Model": "GTO",
        "Color": "Aquamarine",
        "Payment Mode": "Credit Card",
        "Delivery Date": "12/15/2017",
        "Amount": "2338.74"
    },
    {
        "Customer Name": "Jenna Schoolfield",
        "Model": "LX",
        "Color": "Yellow",
        "Payment Mode": "Credit Card",
        "Delivery Date": "10/08/2014",
        "Amount": "9578.45"
    },
    {
        "Customer Name": "Marylynne Harring",
        "Model": "Catera",
        "Color": "Green",
        "Payment Mode": "Cash On Delivery",
        "Delivery Date": "7/01/2017",
        "Amount": "19141.62"
    },
    {
        "Customer Name": "Vilhelmina Leipelt",
        "Model": "7 Series",
        "Color": "Goldenrod",
        "Payment Mode": "Credit Card",
        "Delivery Date": "12/20/2015",
        "Amount": "6543.30"
    },
    {
        "Customer Name": "Barby Heisler",
        "Model": "Corvette",
        "Color": "Red",
        "Payment Mode": "Credit Card",
        "Delivery Date": "11/24/2014",
        "Amount": "13035.06"
    },
    {
        "Customer Name": "Karyn Boik",
        "Model": "Regal",
        "Color": "Indigo",
        "Payment Mode": "Debit Card",
        "Delivery Date": "05/12/2014",
        "Amount": "18488.80"
    },
    {
        "Customer Name": "Jeanette Pamplin",
        "Model": "S4",
        "Color": "Fuscia",
        "Payment Mode": "Net Banking",
        "Delivery Date": "12/30/2014",
        "Amount": "12317.04"
    },
    {
        "Customer Name": "Cristi Espinos",
        "Model": "TL",
        "Color": "Aquamarine",
        "Payment Mode": "Credit Card",
        "Delivery Date": "12/18/2013",
        "Amount": "6230.13"
    },
    {
        "Customer Name": "Issy Humm",
        "Model": "Club Wagon",
        "Color": "Pink",
        "Payment Mode": "Cash On Delivery",
        "Delivery Date": "02/02/2015",
        "Amount": "9709.49"
    },
    {
        "Customer Name": "Tuesday Fautly",
        "Model": "V8 Vantage",
        "Color": "Crimson",
        "Payment Mode": "Debit Card",
        "Delivery Date": "11/19/2014",
        "Amount": "9766.10"
    },
    {
        "Customer Name": "Rosemaria Thomann",
        "Model": "Caravan",
        "Color": "Violet",
        "Payment Mode": "Net Banking",
        "Delivery Date": "02/08/2014",
        "Amount": "7685.49"
    },
    {
        "Customer Name": "Lyell Fuentez",
        "Model": "Bravada",
        "Color": "Violet",
        "Payment Mode": "Debit Card",
        "Delivery Date": "08/05/2016",
        "Amount": "18012.45"
    },
    {
        "Customer Name": "Raynell Layne",
        "Model": "Colorado",
        "Color": "Pink",
        "Payment Mode": "Credit Card",
        "Delivery Date": "05/30/2016",
        "Amount": "2785.49"
    },
    {
        "Customer Name": "Raye Whines",
        "Model": "4Runner",
        "Color": "Red",
        "Payment Mode": "Debit Card",
        "Delivery Date": "12/10/2016",
        "Amount": "9967.74"
    },
    {
        "Customer Name": "Virgina Aharoni",
        "Model": "TSX",
        "Color": "Pink",
        "Payment Mode": "Cash On Delivery",
        "Delivery Date": "10/23/2014",
        "Amount": "5584.33"
    },
    {
        "Customer Name": "Peta Cheshir",
        "Model": "Pathfinder",
        "Color": "Red",
        "Payment Mode": "Net Banking",
        "Delivery Date": "12/24/2015",
        "Amount": "5286.53"
    },
    {
        "Customer Name": "Jule Urion",
        "Model": "Charger",
        "Color": "Violet",
        "Payment Mode": "Debit Card",
        "Delivery Date": "11/20/2013",
        "Amount": "13511.91"
    },
    {
        "Customer Name": "Lew Gilyatt",
        "Model": "Bonneville",
        "Color": "Crimson",
        "Payment Mode": "Credit Card",
        "Delivery Date": "11/19/2013",
        "Amount": "6498.19"
    },
    {
        "Customer Name": "Jobey Fortun",
        "Model": "B-Series",
        "Color": "Blue",
        "Payment Mode": "Net Banking",
        "Delivery Date": "10/30/2014",
        "Amount": "10359.67"
    },
    {
        "Customer Name": "Blondie Crump",
        "Model": "Voyager",
        "Color": "Turquoise",
        "Payment Mode": "Credit Card",
        "Delivery Date": "04/06/2018",
        "Amount": "8118.39"
    },
    {
        "Customer Name": "Florentia Binns",
        "Model": "Grand Prix",
        "Color": "Orange",
        "Payment Mode": "Cash On Delivery",
        "Delivery Date": "10/13/2016",
        "Amount": "10204.37"
    },
    {
        "Customer Name": "Jaquelin Galtone",
        "Model": "Sunbird",
        "Color": "Red",
        "Payment Mode": "Net Banking",
        "Delivery Date": "10/22/2013",
        "Amount": "6528.06"
    },
    {
        "Customer Name": "Hakeem Easseby",
        "Model": "Mirage",
        "Color": "Crimson",
        "Payment Mode": "Debit Card",
        "Delivery Date": "9/12/2014",
        "Amount": "5619.25"
    },
    {
        "Customer Name": "Nickolaus Gidman",
        "Model": "XK",
        "Color": "Orange",
        "Payment Mode": "Debit Card",
        "Delivery Date": "05/12/2016",
        "Amount": "5091.43"
    },
    {
        "Customer Name": "Jenine Iglesia",
        "Model": "Accord",
        "Color": "Orange",
        "Payment Mode": "Debit Card",
        "Delivery Date": "09/03/2018",
        "Amount": "14566.08"
    },
    {
        "Customer Name": "Fax Witherspoon",
        "Model": "Range Rover Sport",
        "Color": "Orange",
        "Payment Mode": "Credit Card",
        "Delivery Date": "2/22/2018",
        "Amount": "5284.87"
    }
];
exports.grossPay = [
    {
        "EMPLOYEE ID": "1001",
        "EMPLOYEE NAME": "Vin Disel",
        "DATE": "4/5/2021",
        "WEEKDAY": "Mon",
        "TIME IN": "8:00 AM",
        "TIME OUT": "10:00 PM",
        "HOURS WORKED": "14",
        "BASIC SALARY (30/hour)": "=G4*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H4 + ((G4-8)*15))"
    }, {
        "EMPLOYEE ID": "1002",
        "EMPLOYEE NAME": "Steve",
        "DATE": "4/6/2021",
        "WEEKDAY": "Tue",
        "TIME IN": "8:00 AM",
        "TIME OUT": "6:00 PM",
        "HOURS WORKED": "10",
        "BASIC SALARY (30/hour)": "=G5*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H5 + ((G5-8)*15))"
    },
    {
        "EMPLOYEE ID": "1003",
        "EMPLOYEE NAME": "Paul Waulker",
        "DATE": "4/6/2021",
        "WEEKDAY": "Tue",
        "TIME IN": "11:00 AM",
        "TIME OUT": "4:00 PM",
        "HOURS WORKED": "9",
        "BASIC SALARY (30/hour)": "=G6*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H6 + ((G6-8)*15))"
    },
    {
        "EMPLOYEE ID": "1004",
        "EMPLOYEE NAME": "John",
        "DATE": "4/8/2021",
        "WEEKDAY": "Thu",
        "TIME IN": "8:00 AM",
        "TIME OUT": "4:00 PM",
        "HOURS WORKED": "8",
        "BASIC SALARY (30/hour)": "=G7*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H7 + ((G7-8)*15))"
    },
    {
        "EMPLOYEE ID": "1005",
        "EMPLOYEE NAME": "Sam",
        "DATE": "4/9/2021",
        "WEEKDAY": "Fri",
        "TIME IN": "7:00 AM",
        "TIME OUT": "6:00 PM",
        "HOURS WORKED": "11",
        "BASIC SALARY (30/hour)": "=G8*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H8 + ((G8-8)*15))"
    },
    {
        "EMPLOYEE ID": "1006",
        "EMPLOYEE NAME": "Chistoper",
        "DATE": "4/12/2021",
        "WEEKDAY": "Mon",
        "TIME IN": "10:00 AM",
        "TIME OUT": "6:00 PM",
        "HOURS WORKED": "8",
        "BASIC SALARY (30/hour)": "=G9*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H9 + ((G9-8)*15))"
    },
    {
        "EMPLOYEE ID": "1007",
        "EMPLOYEE NAME": "Adrew",
        "DATE": "4/13/2021",
        "WEEKDAY": "Tue",
        "TIME IN": "10:00 AM",
        "TIME OUT": "7:00 PM",
        "HOURS WORKED": "9",
        "BASIC SALARY (30/hour)": "=G10*30",
        "Gross Pay With Overtime(15/hour)": "=Sum(H10 + ((G10-8)*15))"
    },
    {
        "EMPLOYEE ID": "1004",
        "EMPLOYEE NAME": "John",
        "DATE": "4/14/2021",
        "WEEKDAY": "Wed",
        "TIME IN": "8:00 AM",
        "TIME OUT": "4:00 PM",
        "HOURS WORKED": "8",
        "BASIC SALARY (30/hour)": "=G11*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H11 + ((G11-8)*15))"
    },
    {
        "EMPLOYEE ID": "1009",
        "EMPLOYEE NAME": "Bravo",
        "DATE": "4/14/2021",
        "WEEKDAY": "Wed",
        "TIME IN": "11:00 AM",
        "TIME OUT": "8:00 PM",
        "HOURS WORKED": "9",
        "BASIC SALARY (30/hour)": "=G12*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H12 + ((G12-8)*15))"
    },
    {
        "EMPLOYEE ID": "1002",
        "EMPLOYEE NAME": "Steve",
        "DATE": "4/15/2021",
        "WEEKDAY": "Thu",
        "TIME IN": "9:00 AM",
        "TIME OUT": "8:00 PM",
        "HOURS WORKED": "11",
        "BASIC SALARY (30/hour)": "=G13*30",
        "GROSS PAY with OVERTIME(15/hour)": "=Sum(H13 + ((G13-8)*15))"
    }
];
exports.hyperlinkCart = [
    {
        "Product Name": "Coffee Maker",
        "Category": "Kitchen",
        "Quantity": "43",
        "Price": "399",
        "Total": "=E4*D4"
    },
    {
        "Product Name": "Apple Pencil",
        "Category": "Electronics",
        "Quantity": "7",
        "Price": "200",
        "Total": "=E5*D5"
    },
    {
        "Product Name": "Juicer",
        "Category": "Kitchen",
        "Quantity": "12",
        "Price": "100",
        "Total": "=E6*D6"
    },
    {
        "Product Name": "Toaster",
        "Category": "Kitchen",
        "Quantity": "69",
        "Price": "183",
        "Total": "=E7*D7"
    },
    {
        "Product Name": "Tea Kettle",
        "Category": "Kitchen",
        "Quantity": "83",
        "Price": "169",
        "Total": "=E8*D8"
    },
    {
        "Product Name": "Logitech Mouse",
        "Category": "Electronics",
        "Quantity": "16",
        "Price": "250",
        "Total": "=E9*D9"
    },
    {
        "Product Name": "Skillet",
        "Category": "Kitchen",
        "Quantity": "20",
        "Price": "149",
        "Total": "=E10*D10"
    },
    {
        "Product Name": "Hamilton Blender",
        "Category": "Appliances",
        "Quantity": "68",
        "Price": "109",
        "Total": "=E11*D11"
    },
    {
        "Product Name": "Plate set",
        "Category": "Kitchen",
        "Quantity": "59",
        "Price": "168",
        "Total": "=E12*D12"
    }
];
exports.hyperlinkStock = [
    {
        "Product ID": "AG940Z",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D4",
        "Available Quantity": "=B2-C2"
    },
    {
        "Product ID": "BJ120K",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D5",
        "Available Quantity": "=B3-C3"
    },
    {
        "Product ID": "BC120M",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D6",
        "Available Quantity": "=B4-C4"
    },
    {
        "Product ID": "BS121L",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D7",
        "Available Quantity": "=B5-C5"
    },
    {
        "Product ID": "BU121K",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D8",
        "Available Quantity": "=B6-C6"
    },
    {
        "Product ID": "BD121M",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D9",
        "Available Quantity": "=B7-C7"
    },
    {
        "Product ID": "AT992X",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D10",
        "Available Quantity": "=B8-C8"
    },
    {
        "Product ID": "AP992Z",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D11",
        "Available Quantity": "=B9-C9"
    },
    {
        "Product ID": "AW920X",
        "Products Received": "100",
        "Products Sold": "=100-Cart!D12",
        "Available Quantity": "=B10-C10"
    }
];
exports.formulaData = [
    {
        "Date": "8-1-2019",
        "Open": "2625.75",
        "Close": "2640.75",
        "High (+)": "2634.45",
        "Low (-)": "2620.65",
        "Profit": "=C2-B2"
    },
    {
        "Date": "8-2-2019",
        "Open": "2640.75",
        "Close": "2638.75",
        "High (+)": "2640.75",
        "Low (-)": "2638.75",
        "Profit": "=C3-B3"
    },
    {
        "Date": "8-3-2019",
        "Open": "2638.75",
        "Close": "2697.65",
        "High (+)": "2690.25",
        "Low (-)": "2647.65",
        "Profit": "=C4-B4"
    },
    {
        "Date": "8-4-2019",
        "Open": "2697.65",
        "Close": "2700.25",
        "High (+)": "2699.21",
        "Low (-)": "2585.10",
        "Profit": "=C5-B5"
    },
    {
        "Date": "8-5-2019",
        "Open": "2700.25",
        "Close": "2730.25",
        "High (+)": "2727.65",
        "Low (-)": "2704.95",
        "Profit": "=C6-B6"
    },
    {
        "Date": "8-6-2019",
        "Open": "2730.25",
        "Close": "2725.25",
        "High (+)": "2727.45",
        "Low (-)": "2725.67",
        "Profit": "=C7-B7"
    },
    {
        "Date": "8-7-2019",
        "Open": "2725.25",
        "Close": "2778.75",
        "High (+)": "2770.45",
        "Low (-)": "2730.60",
        "Profit": "=C8-B8"
    },
    {
        "Date": "8-8-2019",
        "Open": "2778.75",
        "Close": "2800.67",
        "High (+)": "2790.27",
        "Low (-)": "2780.78",
        "Profit": "=C9-B9"
    },
    {
        "Date": "8-9-2019",
        "Open": "2800.67",
        "Close": "2840.80",
        "High (+)": "2838.78",
        "Low (-)": "2827.78",
        "Profit": "=C10-B10"
    },
    {
        "Date": "8-10-2019",
        "Open": "2840.80",
        "Close": "2865.35",
        "High (+)": "2863.30",
        "Low (-)": "2850.20",
        "Profit": "=C11-B11"
    }
];
exports.protectSheetData = [
    {
        "Tenure": "1",
        "Payment Date": "03-04-2020",
        "EMI": "8698.84",
        "Prinicpal": "8032.18",
        "Interest": "666.67",
        "Balance": "91967.82"
    },
    {
        "Tenure": "2",
        "Payment Date": "03-05-2020",
        "EMI": "8698.84",
        "Prinicpal": "8085.72",
        "Interest": "613.12",
        "Balance": "83882.10"
    },
    {
        "Tenure": "3",
        "Payment Date": "03-06-2020",
        "EMI": "8698.84",
        "Prinicpal": "8139.63",
        "Interest": "559.21",
        "Balance": "75742.47"
    },
    {
        "Tenure": "4",
        "Payment Date": "03-07-2020",
        "EMI": "8698.84",
        "Prinicpal": "8193.89",
        "Interest": "504.95",
        "Balance": "67548.58"
    },
    {
        "Tenure": "5",
        "Payment Date": "03-08-2020",
        "EMI": "8698.84",
        "Prinicpal": "8248.52",
        "Interest": "450.32",
        "Balance": "59300.06"
    },
    {
        "Tenure": "6",
        "Payment Date": "03-09-2020",
        "EMI": "8698.84",
        "Prinicpal": "8303.51",
        "Interest": "395.33",
        "Balance": "50996.55"
    },
    {
        "Tenure": "7",
        "Payment Date": "03-10-2020",
        "EMI": "8698.84",
        "Prinicpal": "8358.87",
        "Interest": "339.98",
        "Balance": "42637.68"
    },
    {
        "Tenure": "8",
        "Payment Date": "03-11-2020",
        "EMI": "8698.84",
        "Prinicpal": "8414.59",
        "Interest": "284.25",
        "Balance": "34223.09"
    },
    {
        "Tenure": "9",
        "Payment Date": "03-12-2020",
        "EMI": "8698.84",
        "Prinicpal": "8470.69",
        "Interest": "228.15",
        "Balance": "25752.40"
    },
    {
        "Tenure": "10",
        "Payment Date": "03-01-2021",
        "EMI": "8698.84",
        "Prinicpal": "8527.16",
        "Interest": "171.68",
        "Balance": "17225.24"
    },
    {
        "Tenure": "11",
        "Payment Date": "03-02-2021",
        "EMI": "8698.84",
        "Prinicpal": "8584.01",
        "Interest": "114.83",
        "Balance": "8641.23"
    },
    {
        "Tenure": "12",
        "Payment Date": "03-03-2021",
        "EMI": "8698.84",
        "Prinicpal": "8641.23",
        "Interest": "57.61",
        "Balance": "0.00"
    }
];
exports.numberFormatData = [
    {
        "Item Code": "I231",
        "Item Name": "Chinese Combo Noodle",
        "Quantity": 2,
        "Rate": 125,
        "Amount": "=PRODUCT(C4,D4)"
    },
    {
        "Item Code": "I245",
        "Item Name": "Chinese Combo Rice",
        "Quantity": 3,
        "Rate": 125,
        "Amount": "=PRODUCT(C5,D5)"
    },
    {
        "Item Code": "I237",
        "Item Name": "Amritsari Chola",
        "Quantity": 2,
        "Rate": 225,
        "Amount": "=PRODUCT(C6,D6)"
    },
    {
        "Item Code": "I291",
        "Item Name": "Asian Mixed Entree Platt",
        "Quantity": 3,
        "Rate": 165,
        "Amount": "=PRODUCT(C7,D7)"
    },
    {
        "Item Code": "I268",
        "Item Name": "Chinese Combo Chicken",
        "Quantity": 3,
        "Rate": 125,
        "Amount": "=PRODUCT(C8,D8)"
    },
    {
        "Item Code": "I251",
        "Item Name": "Chivas Regal",
        "Quantity": 1,
        "Rate": 325,
        "Amount": "=PRODUCT(C9,D9)"
    },
    {
        "Item Code": "I256",
        "Item Name": "Chicken Drumsticks",
        "Quantity": 2,
        "Rate": 180,
        "Amount": "=PRODUCT(C10,D10)"
    },
    {
        "Item Code": "I232",
        "Item Name": "Manchow Soup",
        "Quantity": 2,
        "Rate": 160,
        "Amount": "=PRODUCT(C11,D11)"
    },
    {
        "Item Code": "I290",
        "Item Name": "Schezuan Chicken",
        "Quantity": 3,
        "Rate": 180,
        "Amount": "=PRODUCT(C12,D12)"
    },
    {
        "Item Code": "I229",
        "Item Name": "Manchow Soup",
        "Quantity": 2,
        "Rate": 125,
        "Amount": "=PRODUCT(C13,D13)"
    },
    {
        "Item Code": "I239",
        "Item Name": "Jw Black Lable",
        "Quantity": 2,
        "Rate": 175,
        "Amount": "=PRODUCT(C14,D14)"
    }
];
exports.sortingAndFiltering = [
    {
        "Employee ID": "5389863",
        "Employee Name": "Liuka Tewkesberry",
        "Gender": "Female",
        "Department": "Human Resources",
        "Date of Joining": "08/22/2018",
        "Salary": "$32940.53",
        "City": "Valencia"
    },
    {
        "Employee ID": "9141760",
        "Employee Name": "Maurine McGreal",
        "Gender": "Female",
        "Department": "Accounting",
        "Date of Joining": "02/19/2016",
        "Salary": "$12769.67",
        "City": "Thị Xã Lai Châu"
    },
    {
        "Employee ID": "0440371",
        "Employee Name": "Abby Marcum",
        "Gender": "Male",
        "Department": "Legal",
        "Date of Joining": "03/12/2019",
        "Salary": "$18565.98",
        "City": "Lachute"
    },
    {
        "Employee ID": "4319060",
        "Employee Name": "Colet Dreghorn",
        "Gender": "Male",
        "Department": "Marketing",
        "Date of Joining": "11/02/2018",
        "Salary": "$36075.62",
        "City": "Cincinnati"
    },
    {
        "Employee ID": "8524811",
        "Employee Name": "Morey Ilyin",
        "Gender": "Male",
        "Department": "Sales",
        "Date of Joining": "07/13/2018",
        "Salary": "$19845.66",
        "City": "Liudu"
    },
    {
        "Employee ID": "3536836",
        "Employee Name": "Justus Eilert",
        "Gender": "Male",
        "Department": "Services",
        "Date of Joining": "08/30/2016",
        "Salary": "$25054.73",
        "City": "Kitamilo"
    },
    {
        "Employee ID": "6035100",
        "Employee Name": "Lorelle Whyberd",
        "Gender": "Female",
        "Department": "Marketing",
        "Date of Joining": "09/16/2010",
        "Salary": "$21552.49",
        "City": "Qandala"
    },
    {
        "Employee ID": "9595117",
        "Employee Name": "Hans Sponer",
        "Gender": "Male",
        "Department": "Legal",
        "Date of Joining": "11/25/2019",
        "Salary": "$12751.78",
        "City": "Parakou"
    },
    {
        "Employee ID": "9251706",
        "Employee Name": "Esme Beaglehole",
        "Gender": "Female",
        "Department": "Engineering",
        "Date of Joining": "11/24/2017",
        "Salary": "$34244.64",
        "City": "Saint-Ambroise"
    },
    {
        "Employee ID": "5041677",
        "Employee Name": "Fernandina Grindrod",
        "Gender": "Female",
        "Department": "Marketing",
        "Date of Joining": "01/21/2019",
        "Salary": "$25722.37",
        "City": "Huashi"
    },
    {
        "Employee ID": "9596007",
        "Employee Name": "Glen Lambrechts",
        "Gender": "Male",
        "Department": "Human Resources",
        "Date of Joining": "09/25/2019",
        "Salary": "$47190.81",
        "City": "Krajan Nglinggis"
    },
    {
        "Employee ID": "3506310",
        "Employee Name": "Judy Crick",
        "Gender": "Female",
        "Department": "Human Resources",
        "Date of Joining": "10/22/2013",
        "Salary": "$14168.85",
        "City": "Jianchang"
    },
    {
        "Employee ID": "0283605",
        "Employee Name": "Megen McSherry",
        "Gender": "Female",
        "Department": "Engineering",
        "Date of Joining": "10/19/2016",
        "Salary": "$49673.94",
        "City": "Talisayan"
    },
    {
        "Employee ID": "5918336",
        "Employee Name": "Aloysius Blunden",
        "Gender": "Male",
        "Department": "Services",
        "Date of Joining": "06/16/2010",
        "Salary": "$23992.84",
        "City": "Italó"
    },
    {
        "Employee ID": "0858107",
        "Employee Name": "Colene Game",
        "Gender": "Female",
        "Department": "Training",
        "Date of Joining": "10/18/2015",
        "Salary": "$21768.47",
        "City": "Podbrdo"
    },
    {
        "Employee ID": "7239057",
        "Employee Name": "Bruce Pook",
        "Gender": "Male",
        "Department": "Services",
        "Date of Joining": "08/15/2019",
        "Salary": "$46169.78",
        "City": "Drawsko"
    },
    {
        "Employee ID": "2603324",
        "Employee Name": "Reggie Gethins",
        "Gender": "Female",
        "Department": "Training",
        "Date of Joining": "05/03/2014",
        "Salary": "$48793.33",
        "City": "Baiqiao"
    },
    {
        "Employee ID": "8641019",
        "Employee Name": "Happy Terrell",
        "Gender": "Female",
        "Department": "Product Management",
        "Date of Joining": "01/07/2010",
        "Salary": "$11971.89",
        "City": "Bandar-e Lengeh"
    },
    {
        "Employee ID": "2229621",
        "Employee Name": "Dermot Swithenby",
        "Gender": "Male",
        "Department": "Business Development",
        "Date of Joining": "05/31/2017",
        "Salary": "$14627.52",
        "City": "Da’an"
    },
    {
        "Employee ID": "6507226",
        "Employee Name": "Dusty Naulls",
        "Gender": "Female",
        "Department": "Engineering",
        "Date of Joining": "05/17/2012",
        "Salary": "$49569.60",
        "City": "Kelowna"
    },
    {
        "Employee ID": "9588238",
        "Employee Name": "Boycey Gemlbett",
        "Gender": "Male",
        "Department": "Accounting",
        "Date of Joining": "08/19/2014",
        "Salary": "$11400.60",
        "City": "Quezon"
    },
    {
        "Employee ID": "4783061",
        "Employee Name": "Gregoire Craik",
        "Gender": "Male",
        "Department": "Support",
        "Date of Joining": "01/07/2015",
        "Salary": "$28536.96",
        "City": "Sacramento"
    },
    {
        "Employee ID": "1086662",
        "Employee Name": "Paquito Keetch",
        "Gender": "Male",
        "Department": "Legal",
        "Date of Joining": "07/20/2017",
        "Salary": "$10948.94",
        "City": "Xieshui"
    },
    {
        "Employee ID": "7111790",
        "Employee Name": "Siouxie Lippini",
        "Gender": "Female",
        "Department": "Engineering",
        "Date of Joining": "02/08/2012",
        "Salary": "$42688.51",
        "City": "Bigoudine"
    },
    {
        "Employee ID": "9189124",
        "Employee Name": "Valerye Russell",
        "Gender": "Female",
        "Department": "Services",
        "Date of Joining": "07/17/2012",
        "Salary": "$32651.96",
        "City": "Pichilemu"
    },
    {
        "Employee ID": "6015219",
        "Employee Name": "Patience Ludman",
        "Gender": "Female",
        "Department": "Training",
        "Date of Joining": "09/25/2018",
        "Salary": "$17589.61",
        "City": "Binjiang"
    },
    {
        "Employee ID": "5210163",
        "Employee Name": "Helene Borsay",
        "Gender": "Female",
        "Department": "Services",
        "Date of Joining": "01/19/2018",
        "Salary": "$34017.42",
        "City": "Chengkan"
    },
    {
        "Employee ID": "6656266",
        "Employee Name": "Garrek Fatharly",
        "Gender": "Male",
        "Department": "Business Development",
        "Date of Joining": "08/22/2018",
        "Salary": "$49775.81",
        "City": "San Antonio Oeste"
    },
    {
        "Employee ID": "8740096",
        "Employee Name": "Fulton Culverhouse",
        "Gender": "Male",
        "Department": "Product Management",
        "Date of Joining": "01/11/2014",
        "Salary": "$36633.61",
        "City": "Borlänge"
    },
    {
        "Employee ID": "7875847",
        "Employee Name": "Lavena Yandle",
        "Gender": "Female",
        "Department": "Accounting",
        "Date of Joining": "08/31/2016",
        "Salary": "$25835.58",
        "City": "Borino"
    },
    {
        "Employee ID": "8436792",
        "Employee Name": "Sophia Cowwell",
        "Gender": "Female",
        "Department": "Product Management",
        "Date of Joining": "08/06/2011",
        "Salary": "$46836.53",
        "City": "Fécamp"
    },
    {
        "Employee ID": "2789522",
        "Employee Name": "Dode Bowmen",
        "Gender": "Female",
        "Department": "Sales",
        "Date of Joining": "02/21/2010",
        "Salary": "$27575.10",
        "City": "Karangwaru"
    },
    {
        "Employee ID": "2816501",
        "Employee Name": "Peggi Grishelyov",
        "Gender": "Female",
        "Department": "Accounting",
        "Date of Joining": "12/08/2012",
        "Salary": "$41712.59",
        "City": "San Antonio"
    },
    {
        "Employee ID": "5648843",
        "Employee Name": "Ailsun Porker",
        "Gender": "Female",
        "Department": "Services",
        "Date of Joining": "11/23/2016",
        "Salary": "$46533.06",
        "City": "Mayisyan"
    },
    {
        "Employee ID": "3645602",
        "Employee Name": "Licha McKee",
        "Gender": "Female",
        "Department": "Research and Development",
        "Date of Joining": "08/29/2018",
        "Salary": "$31008.69",
        "City": "Wenqiao"
    },
    {
        "Employee ID": "7413262",
        "Employee Name": "Ives Tunn",
        "Gender": "Male",
        "Department": "Product Management",
        "Date of Joining": "08/31/2018",
        "Salary": "$27791.30",
        "City": "Tabia"
    },
    {
        "Employee ID": "9348082",
        "Employee Name": "Cathy Bugdale",
        "Gender": "Female",
        "Department": "Training",
        "Date of Joining": "04/23/2016",
        "Salary": "$31759.16",
        "City": "Azul"
    },
    {
        "Employee ID": "7109150",
        "Employee Name": "Waverley Gingedale",
        "Gender": "Male",
        "Department": "Accounting",
        "Date of Joining": "09/04/2011",
        "Salary": "$27893.84",
        "City": "Frýdek-Místek"
    },
    {
        "Employee ID": "1119404",
        "Employee Name": "Cooper Capes",
        "Gender": "Male",
        "Department": "Sales",
        "Date of Joining": "01/02/2016",
        "Salary": "$26407.41",
        "City": "Ash Sharyah"
    },
    {
        "Employee ID": "9560117",
        "Employee Name": "Shayna Potebury",
        "Gender": "Female",
        "Department": "Human Resources",
        "Date of Joining": "10/23/2012",
        "Salary": "$35332.31",
        "City": "Baluk"
    },
    {
        "Employee ID": "2598799",
        "Employee Name": "Doro Gaitone",
        "Gender": "Female",
        "Department": "Sales",
        "Date of Joining": "02/04/2012",
        "Salary": "$22904.55",
        "City": "Wulingyuan"
    },
    {
        "Employee ID": "6767113",
        "Employee Name": "Chantal Ventam",
        "Gender": "Female",
        "Department": "Training",
        "Date of Joining": "07/02/2014",
        "Salary": "$25515.12",
        "City": "Xinshui"
    },
    {
        "Employee ID": "3735396",
        "Employee Name": "Tobie Brodeur",
        "Gender": "Male",
        "Department": "Training",
        "Date of Joining": "08/11/2015",
        "Salary": "$24508.84",
        "City": "Xin’an"
    },
    {
        "Employee ID": "2942841",
        "Employee Name": "Feodor MacDermid",
        "Gender": "Male",
        "Department": "Human Resources",
        "Date of Joining": "06/02/2019",
        "Salary": "$14844.80",
        "City": "Rantauambacang"
    },
    {
        "Employee ID": "1511001",
        "Employee Name": "Allyce Decker",
        "Gender": "Female",
        "Department": "Support",
        "Date of Joining": "04/14/2017",
        "Salary": "$39356.65",
        "City": "Saint Paul"
    },
    {
        "Employee ID": "9062663",
        "Employee Name": "Ricard Connock",
        "Gender": "Male",
        "Department": "Product Management",
        "Date of Joining": "11/25/2010",
        "Salary": "$27421.61",
        "City": "Lokorae"
    },
    {
        "Employee ID": "6210073",
        "Employee Name": "Minerva Greenham",
        "Gender": "Female",
        "Department": "Support",
        "Date of Joining": "05/23/2014",
        "Salary": "$48300.27",
        "City": "Pakemitan"
    },
    {
        "Employee ID": "1830385",
        "Employee Name": "Cyb Gallant",
        "Gender": "Female",
        "Department": "Accounting",
        "Date of Joining": "11/25/2010",
        "Salary": "$36518.72",
        "City": "Berëzovyy"
    },
    {
        "Employee ID": "7466629",
        "Employee Name": "Harman Free",
        "Gender": "Male",
        "Department": "Human Resources",
        "Date of Joining": "07/15/2012",
        "Salary": "$35939.87",
        "City": "Watodei"
    },
    {
        "Employee ID": "2963633",
        "Employee Name": "Kylie Phettis",
        "Gender": "Female",
        "Department": "Marketing",
        "Date of Joining": "03/18/2011",
        "Salary": "$26038.56",
        "City": "Huangzhai"
    }
];
exports.orderDetails = [
    {
        "Order ID": 10248,
        "Customer Name": "Paul Henriot",
        "Order Date": "7/4/1996",
        "Address": "59 rue de l Abbaye",
        "City": "Reims",
        "Postal Code": "51100",
        "Country": "France",
        "Status": "Delivered",
        "Freight": "$32.38"
    },
    {
        "Order ID": 10249,
        "Customer Name": "Karin Josephs",
        "Order Date": "7/5/1996",
        "Address": "Luisenstr. 48",
        "City": "Münster",
        "Postal Code": "44087",
        "Country": "Germany",
        "Status": "Delivered",
        "Freight": "$11.61"
    },
    {
        "Order ID": 10250,
        "Customer Name": "Mario Pontes",
        "Order Date": "7/8/1996",
        "Address": "Rua do Paço, 67",
        "City": "Rio de Janeiro",
        "Postal Code": "05454876",
        "Country": "Brazil",
        "Status": "Shipped",
        "Freight": "$65.83"
    },
    {
        "Order ID": 10251,
        "Customer Name": "Mary Saveley",
        "Order Date": "7/4/1996",
        "Address": "2, rue du Commerce",
        "City": "Lyon",
        "Postal Code": "69004",
        "Country": "France",
        "Status": "Delivered",
        "Freight": "$41.34"
    },
    {
        "Order ID": 10252,
        "Customer Name": "Pascale Cartrain",
        "Order Date": "7/8/1996",
        "Address": "Boulevard Tirou, 255",
        "City": "Charleroi",
        "Postal Code": "6000",
        "Country": "Belgium",
        "Status": "Shipped",
        "Freight": "$51.3"
    },
    {
        "Order ID": 10253,
        "Customer Name": "Carlos Hernández",
        "Order Date": "7/1/1996",
        "Address": "Rua do Paço, 67",
        "City": "Rio de Janeiro",
        "Postal Code": "05454876",
        "Country": "Brazil",
        "Status": "Cancelled",
        "Freight": "$58.17"
    },
    {
        "Order ID": 10254,
        "Customer Name": "Yang Wang",
        "Order Date": "7/18/1996",
        "Address": "Hauptstr. 31",
        "City": "Bern",
        "Postal Code": "3012",
        "Country": "Switzerland",
        "Status": "Pending",
        "Freight": "$22.98"
    },
    {
        "Order ID": 10255,
        "Customer Name": "Antonio Moreno",
        "Order Date": "7/7/1996",
        "Address": "Starenweg 5",
        "City": "Genève",
        "Postal Code": "1204",
        "Country": "Switzerland",
        "Status": "Delivered",
        "Freight": "$148.33"
    },
    {
        "Order ID": 10256,
        "Customer Name": "Paula Parente",
        "Order Date": "7/10/1996",
        "Address": "Rua do Mercado, 12",
        "City": "Resende",
        "Postal Code": "08737363",
        "Country": "Brazil",
        "Status": "Shipped",
        "Freight": "$13.97"
    },
    {
        "Order ID": 10257,
        "Customer Name": "Michael Holz",
        "Order Date": "7/9/1996",
        "Address": "Carrera 22 con Ave. Carlos Soublette",
        "City": "San Cristóbal",
        "Postal Code": "5022",
        "Country": "Venezuela",
        "Status": "Delivered",
        "Freight": "$81.91"
    },
    {
        "Order ID": 10258,
        "Customer Name": "Roland Mendel",
        "Order Date": "7/3/1996",
        "Address": "Kirchgasse 6",
        "City": "Graz",
        "Postal Code": "8010",
        "Country": "Austria",
        "Status": "Cancelled",
        "Freight": "$140.51"
    },
    {
        "Order ID": 10259,
        "Customer Name": "Francisco Chang",
        "Order Date": "7/22/1996",
        "Address": "Sierras de Granada 9993",
        "City": "México D.F.",
        "Postal Code": "05022",
        "Country": "Mexico",
        "Status": "Pending",
        "Freight": "$3.25"
    },
    {
        "Order ID": 10260,
        "Customer Name": "Henriette Pfalzheim",
        "Order Date": "7/12/1996",
        "Address": "Mehrheimerstr. 369",
        "City": "Köln",
        "Postal Code": "50739",
        "Country": "Germany",
        "Status": "Delivered",
        "Freight": "$55.09"
    },
    {
        "Order ID": 10261,
        "Customer Name": "Bernardo Batista",
        "Order Date": "7/15/1996",
        "Address": "Rua da Panificadora, 12",
        "City": "Rio de Janeiro",
        "Postal Code": "02389673",
        "Country": "Brazil",
        "Status": "Shipped",
        "Freight": "$3.05"
    },
    {
        "Order ID": 10262,
        "Customer Name": "Paula Wilson",
        "Order Date": "7/8/1996",
        "Address": "2817 Milton Dr.",
        "City": "Albuquerque",
        "Postal Code": "87110",
        "Country": "USA",
        "Status": "Delivered",
        "Freight": "$48.29"
    }
];
exports.conditionalFormatting = [
    {
        "Item Code": "AG940Z",
        "Item Name": "Laser Printer",
        "Quantity": "144",
        "Purchase Price": "$169.50",
        "Selling Price": "$172.00",
        "Profit": "=E3-D3",
        "Last Updated": "5/25/19",
        "Rating": "4.5"
    },
    {
        "Item Code": "BJ120K",
        "Item Name": "Scientific Calculator",
        "Quantity": "116",
        "Purchase Price": "$21.80",
        "Selling Price": "$23.00",
        "Profit": "=E4-D4",
        "Last Updated": "7/28/19",
        "Rating": "4.0"
    },
    {
        "Item Code": "BC120M",
        "Item Name": "Wired Keyboard",
        "Quantity": "438",
        "Purchase Price": "$26.80",
        "Selling Price": "$29.00",
        "Profit": "=E5-D5",
        "Last Updated": "3/30/20",
        "Rating": "4.25"
    },
    {
        "Item Code": "BS121L",
        "Item Name": "Memory Card",
        "Quantity": "486",
        "Purchase Price": "$23.50",
        "Selling Price": "$25.00",
        "Profit": "=E6-D6",
        "Last Updated": "8/20/19",
        "Rating": "3.5"
    },
    {
        "Item Code": "BU121K",
        "Item Name": "Coffee Maker",
        "Quantity": "176",
        "Purchase Price": "$56.50",
        "Selling Price": "$59.00",
        "Profit": "=E7-D7",
        "Last Updated": "2/2/20",
        "Rating": "4.5"
    },
    {
        "Item Code": "BD121M",
        "Item Name": "Table Lamp",
        "Quantity": "0",
        "Purchase Price": "$22.50",
        "Selling Price": "$25.00",
        "Profit": "=E8-D8",
        "Last Updated": "11/11/19",
        "Rating": "5.0"
    },
    {
        "Item Code": "AT992X",
        "Item Name": "Document Scanner",
        "Quantity": "116",
        "Purchase Price": "$175.00",
        "Selling Price": "$177.00",
        "Profit": "=E9-D9",
        "Last Updated": "4/13/19",
        "Rating": "4.75"
    },
    {
        "Item Code": "AP992Z",
        "Item Name": "Gaming Headset",
        "Quantity": "58",
        "Purchase Price": "$32.00",
        "Selling Price": "$35.00",
        "Profit": "=E10-D10",
        "Last Updated": "2/14/20",
        "Rating": "4.4"
    },
    {
        "Item Code": "AW920X",
        "Item Name": "Laptop Bag",
        "Quantity": "232",
        "Purchase Price": "$18.90",
        "Selling Price": "$19.00",
        "Profit": "=E11-D11",
        "Last Updated": "6/10/19",
        "Rating": "3.9"
    },
    {
        "Item Code": "AQ920Z",
        "Item Name": "Table Fan",
        "Quantity": "405",
        "Purchase Price": "$33.90",
        "Selling Price": "$35.00",
        "Profit": "=E12-D12",
        "Last Updated": "5/28/19",
        "Rating": "3.75"
    },
    {
        "Item Code": "AE940X",
        "Item Name": "Electric Mop",
        "Quantity": "47",
        "Purchase Price": "$153.50",
        "Selling Price": "$155.00",
        "Profit": "=E13-D13",
        "Last Updated": "12/18/19",
        "Rating": "3.9"
    },
    {
        "Item Code": "UI152C",
        "Item Name": "Smart LED TV",
        "Quantity": "232",
        "Purchase Price": "$201.25",
        "Selling Price": "$204.00",
        "Profit": "=E14-D14",
        "Last Updated": "3/13/20",
        "Rating": "4.35"
    },
    {
        "Item Code": "UD152V",
        "Item Name": "Robotic Vacuum Cleaner",
        "Quantity": "210",
        "Purchase Price": "$182.25",
        "Selling Price": "$185.00",
        "Profit": "=E15-D15",
        "Last Updated": "8/26/19",
        "Rating": "4.0"
    },
    {
        "Item Code": "BK120L",
        "Item Name": "Gaming Mouse",
        "Quantity": "225",
        "Purchase Price": "$34.80",
        "Selling Price": "$38.00",
        "Profit": "=E16-D16",
        "Last Updated": "1/12/20",
        "Rating": "4.2"
    },
    {
        "Item Code": "UF162V",
        "Item Name": "Chair",
        "Quantity": "373",
        "Purchase Price": "$104.800",
        "Selling Price": "$108.00",
        "Profit": "=E17-D17",
        "Last Updated": "4/12/19",
        "Rating": "3.25"
    },
    {
        "Item Code": "UR162C",
        "Item Name": "Welding Gloves",
        "Quantity": "216",
        "Purchase Price": "$19.00",
        "Selling Price": "$21.00",
        "Profit": "=E18-D18",
        "Last Updated": "11/22/19",
        "Rating": "3.85"
    }
];
exports.GDPData = [
    {
        "Country": "USA",
        "Year 2017": "18.00",
        "Year 2018": "20.40",
        "Year 2019": "21.42",
        "Year 2020": "19.48"
    },
    {
        "Country": "China",
        "Year 2017": "11.00",
        "Year 2018": "14.00",
        "Year 2019": "14.32",
        "Year 2020": "12.23"
    },
    {
        "Country": "Japan",
        "Year 2017": "4.4",
        "Year 2018": "5.10",
        "Year 2019": "5.08",
        "Year 2020": "4.87"
    },
    {
        "Country": "Germany",
        "Year 2017": "3.3",
        "Year 2018": "4.20",
        "Year 2019": "3.84",
        "Year 2020": "3.69"
    },
    {
        "Country": "India",
        "Year 2017": "2.00",
        "Year 2018": "2.85",
        "Year 2019": "2.87",
        "Year 2020": "2.65"
    },
    {
        "Country": "UK",
        "Year 2017": "2.90",
        "Year 2018": "2.94",
        "Year 2019": "2.82",
        "Year 2020": "2.63"
    },
    {
        "Country": "France",
        "Year 2017": "2.40",
        "Year 2018": "2.93",
        "Year 2019": "2.71",
        "Year 2020": "2.58"
    }
];
exports.freezePaneData = [
    {
        "Month": "January",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C3,E3,G3,I3,K3,M3)",
        "Revised Total Gross Salary": "=SUM(D3,F3,H3,J3,L3,N3)"
    },
    {
        "Month": "February",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C4,E4,G4,I4,K4,M4)",
        "Revised Total Gross Salary": "=SUM(D4,F4,H4,J4,L4,N4)"
    },
    {
        "Month": "March",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C5,E5,G5,I5,K5,M5)",
        "Revised Total Gross Salary": "=SUM(D5,F5,H5,J5,L5,N5)"
    },
    {
        "Month": "April",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C6,E6,G6,I6,K6,M6)",
        "Revised Total Gross Salary": "=SUM(D6,F6,H6,J6,L6,N6)"
    },
    {
        "Month": "May",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C7,E7,G7,I7,K7,M7)",
        "Revised Total Gross Salary": "=SUM(D7,F7,H7,J7,L7,N7)"
    },
    {
        "Month": "June",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C8,E8,G8,I8,K8,M8)",
        "Revised Total Gross Salary": "=SUM(D8,F8,H8,J8,L8,N8)"
    },
    {
        "Month": "July",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C9,E9,G9,I9,K9,M9)",
        "Revised Total Gross Salary": "=SUM(D9,F9,H9,J9,L9,N9)"
    },
    {
        "Month": "August",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C10,E10,G10,I10,K10,M10)",
        "Revised Total Gross Salary": "=SUM(D10,F10,H10,J10,L10,N10)"
    },
    {
        "Month": "September",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C11,E11,G11,I11,K11,M11)",
        "Revised Total Gross Salary": "=SUM(D11,F11,H11,J11,L11,N11)"
    },
    {
        "Month": "October",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C12,E12,G12,I12,K12,M12)",
        "Revised Total Gross Salary": "=SUM(D12,F12,H12,J12,L12,N12)"
    },
    {
        "Month": "November",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C13,E13,G13,I13,K13,M13)",
        "Revised Total Gross Salary": "=SUM(D13,F13,H13,J13,L13,N13)"
    },
    {
        "Month": "December",
        "Year": "2019",
        "Basic Salary": 15100,
        "Revised Basic Salary": 15800,
        "DA": 2000,
        "Revised DA": 2200,
        "HRA": 5000,
        "Revised HRA": 6500,
        "Conveyance Allowance": 1500,
        "Revised Conveyance Allowance": 1700,
        "Medical Expenses": 1250,
        "Revised Medical Expenses": 1500,
        "Special Allowance": 1000,
        "Revised Spcial Allowance": 1200,
        "Total Gross Salary": "=SUM(C14,E14,G14,I14,K14,M14)",
        "Revised Total Gross Salary": "=SUM(D14,F14,H14,J14,L14,N14)"
    },
    {
        "Month": "January",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C15,E15,G15,I15,K15,M15)",
        "Revised Total Gross Salary": "=SUM(D15,F15,H15,J15,L15,N15)"
    },
    {
        "Month": "February",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C16,E16,G16,I16,K16,M16)",
        "Revised Total Gross Salary": "=SUM(D16,F16,H16,J16,L16,N16)"
    },
    {
        "Month": "March",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C17,E17,G17,I17,K17,M17)",
        "Revised Total Gross Salary": "=SUM(D17,F17,H17,J17,L17,N17)"
    },
    {
        "Month": "April",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C18,E18,G18,I18,K18,M18)",
        "Revised Total Gross Salary": "=SUM(D18,F18,H18,J18,L18,N18)"
    },
    {
        "Month": "May",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C19,E19,G19,I19,K19,M19)",
        "Revised Total Gross Salary": "=SUM(D19,F19,H19,J19,L19,N19)"
    },
    {
        "Month": "June",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C20,E20,G20,I20,K20,M20)",
        "Revised Total Gross Salary": "=SUM(D20,F20,H20,J20,L20,N20)"
    },
    {
        "Month": "July",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C21,E21,G21,I21,K21,M21)",
        "Revised Total Gross Salary": "=SUM(D21,F21,H21,J21,L21,N21)"
    },
    {
        "Month": "August",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C22,E22,G22,I22,K22,M22)",
        "Revised Total Gross Salary": "=SUM(D22,F22,H22,J22,L22,N22)"
    },
    {
        "Month": "September",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C23,E23,G23,I23,K23,M23)",
        "Revised Total Gross Salary": "=SUM(D23,F23,H23,J23,L23,N23)"
    },
    {
        "Month": "October",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C24,E24,G24,I24,K24,M24)",
        "Revised Total Gross Salary": "=SUM(D24,F24,H24,J24,L24,N24)"
    },
    {
        "Month": "November",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C25,E25,G25,I25,K25,M25)",
        "Revised Total Gross Salary": "=SUM(D25,F25,H25,J25,L25,N25)"
    },
    {
        "Month": "December",
        "Year": "2020",
        "Basic Salary": 16610,
        "Revised Basic Salary": 17380,
        "DA": 2200,
        "Revised DA": 2420,
        "HRA": 5500,
        "Revised HRA": 7150,
        "Conveyance Allowance": 1650,
        "Revised Conveyance Allowance": 1870,
        "Medical Expenses": 1375,
        "Revised Medical Expenses": 1650,
        "Special Allowance": 1100,
        "Revised Spcial Allowance": 1320,
        "Total Gross Salary": "=SUM(C26,E26,G26,I26,K26,M26)",
        "Revised Total Gross Salary": "=SUM(D26,F26,H26,J26,L26,N26)"
    }
];
