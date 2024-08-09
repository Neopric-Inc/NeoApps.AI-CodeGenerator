import React from 'react';
import shortid from "shortid";
import GridViewIcon from '@mui/icons-material/GridView';
import ApiIcon from '@mui/icons-material/Api';
import GridOnIcon from '@mui/icons-material/GridOn';
import ListIcon from '@mui/icons-material/List';
import CropSquareIcon from '@mui/icons-material/CropSquare';
import FormatColorTextIcon from '@mui/icons-material/FormatColorText';
import BadgeIcon from '@mui/icons-material/Badge';
import EmailIcon from '@mui/icons-material/Email';
import PhoneIcon from '@mui/icons-material/Phone';
import ImageIcon from '@mui/icons-material/Image';
import TitleIcon from '@mui/icons-material/Title';
import AddCardIcon from '@mui/icons-material/AddCard';
import {Card} from 'Dnd/Draggable Components/Previous_Components/CustomComponents/Card/card'
import Heading from 'Dnd/Draggable Components/Previous_Components/CustomComponents/Heading/heading';

import DataGridlist from "Dnd/Draggable Components/Previous_Components/CustomComponents/GridList/DataGrid"
import NestedList from 'Dnd/Draggable Components/Previous_Components/CustomComponents/List/List';
import CustomButton from 'Dnd/Draggable Components/Previous_Components/CustomComponents/Button/Button';
import CustomTextField from 'Dnd/Draggable Components/Previous_Components/CustomComponents/TextField/TextField';
import Grid from 'Dnd/Draggable Components/Previous_Components/SyncFusionComponents/Grid'
import { DropDownList } from "Dnd/Draggable Components/Previous_Components/CustomComponents/DropDownList";
import MenuBar from 'Dnd/Draggable Components/Previous_Components/CustomComponents/MenuBar';
import {AccountTypesDropDownList} from "../../Draggable Components/syncfusion_components/DropDownList/AccountTypesDropDownList";
import {AccountTypesGridView} from "../../Draggable Components/syncfusion_components/Grid/AccountTypesGridView";
import {AccountTypesListView} from "../../Draggable Components/syncfusion_components/ListView/AccountTypesListView";
import {AccountTypesAutoComplete} from "Dnd/Draggable Components/syncfusion_components/Autocomplete/AccountTypesAutoComplete";
import {AccountTypesQueryBuilder} from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/AccountTypesQueryBuilder";
import {AccountTypes} from "Dnd/Draggable Components/Previous_Components/CustomComponents/AccountTypes";
import {CategoryTypesDropDownList} from "../../Draggable Components/syncfusion_components/DropDownList/CategoryTypesDropDownList";
import {CategoryTypesGridView} from "../../Draggable Components/syncfusion_components/Grid/CategoryTypesGridView";
import {CategoryTypesListView} from "../../Draggable Components/syncfusion_components/ListView/CategoryTypesListView";
import {CategoryTypesAutoComplete} from "Dnd/Draggable Components/syncfusion_components/Autocomplete/CategoryTypesAutoComplete";
import {CategoryTypesQueryBuilder} from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/CategoryTypesQueryBuilder";
import {CategoryTypes} from "Dnd/Draggable Components/Previous_Components/CustomComponents/CategoryTypes";
import {AccountsDropDownList} from "../../Draggable Components/syncfusion_components/DropDownList/AccountsDropDownList";
import {AccountsGridView} from "../../Draggable Components/syncfusion_components/Grid/AccountsGridView";
import {AccountsListView} from "../../Draggable Components/syncfusion_components/ListView/AccountsListView";
import {AccountsAutoComplete} from "Dnd/Draggable Components/syncfusion_components/Autocomplete/AccountsAutoComplete";
import {AccountsQueryBuilder} from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/AccountsQueryBuilder";
import {Accounts} from "Dnd/Draggable Components/Previous_Components/CustomComponents/Accounts";
import {CategoriesDropDownList} from "../../Draggable Components/syncfusion_components/DropDownList/CategoriesDropDownList";
import {CategoriesGridView} from "../../Draggable Components/syncfusion_components/Grid/CategoriesGridView";
import {CategoriesListView} from "../../Draggable Components/syncfusion_components/ListView/CategoriesListView";
import {CategoriesAutoComplete} from "Dnd/Draggable Components/syncfusion_components/Autocomplete/CategoriesAutoComplete";
import {CategoriesQueryBuilder} from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/CategoriesQueryBuilder";
import {Categories} from "Dnd/Draggable Components/Previous_Components/CustomComponents/Categories";
import {TransactionsDropDownList} from "../../Draggable Components/syncfusion_components/DropDownList/TransactionsDropDownList";
import {TransactionsGridView} from "../../Draggable Components/syncfusion_components/Grid/TransactionsGridView";
import {TransactionsListView} from "../../Draggable Components/syncfusion_components/ListView/TransactionsListView";
import {TransactionsAutoComplete} from "Dnd/Draggable Components/syncfusion_components/Autocomplete/TransactionsAutoComplete";
import {TransactionsQueryBuilder} from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/TransactionsQueryBuilder";
import {Transactions} from "Dnd/Draggable Components/Previous_Components/CustomComponents/Transactions";

// import NavBar from '../../Draggable Components/Previous_Components/CustomComponents/Canvas/Navbar';
// import SideBar from '../../Draggable Components/Previous_Components/CustomComponents/Canvas/Sidebar';

export const SIDEBAR_ITEM = "sidebarItem";
export const ROW = "row";
export const COLUMN = "column";
export const COMPONENT = "component";
export const SIDEBAR_ITEM_CRUD = "sidebarItemCRUD";

export const ErrorControlList = {
  email: "email",
  text: "text",
  number: { min: "minimum 8 digit is required", max: "maximum limit 20 digit" },
  password: {
    min: "minimum length 8 is required",
    max: "maximum limit 20 character",
    upperCase: "Must contain 1 uppercase letter",
    lowerCase: "Must contain 1 lowercase letter",
  },
};

export const ValidationControl = (value, m1, m2) => {
  switch (m1) {
    case "password":
      switch (m2) {
        case "max":
          var regexp = /^.{,20}$/i;
          let maxvalid = regexp.test(value);
          return maxvalid
            ? {
                isValid: true,
              }
            : {
                isValid: false,
                errorMessage: ErrorControlList["password"]["max"],
              };
          break;
        case "min":
          var regexp = /^.{8,}$/i;
          let minvalid = regexp.test(value);
          return minvalid
            ? {
                isValid: true,
              }
            : {
                isValid: false,
                errorMessage: ErrorControlList["password"]["min"],
              };
          break;
        case "upperCase":
          var regexp = /^(?=.*[A-Z])/;
          let upvalid = regexp.test(value);
          return upvalid
            ? {
                isValid: true,
              }
            : {
                isValid: false,
                errorMessage: ErrorControlList["password"]["upperCase"],
              };

          break;
        case "lowerCase":
          var regexp = /^(?=.*[a-z])/;
          let lowvalid = regexp.test(value);
          return lowvalid
            ? {
                isValid: true,
              }
            : {
                isValid: false,
                errorMessage: ErrorControlList["password"]["min"],
              };
          break;
        default:
          break;
      }
      break;
    case "number":
      switch (m2) {
        case "max":
          var regexp = /^.{,20}$/i;
          let maxvalid = regexp.test(value);
          return maxvalid
            ? {
                isValid: true,
              }
            : {
                isValid: false,
                errorMessage: ErrorControlList["number"]["max"],
              };
          break;
        case "min":
          var regexp = /^.{8,}$/i;
          let minvalid = regexp.test(value);
          return minvalid
            ? {
                isValid: true,
              }
            : {
                isValid: false,
                errorMessage: ErrorControlList["number"]["min"],
              };
          break;
        default:
          break;
      }
      break;
    case "email":
      var regexp = /^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$/g;
      let emvalid = regexp.test(value);
      return emvalid
        ? {
            isValid: true,
          }
        : {
            isValid: false,
            errorMessage: m2,
          };
      break;
    case "text":
      var regexp = /[\S\s]+[\S]+/;
      let maxvalid = regexp.test(value);
      return maxvalid
        ? {
            isValid: true,
          }
        : {
            isValid: false,
            errorMessage: m2,
          };
      break;
    default:
      break;
  }
};

export interface ISidebar_Items {
    id: string;
    type: string;
    component: {
        type: string;
        content: JSX.Element | string | React.FC | any;
        icon: JSX.Element;
        component_name?: string;
        icon_name?: string;
    },
}

export const getList = (params: string) => {
  if (params === "int") return ["radio", "dropdown"];
  if (params === "date") return ["date", "datetime"];
};


export const SIDEBAR_ITEMS: ISidebar_Items[] = [
	{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "Heading",
            content: <Heading />,
            icon: < TitleIcon className='dnd sidebarIcon' />,
            component_name: "Heading",
            icon_name: "TitleIcon",
        },
    },
       /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountTypesDropDownList",
            content: AccountTypesDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountTypesDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountTypesGridView",
            content: AccountTypesGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountTypesGridView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountTypesListView",
            content: AccountTypesListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountTypesListView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountTypesAutoComplete",
            content: AccountTypesAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountTypesAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountTypesQueryBuilder",
            content: AccountTypesQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountTypesQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
	{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountTypes",
            content: AccountTypes,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountTypes",
            icon_name: "ApiIcon",
        },
    },
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoryTypesDropDownList",
            content: CategoryTypesDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoryTypesDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoryTypesGridView",
            content: CategoryTypesGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoryTypesGridView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoryTypesListView",
            content: CategoryTypesListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoryTypesListView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoryTypesAutoComplete",
            content: CategoryTypesAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoryTypesAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoryTypesQueryBuilder",
            content: CategoryTypesQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoryTypesQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
	{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoryTypes",
            content: CategoryTypes,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoryTypes",
            icon_name: "ApiIcon",
        },
    },
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountsDropDownList",
            content: AccountsDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountsDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountsGridView",
            content: AccountsGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountsGridView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountsListView",
            content: AccountsListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountsListView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountsAutoComplete",
            content: AccountsAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountsAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AccountsQueryBuilder",
            content: AccountsQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "AccountsQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
	{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "Accounts",
            content: Accounts,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "Accounts",
            icon_name: "ApiIcon",
        },
    },
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoriesDropDownList",
            content: CategoriesDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoriesDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoriesGridView",
            content: CategoriesGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoriesGridView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoriesListView",
            content: CategoriesListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoriesListView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoriesAutoComplete",
            content: CategoriesAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoriesAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CategoriesQueryBuilder",
            content: CategoriesQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "CategoriesQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
	{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "Categories",
            content: Categories,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "Categories",
            icon_name: "ApiIcon",
        },
    },
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "TransactionsDropDownList",
            content: TransactionsDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "TransactionsDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "TransactionsGridView",
            content: TransactionsGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "TransactionsGridView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "TransactionsListView",
            content: TransactionsListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "TransactionsListView",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "TransactionsAutoComplete",
            content: TransactionsAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "TransactionsAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
	/*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "TransactionsQueryBuilder",
            content: TransactionsQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "TransactionsQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
	{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "Transactions",
            content: Transactions,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "Transactions",
            icon_name: "ApiIcon",
        },
    },
	
    /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "Card",
            content: <Card />,
            icon: < AddCardIcon className='dnd sidebarIcon' />,
            component_name: "Card",
            icon_name: "AddCardIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "SyncFusion GRID",
            content: Grid,
            icon: < GridViewIcon className='dnd sidebarIcon' />,
            component_name: "Grid",
            icon_name: "GridViewIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "GridList",
            content: < DataGridlist />,
            icon: < GridOnIcon className='dnd sidebarIcon' />,
            component_name: "DataGridlist",
            icon_name: "GridOnIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "List",
            content: <DropDownList />,
            icon: < ListIcon className='dnd sidebarIcon' />,
            component_name: "NestedList",
            icon_name: "ListIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "Button",
            content: < CustomButton />,
            icon: < CropSquareIcon className='dnd sidebarIcon' />,
            component_name: "CustomButton",
            icon_name: "CropSquareIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "input",
            content: <CustomTextField />,
            icon: < FormatColorTextIcon className='dnd sidebarIcon' />,
            component_name: "CustomTextField",
            icon_name: "FormatColorTextIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "MenuBar",
            content: <MenuBar />,
            icon: < FormatColorTextIcon className='dnd sidebarIcon' />,
            component_name: "MenuBar",
            icon_name: "FormatColorTextIcon",
        },
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "name",
            content: "Some name",
            icon: < BadgeIcon className='dnd sidebarIcon' />,
            icon_name: "BadgeIcon",
        }
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "email",
            content: "Some email",
            icon: < EmailIcon className='dnd sidebarIcon' />,
            icon_name: "EmailIcon",
        }
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "phone",
            content: "Some phone",
            icon: < PhoneIcon className='dnd sidebarIcon' />,
            icon_name: "PhoneIcon",
        }
    },
    {
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "image",
            content: "Some image",
            icon: < ImageIcon className='dnd sidebarIcon' />,
            icon_name: "ImageIcon",
        }
    }*/
];

export const functionTOmap = (component, config) => {
    // console.log("component from functionToMap :", component)
    switch (component) {
		case "Heading":
            return <Heading config={config} />;
            break;
        case "Card":
            return <Card config={config} />
            break;
        case "SyncFusion GRID":
            return <Grid />
            break;
        /*case "AccountTypesDropDownList":
            return < AccountTypesDropDownList config={config} />
            break;*/ 
	/*case "AccountTypesGridView":
            return < AccountTypesGridView config={config} />
            break; */
	/*case "AccountTypesListView":
            return < AccountTypesListView config={config} />
            break; */
	/*case "AccountTypesAutoComplete":
            return < AccountTypesAutoComplete config={config} />
            break; */
	/*case "AccountTypesQueryBuilder":
            return < AccountTypesQueryBuilder config={config} />
            break;*/ 
	case "AccountTypes":
            return < AccountTypes config={config} />
            break; 
	/*case "CategoryTypesDropDownList":
            return < CategoryTypesDropDownList config={config} />
            break;*/ 
	/*case "CategoryTypesGridView":
            return < CategoryTypesGridView config={config} />
            break; */
	/*case "CategoryTypesListView":
            return < CategoryTypesListView config={config} />
            break; */
	/*case "CategoryTypesAutoComplete":
            return < CategoryTypesAutoComplete config={config} />
            break; */
	/*case "CategoryTypesQueryBuilder":
            return < CategoryTypesQueryBuilder config={config} />
            break;*/ 
	case "CategoryTypes":
            return < CategoryTypes config={config} />
            break; 
	/*case "AccountsDropDownList":
            return < AccountsDropDownList config={config} />
            break;*/ 
	/*case "AccountsGridView":
            return < AccountsGridView config={config} />
            break; */
	/*case "AccountsListView":
            return < AccountsListView config={config} />
            break; */
	/*case "AccountsAutoComplete":
            return < AccountsAutoComplete config={config} />
            break; */
	/*case "AccountsQueryBuilder":
            return < AccountsQueryBuilder config={config} />
            break;*/ 
	case "Accounts":
            return < Accounts config={config} />
            break; 
	/*case "CategoriesDropDownList":
            return < CategoriesDropDownList config={config} />
            break;*/ 
	/*case "CategoriesGridView":
            return < CategoriesGridView config={config} />
            break; */
	/*case "CategoriesListView":
            return < CategoriesListView config={config} />
            break; */
	/*case "CategoriesAutoComplete":
            return < CategoriesAutoComplete config={config} />
            break; */
	/*case "CategoriesQueryBuilder":
            return < CategoriesQueryBuilder config={config} />
            break;*/ 
	case "Categories":
            return < Categories config={config} />
            break; 
	/*case "TransactionsDropDownList":
            return < TransactionsDropDownList config={config} />
            break;*/ 
	/*case "TransactionsGridView":
            return < TransactionsGridView config={config} />
            break; */
	/*case "TransactionsListView":
            return < TransactionsListView config={config} />
            break; */
	/*case "TransactionsAutoComplete":
            return < TransactionsAutoComplete config={config} />
            break; */
	/*case "TransactionsQueryBuilder":
            return < TransactionsQueryBuilder config={config} />
            break;*/ 
	case "Transactions":
            return < Transactions config={config} />
            break; 
	
        case "Button":
            return <CustomButton config={config} />
            break;
        case "input":
            return <CustomTextField config={config} />
            break;
        case "MenuBar":
            return <MenuBar />
            break;
        case "GridList":
            return <DataGridlist />
            break;
        case "List":
            return <DropDownList />
            // return <NestedList config={config} />
            break;
        case "name":
            return <h3>Some Name</h3>
            break;
        case "email":
            return "Some email"
            break;
        case "phone":
            return "Some phone"
            break;
        case "image":
            return "Some image"
            break;
        default:
            return <p>Cant't find any Component</p>
    }
}
export const mapNametoComponent = {
    "Grid": Grid,
    "AccountTypesDropDownList": AccountTypesDropDownList,
	"AccountTypesGridView": AccountTypesGridView,
	"AccountTypesListView": AccountTypesListView,
	"AccountTypesAutoComplete": AccountTypesAutoComplete,
	"AccountTypesQueryBuilder": AccountTypesQueryBuilder,
	"AccountTypes": AccountTypes,
	"CategoryTypesDropDownList": CategoryTypesDropDownList,
	"CategoryTypesGridView": CategoryTypesGridView,
	"CategoryTypesListView": CategoryTypesListView,
	"CategoryTypesAutoComplete": CategoryTypesAutoComplete,
	"CategoryTypesQueryBuilder": CategoryTypesQueryBuilder,
	"CategoryTypes": CategoryTypes,
	"AccountsDropDownList": AccountsDropDownList,
	"AccountsGridView": AccountsGridView,
	"AccountsListView": AccountsListView,
	"AccountsAutoComplete": AccountsAutoComplete,
	"AccountsQueryBuilder": AccountsQueryBuilder,
	"Accounts": Accounts,
	"CategoriesDropDownList": CategoriesDropDownList,
	"CategoriesGridView": CategoriesGridView,
	"CategoriesListView": CategoriesListView,
	"CategoriesAutoComplete": CategoriesAutoComplete,
	"CategoriesQueryBuilder": CategoriesQueryBuilder,
	"Categories": Categories,
	"TransactionsDropDownList": TransactionsDropDownList,
	"TransactionsGridView": TransactionsGridView,
	"TransactionsListView": TransactionsListView,
	"TransactionsAutoComplete": TransactionsAutoComplete,
	"TransactionsQueryBuilder": TransactionsQueryBuilder,
	"Transactions": Transactions,

    "DataGridlist": <DataGridlist />,
    "NestedList": <NestedList />,
    "CustomButton": <CustomButton />,
    "CustomTextFields": <CustomTextField />,
    "Some name": "Some name",
    "Some email": "Some email",
    "Some phone": "Some phone",
    "Some image": "Some image"
}

export const SyncFusion_Component_List = ["SyncFusion GRID"];
export const MUI_Component_List = ["DataGridlist", "NestedList", "CustomButton", "CustomTextFields"];

export const Configurations = {
    "Button": {
        "innerContent": {
            "input-type": "text"
        },
        "type": {
            "input-type": "list",
            "options": ["Button", "Submit", "Reset"]
        },
        "cssClass": {
            "input-type": "list",
            "options": ["e-primary", "e-success", "e-info", "e-warning", "e-danger"]
        },
        "onClick": {
            "input-type": "text"
        }
    },
    "input": {
        "innerContent": {
            "input-type": "text"
        },
    },
    Heading: {
    innerContent: {
      "input-type": "text",
    },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: {
      "input-type": "heading-color",
      options: [
        "black",
        "gray",
        "white",
        "Tomato",
        "DodgerBlue",
        "MediumSeaGreen",
        "LightGray",
      ],
    },
    color: {
      "input-type": "heading-color",
      options: ["black", "white", "gray"],
    },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "Courier New",
        "cursive",
      ],
    },
  },
    GlobalConfig: {
    backgroundColor: {
      "input-type": "table-color",
      options: [
        "black",
        "gray",
        "white",
        "Tomato",
        "DodgerBlue",
        "MediumSeaGreen",
        "LightGray",
      ],
    },
    SidebarBackgroundColor: {
      "input-type": "table-color",
      options: [
        "black",
        "gray",
        "white",
        "Tomato",
        "DodgerBlue",
        "MediumSeaGreen",
        "LightGray",
      ],
    },
    SidebarTextColor: {
      "input-type": "table-color",
      options: [
        "black",
        "gray",
        "white",
        "Tomato",
        "DodgerBlue",
        "MediumSeaGreen",
        "LightGray",
      ],
    },
    SidebarTextBackgroundColor: {
      "input-type": "table-color",
      options: [
        "black",
        "gray",
        "white",
        "Tomato",
        "DodgerBlue",
        "MediumSeaGreen",
        "LightGray",
      ],
    },
  },

    "List": {
        "primaryKeyList": {
            "input-type": "list",
            "options": ["backend_stack_id", "backend_stack_name", "createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive"]
        },
        "secondaryKeyList": {
            "input-type": "list",
            "options": ["backend_stack_id", "backend_stack_name", "createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive"]
        },
        "functionName": {
            "input-type": "list",
            "options": ["getAllBackend_Stacks", "getOneBackend_Stacks", "searchBackend_Stacks", "addBackend_Stacks", "updateBackend_Stacks", "deleteBackend_Stacks"]
        },
    },
    "SyncFusion GRID": {
        "primaryKeyList": {
            "input-type": "list",
            "options": ["backend_stack_id", "backend_stack_name", "createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive"]
        },
        "modelName": {
            "input-type": "list",
            "options": ["", "Backend_Stacks"]
        },
        "tableName": {
            "input-type": "list",
            "options": ["", "backend_stacks"]
        },
        "functionName": {
            "input-type": "list",
            "options": ["getAllBackend_Stacks", "getOneBackend_Stacks", "searchBackend_Stacks", "addBackend_Stacks", "updateBackend_Stacks", "deleteBackend_Stacks"]
        },
    },
    "Card": {
        "header": {
            "input-type": "text"
        },
        "content": {
            "input-type": "text"
        }
    },
    AccountTypes :{innerContent:{"input-type":"text",},headerSize:{"input-type":"list",options:["h1","h2","h3","h4","h5","h6"],},backgroundColor:{"input-type":"heading-color"},color:{"input-type":"heading-color"},tableHeadBackgroundColor:{"input-type":"table-head-color",},HeadColor:{"input-type":"table-head-color",},tableBackgroundColor:{"input-type":"table-color",},HeadRowBackgroundColor:{"input-type":"table-color",},HeadRowColor:{"input-type":"row-color",},RowBackgroundColor:{"input-type":"row-color",},RowColor:{"input-type":"row-color",},fontFamily:{"input-type":"list",options:["Arial","Helvetica","Verdana","Georgia","CourierNew","cursive",],},columns: {"input-type": "group","columns-list":[{name: 'account_type_id',fkey:false,icontrol: getList('int'),type: 'int',slice: '',},{name: 'account_type_name',fkey:false,icontrol: getList('varchar'),type: 'varchar',slice: '',},],"error-control-list": ["password", "email", "text", "number"],},row:{"input-type": "filter-form","columns-list": ['account_type_id','account_type_name',],"column-condition": ["==", "!=", ">"],}},CategoryTypes :{innerContent:{"input-type":"text",},headerSize:{"input-type":"list",options:["h1","h2","h3","h4","h5","h6"],},backgroundColor:{"input-type":"heading-color"},color:{"input-type":"heading-color"},tableHeadBackgroundColor:{"input-type":"table-head-color",},HeadColor:{"input-type":"table-head-color",},tableBackgroundColor:{"input-type":"table-color",},HeadRowBackgroundColor:{"input-type":"table-color",},HeadRowColor:{"input-type":"row-color",},RowBackgroundColor:{"input-type":"row-color",},RowColor:{"input-type":"row-color",},fontFamily:{"input-type":"list",options:["Arial","Helvetica","Verdana","Georgia","CourierNew","cursive",],},columns: {"input-type": "group","columns-list":[{name: 'category_type_id',fkey:false,icontrol: getList('int'),type: 'int',slice: '',},{name: 'category_type_name',fkey:false,icontrol: getList('varchar'),type: 'varchar',slice: '',},],"error-control-list": ["password", "email", "text", "number"],},row:{"input-type": "filter-form","columns-list": ['category_type_id','category_type_name',],"column-condition": ["==", "!=", ">"],}},Accounts :{innerContent:{"input-type":"text",},headerSize:{"input-type":"list",options:["h1","h2","h3","h4","h5","h6"],},backgroundColor:{"input-type":"heading-color"},color:{"input-type":"heading-color"},tableHeadBackgroundColor:{"input-type":"table-head-color",},HeadColor:{"input-type":"table-head-color",},tableBackgroundColor:{"input-type":"table-color",},HeadRowBackgroundColor:{"input-type":"table-color",},HeadRowColor:{"input-type":"row-color",},RowBackgroundColor:{"input-type":"row-color",},RowColor:{"input-type":"row-color",},fontFamily:{"input-type":"list",options:["Arial","Helvetica","Verdana","Georgia","CourierNew","cursive",],},columns: {"input-type": "group","columns-list":[{name: 'account_id',fkey:false,icontrol: getList('int'),type: 'int',slice: '',},{name: 'account_name',fkey:false,icontrol: getList('varchar'),type: 'varchar',slice: '',},{name: 'account_type_id',fkey:true,icontrol: getList('int'),type: 'int',slice: 'AccountTypes',},{name: 'balance',fkey:false,icontrol: getList('decimal'),type: 'decimal',slice: '',},{name: 'created_at',fkey:false,icontrol: getList('timestamp'),type: 'timestamp',slice: '',},],"error-control-list": ["password", "email", "text", "number"],},row:{"input-type": "filter-form","columns-list": ['account_id','account_name','account_type_id','balance','created_at',],"column-condition": ["==", "!=", ">"],}},Categories :{innerContent:{"input-type":"text",},headerSize:{"input-type":"list",options:["h1","h2","h3","h4","h5","h6"],},backgroundColor:{"input-type":"heading-color"},color:{"input-type":"heading-color"},tableHeadBackgroundColor:{"input-type":"table-head-color",},HeadColor:{"input-type":"table-head-color",},tableBackgroundColor:{"input-type":"table-color",},HeadRowBackgroundColor:{"input-type":"table-color",},HeadRowColor:{"input-type":"row-color",},RowBackgroundColor:{"input-type":"row-color",},RowColor:{"input-type":"row-color",},fontFamily:{"input-type":"list",options:["Arial","Helvetica","Verdana","Georgia","CourierNew","cursive",],},columns: {"input-type": "group","columns-list":[{name: 'category_id',fkey:false,icontrol: getList('int'),type: 'int',slice: '',},{name: 'category_name',fkey:false,icontrol: getList('varchar'),type: 'varchar',slice: '',},{name: 'category_type_id',fkey:true,icontrol: getList('int'),type: 'int',slice: 'CategoryTypes',},{name: 'created_at',fkey:false,icontrol: getList('timestamp'),type: 'timestamp',slice: '',},],"error-control-list": ["password", "email", "text", "number"],},row:{"input-type": "filter-form","columns-list": ['category_id','category_name','category_type_id','created_at',],"column-condition": ["==", "!=", ">"],}},Transactions :{innerContent:{"input-type":"text",},headerSize:{"input-type":"list",options:["h1","h2","h3","h4","h5","h6"],},backgroundColor:{"input-type":"heading-color"},color:{"input-type":"heading-color"},tableHeadBackgroundColor:{"input-type":"table-head-color",},HeadColor:{"input-type":"table-head-color",},tableBackgroundColor:{"input-type":"table-color",},HeadRowBackgroundColor:{"input-type":"table-color",},HeadRowColor:{"input-type":"row-color",},RowBackgroundColor:{"input-type":"row-color",},RowColor:{"input-type":"row-color",},fontFamily:{"input-type":"list",options:["Arial","Helvetica","Verdana","Georgia","CourierNew","cursive",],},columns: {"input-type": "group","columns-list":[{name: 'account_id',fkey:true,icontrol: getList('int'),type: 'int',slice: 'Accounts',},{name: 'amount',fkey:false,icontrol: getList('decimal'),type: 'decimal',slice: '',},{name: 'category_id',fkey:true,icontrol: getList('int'),type: 'int',slice: 'Categories',},{name: 'created_at',fkey:false,icontrol: getList('timestamp'),type: 'timestamp',slice: '',},{name: 'description',fkey:false,icontrol: getList('varchar'),type: 'varchar',slice: '',},{name: 'transaction_date',fkey:false,icontrol: getList('date'),type: 'date',slice: '',},{name: 'transaction_id',fkey:false,icontrol: getList('int'),type: 'int',slice: '',},{name: 'transaction_type_id',fkey:false,icontrol: getList('int'),type: 'int',slice: '',},],"error-control-list": ["password", "email", "text", "number"],},row:{"input-type": "filter-form","columns-list": ['account_id','amount','category_id','created_at','description','transaction_date','transaction_id','transaction_type_id',],"column-condition": ["==", "!=", ">"],}},

}


// "SyncFusion GRID": {
//     "heading": {
//         "input-type": "text"
//     },
//     "primaryKeyList": {
//         "input-type": "list",
//         "options": ["backend_stack_id", "backend_stack_name", "createdBy", "modifiedBy", "isActive"]
//     },
//     "modelName": {
//         "input-type": "list",
//         "options": ["Backend_Stacks", "Branches", "Project", "User"]
//     },
//     "componentName": {
//         "input-type": "text"
//     }
// }

// {
//     id: shortid.generate(),
//     type: SIDEBAR_ITEM,
//     component: {
//         type: "NavBar",
//         content: NavBar,
//         icon: < ApiIcon className='dnd sidebarIcon' />,
//         component_name: "NavBar",
//         icon_name: "ApiIcon",
//     },
// },
// {
//     id: shortid.generate(),
//     type: SIDEBAR_ITEM,
//     component: {
//         type: "SideBar",
//         content: SideBar,
//         icon: < ApiIcon className='dnd sidebarIcon' />,
//         component_name: "SideBar",
//         icon_name: "ApiIcon",
//     },
// },

// case "NavBar":
//     return <NavBar />
//     break;
// case "SideBar":
//     return <SideBar />
//     break;