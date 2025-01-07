import React, { useEffect, useState } from "react";
import shortid from "shortid";
import ApiIcon from "@mui/icons-material/Api";
import TitleIcon from "@mui/icons-material/Title";
import { Card } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Card/card";
import Heading from "Dnd/Draggable Components/Previous_Components/CustomComponents/Heading/heading";
import { GetDataUrl } from "services/fileUploadService";
import DataGridlist from "Dnd/Draggable Components/Previous_Components/CustomComponents/GridList/DataGrid";
import NestedList from "Dnd/Draggable Components/Previous_Components/CustomComponents/List/List";
import CustomButton from "Dnd/Draggable Components/Previous_Components/CustomComponents/Button/Button";
import CustomTextField from "Dnd/Draggable Components/Previous_Components/CustomComponents/TextField/TextField";
import Grid from "Dnd/Draggable Components/Previous_Components/SyncFusionComponents/Grid";
import { DropDownList } from "Dnd/Draggable Components/Previous_Components/CustomComponents/DropDownList";
import MenuBar from "Dnd/Draggable Components/Previous_Components/CustomComponents/MenuBar";
import { S3bucketDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/s3bucketDropDownList";
import { S3bucketGridView } from "../../Draggable Components/syncfusion_components/Grid/s3bucketGridView";
import { S3bucketListView } from "../../Draggable Components/syncfusion_components/ListView/s3bucketListView";
import { S3bucketAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/s3bucketAutoComplete";
import { S3bucketQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/s3bucketQueryBuilder";
import { S3bucket } from "Dnd/Draggable Components/Previous_Components/CustomComponents/S3bucket";
import { EngagementtypeDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/engagementtypeDropDownList";
import { EngagementtypeGridView } from "../../Draggable Components/syncfusion_components/Grid/engagementtypeGridView";
import { EngagementtypeListView } from "../../Draggable Components/syncfusion_components/ListView/engagementtypeListView";
import { EngagementtypeAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/engagementtypeAutoComplete";
import { EngagementtypeQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/engagementtypeQueryBuilder";
import { Engagementtype } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Engagementtype";
import { ContentstatusDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/contentstatusDropDownList";
import { ContentstatusGridView } from "../../Draggable Components/syncfusion_components/Grid/contentstatusGridView";
import { ContentstatusListView } from "../../Draggable Components/syncfusion_components/ListView/contentstatusListView";
import { ContentstatusAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/contentstatusAutoComplete";
import { ContentstatusQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/contentstatusQueryBuilder";
import { Contentstatus } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Contentstatus";
import { UsersDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/usersDropDownList";
import { UsersGridView } from "../../Draggable Components/syncfusion_components/Grid/usersGridView";
import { UsersListView } from "../../Draggable Components/syncfusion_components/ListView/usersListView";
import { UsersAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/usersAutoComplete";
import { UsersQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/usersQueryBuilder";
import { Users } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Users";
import { SocialmediaaccountsDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/socialmediaaccountsDropDownList";
import { SocialmediaaccountsGridView } from "../../Draggable Components/syncfusion_components/Grid/socialmediaaccountsGridView";
import { SocialmediaaccountsListView } from "../../Draggable Components/syncfusion_components/ListView/socialmediaaccountsListView";
import { SocialmediaaccountsAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/socialmediaaccountsAutoComplete";
import { SocialmediaaccountsQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/socialmediaaccountsQueryBuilder";
import { Socialmediaaccounts } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Socialmediaaccounts";
import { ContentDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/contentDropDownList";
import { ContentGridView } from "../../Draggable Components/syncfusion_components/Grid/contentGridView";
import { ContentListView } from "../../Draggable Components/syncfusion_components/ListView/contentListView";
import { ContentAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/contentAutoComplete";
import { ContentQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/contentQueryBuilder";
import { Content } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Content";
import { EngagementDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/engagementDropDownList";
import { EngagementGridView } from "../../Draggable Components/syncfusion_components/Grid/engagementGridView";
import { EngagementListView } from "../../Draggable Components/syncfusion_components/ListView/engagementListView";
import { EngagementAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/engagementAutoComplete";
import { EngagementQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/engagementQueryBuilder";
import { Engagement } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Engagement";
import { AnalyticsDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/analyticsDropDownList";
import { AnalyticsGridView } from "../../Draggable Components/syncfusion_components/Grid/analyticsGridView";
import { AnalyticsListView } from "../../Draggable Components/syncfusion_components/ListView/analyticsListView";
import { AnalyticsAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/analyticsAutoComplete";
import { AnalyticsQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/analyticsQueryBuilder";
import { Analytics } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Analytics";
import { InfluencerDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/influencerDropDownList";
import { InfluencerGridView } from "../../Draggable Components/syncfusion_components/Grid/influencerGridView";
import { InfluencerListView } from "../../Draggable Components/syncfusion_components/ListView/influencerListView";
import { InfluencerAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/influencerAutoComplete";
import { InfluencerQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/influencerQueryBuilder";
import { Influencer } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Influencer";
import { CollaborationDropDownList } from "../../Draggable Components/syncfusion_components/DropDownList/collaborationDropDownList";
import { CollaborationGridView } from "../../Draggable Components/syncfusion_components/Grid/collaborationGridView";
import { CollaborationListView } from "../../Draggable Components/syncfusion_components/ListView/collaborationListView";
import { CollaborationAutoComplete } from "Dnd/Draggable Components/syncfusion_components/Autocomplete/collaborationAutoComplete";
import { CollaborationQueryBuilder } from "Dnd/Draggable Components/syncfusion_components/QueryBuilder/collaborationQueryBuilder";
import { Collaboration } from "Dnd/Draggable Components/Previous_Components/CustomComponents/Collaboration";

// import NavBar from '../../Draggable Components/Previous_Components/CustomComponents/Canvas/Navbar';
// import SideBar from '../../Draggable Components/Previous_Components/CustomComponents/Canvas/Sidebar';

export const SIDEBAR_ITEM = "sidebarItem";
export const ROW = "row";
export const COLUMN = "column";
export const COMPONENT = "component";
export const SIDEBAR_ITEM_CRUD = "sidebarItemCRUD";

export const getColumnNameList = (slices) => {
  return Configurations[slices[0].toUpperCase() + slices.slice(1)]["row"][
    "columns-list"
  ];
};

export const getGridList = (name) => {
  switch (name) {
    case "gridView1":
      return [
        "avtar",
        "name",
        "category",
        "cover",
        "publishedAt",
        "readTime",
        "shortDescription",
        "title",
      ];
    case "gridView2":
      return [
        "avatar",
        "username",
        "cover",
        "profile_url",
        "follower_count",
        "updatedAt",
      ];
    case "gridView3":
      return ["mimeType", "name", "size", "url"];
    case "gridView4":
      return ["avatar", "username", "cover", "profile_url", "platform"];
    case "gridView5":
      return ["avatar", "name", "createdAt", "likes", "media", "message"];
    case "gridView6":
      return ["avatar", "username", "follower_count", "email"];

    case "groupedList1":
      return ["customerAvatar", "customerName", "description", "createdAt"];

    case "groupedList2":
      return ["username", "email", "profile_url"];
    case "groupedList3":
      return ["title"];

    case "groupedList4":
      return ["value", "type", "message"];
    case "groupedList5":
      return ["image", "name", "sales", "profit", "currency", "conversionRate"];


    case "groupedList6":
      return ["date", "sender", "type", "amount", "currency"];

    case "groupedList7":
      return ["username", "email", "avatar", "date"];
    case "groupedList8":
      return ["avatar", "name", "job"];
    case "groupedList9":
      return ["avatar", "rating", "media_url", "text", "date"];
    case "groupedList10":
      return ["avatar", "subject", "description", "createdAt"];
    case "groupedList11":
      return [
        "campaign_name",
        "campaign_description",
        "start_date",
        "campaign_status",
        "influencer_name",
        "end_date",
      ];
    case "detailList1":
      return ["impressions", "likes", "comments", "shares"];
    case "detailList2":
      return [
        "email",
        "username",
        "password",
        "city",
        "address",
        "address1",
        "address2",
      ];
    case "detailList3":
      return ["profile_url", "username", "platform", "created_by"];
    case "detailList6":
      return ["media_url", "schedule_date_time", "text"];
    case "detailList7":
      return [
        "username",
        "campaign_name",
        "campaign_description",
        "start_date",
        "end_date",
      ];
    default:
      return ["null"];
  }
};
export const getNavNameList = () => {
  return ["page redirect", "popup", "slide in drawer"];
};
export const getViewList = [
  "defaultView",
  "gridView1",
  "gridView2",
  "gridView3",
  "gridView4",
  "gridView5",
  "gridView6",
  "groupedList1",
  "groupedList2",
  "groupedList3",
  "groupedList4",
  "groupedList5",
  "groupedList6",
  "groupedList7",
  "groupedList8",
  "groupedList9",
  "groupedList10",
  "groupedList11",
  "detailList1",
  "detailList2",
  "detailList3",
  "detailList4",
  "detailList5",
  "detailList6",
  "detailList7",
];

export const displayControlList = {
  file: ["image", "audio", "video", "pdf"],
  url: [
    "image",
    "audio",
    "video",
    "Object(ex.pdf)",
    "webpages",
    "Embed audio",
    "Embed video",
  ],
};

export const getRowElement = (
  inputType: string,
  field: any,
  display_control?: string,
  srcData?: string
) => {
  if (inputType === "file") {
    switch (display_control) {
      case "image":
        return (
          <img
            src={srcData}
            alt="row content"
            style={{ width: "50px", height: "50px" }}
          />
        );
      case "audio":
        return (
          <audio controls>
            <source src={srcData} type="audio/mpeg" />
            Your browser does not support the audio element.
          </audio>
        );
      case "video":
        return (
          <video controls>
            <source src={srcData} type="video/mp4" />
            Your browser does not support the video tag.
          </video>
        );
      case "pdf":
        return (
          <iframe src={srcData} style={{ width: "600px", height: "500px" }}>
            Your browser does not support the iframe tag.
          </iframe>
        );
      default:
        break;
    }
  } else if (inputType === "url") {
    switch (display_control) {
      case "image":
        return (
          <img
            src={field}
            alt="row content"
            style={{ width: "50px", height: "50px" }}
          />
        );
      case "audio":
        return (
          <audio controls>
            <source src={field} type="audio/mpeg" />
            Your browser does not support the audio element.
          </audio>
        );
      case "video":
        return (
          <video controls>
            <source src={field} type="video/mp4" />
            Your browser does not support the video tag.
          </video>
        );
      case "Object(ex.pdf)":
        return <object data={field}></object>;
      case "webpages":
        return <iframe src={field}></iframe>;
      case "Embed audio":
        return <embed src={field} type="audio/mpeg" />;
      case "Embed video":
        return <embed src={field} type="video/mp4" />;
      default:
        break;
    }
  } else if (inputType === "rich text editor") {
    return <div dangerouslySetInnerHTML={{ __html: field }} />;
  } else if (inputType === "url") {
    return (
      <iframe
        src={field}
        title="URL Display"
        width="100%"
        height="600px"
        style={{ border: 0 }}
      >
        Your browser does not support iframes.
      </iframe>
    );
  } else {
    return field;
  }
};

const dataCache = {};

export function useRowSelector(inputType, field, display_control, bucket_id) {
  const [srcData, setSrcData] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      if (dataCache[field]) {
        // If the data for this field exists in the cache, use it directly
        setSrcData(dataCache[field]);
      } else if (inputType === "file") {
        const formData = new FormData();
        formData.append("Key", field);
        formData.append("BucketId", bucket_id);
        const response = await GetDataUrl(formData);
        const newData = response.data.document;
        // Cache the fetched data for future use
        dataCache[field] = newData;
        setSrcData(newData);
      }
    };

    fetchData();
  }, [inputType, field, bucket_id]);

  return srcData;
}

export const ErrorControlList = {
  email: "email",
  text: "text",
  url: "url",
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
        default:
          return {
            isValid: true,
          };
      }
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
        default:
          return {
            isValid: true,
          };
      }
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
    case "url":
      var urlRegexp = new RegExp(
        "^" +
          // protocol identifier (optional)
          "(?:(?:https?|ftp)://)" +
          // user:pass authentication (optional)
          "(?:\\S+(?::\\S*)?@)?" +
          "(?:" +
          // IP address (simple regex, not exact)
          "\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}|" +
          // host & domain names, may end with dot
          // can contain dashes, dots, percent, and unicode characters
          "(?:[\\p{L}\\p{N}][-\\p{L}\\p{N}\\.]*(?:[\\p{L}\\p{N}]|\\p{L})\\.?|" +
          // or localhost without domain
          "localhost" +
          ")" +
          // port number (optional)
          "(?::\\d{2,5})?" +
          // resource path (optional)
          "(?:[/?#]\\S*)?" +
          "$",
        "iu"
      );
      let urlvalid = urlRegexp.test(value);
      return urlvalid
        ? {
            isValid: true,
          }
        : {
            isValid: false,
            errorMessage: m2,
          };

    default:
      return {
        isValid: true,
      };
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
  };
}

export const getList = (params: string, fkey?: Boolean) => {
  if (fkey) {
    return ["radio", "dropdown"];
  }
  if (
    params === "int" ||
    params === "decimal" ||
    params === "bigint" ||
    params === "double" ||
    params === "smallint" ||
    params === "tinyint" ||
    params === "enum" ||
    params === "mediumint" ||
    params === "float" ||
    params === "binary" ||
    params === "varbinary" ||
    params === "boolean"
  )
    return ["text"];
  if (
    params === "date" ||
    params === "timestamp" ||
    params === "datetime" ||
    params === "time" ||
    params === "year"
  )
    return ["date", "datetime"];
  if (
    params === "varchar" ||
    params === "text" ||
    params === "char" ||
    params === "blob" ||
    params === "tinyblob" ||
    params === "mediumblob" ||
    params === "longblob" ||
    params === "set"
  )
    return ["text", "file", "rich text editor", "url"];
};

export const SIDEBAR_ITEMS: ISidebar_Items[] = [
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Heading",
      content: <Heading />,
      icon: <TitleIcon className="dnd sidebarIcon" />,
      component_name: "Heading",
      icon_name: "TitleIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "S3bucketDropDownList",
            content: S3bucketDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "s3bucketDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "S3bucketGridView",
            content: S3bucketGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "s3bucketGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "S3bucketListView",
            content: S3bucketListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "s3bucketListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "S3bucketAutoComplete",
            content: S3bucketAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "s3bucketAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "S3bucketQueryBuilder",
            content: S3bucketQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "s3bucketQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "S3bucket",
      content: S3bucket,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "s3bucket",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementtypeDropDownList",
            content: EngagementtypeDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementtypeDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementtypeGridView",
            content: EngagementtypeGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementtypeGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementtypeListView",
            content: EngagementtypeListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementtypeListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementtypeAutoComplete",
            content: EngagementtypeAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementtypeAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementtypeQueryBuilder",
            content: EngagementtypeQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementtypeQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Engagementtype",
      content: Engagementtype,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "engagementtype",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentstatusDropDownList",
            content: ContentstatusDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentstatusDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentstatusGridView",
            content: ContentstatusGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentstatusGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentstatusListView",
            content: ContentstatusListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentstatusListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentstatusAutoComplete",
            content: ContentstatusAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentstatusAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentstatusQueryBuilder",
            content: ContentstatusQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentstatusQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Contentstatus",
      content: Contentstatus,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "contentstatus",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "UsersDropDownList",
            content: UsersDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "usersDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "UsersGridView",
            content: UsersGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "usersGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "UsersListView",
            content: UsersListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "usersListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "UsersAutoComplete",
            content: UsersAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "usersAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "UsersQueryBuilder",
            content: UsersQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "usersQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Users",
      content: Users,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "users",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "SocialmediaaccountsDropDownList",
            content: SocialmediaaccountsDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "socialmediaaccountsDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "SocialmediaaccountsGridView",
            content: SocialmediaaccountsGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "socialmediaaccountsGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "SocialmediaaccountsListView",
            content: SocialmediaaccountsListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "socialmediaaccountsListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "SocialmediaaccountsAutoComplete",
            content: SocialmediaaccountsAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "socialmediaaccountsAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "SocialmediaaccountsQueryBuilder",
            content: SocialmediaaccountsQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "socialmediaaccountsQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Socialmediaaccounts",
      content: Socialmediaaccounts,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "socialmediaaccounts",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentDropDownList",
            content: ContentDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentGridView",
            content: ContentGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentListView",
            content: ContentListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentAutoComplete",
            content: ContentAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "ContentQueryBuilder",
            content: ContentQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "contentQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Content",
      content: Content,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "content",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementDropDownList",
            content: EngagementDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementGridView",
            content: EngagementGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementListView",
            content: EngagementListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementAutoComplete",
            content: EngagementAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "EngagementQueryBuilder",
            content: EngagementQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "engagementQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Engagement",
      content: Engagement,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "engagement",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AnalyticsDropDownList",
            content: AnalyticsDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "analyticsDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AnalyticsGridView",
            content: AnalyticsGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "analyticsGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AnalyticsListView",
            content: AnalyticsListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "analyticsListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AnalyticsAutoComplete",
            content: AnalyticsAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "analyticsAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "AnalyticsQueryBuilder",
            content: AnalyticsQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "analyticsQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Analytics",
      content: Analytics,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "analytics",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "InfluencerDropDownList",
            content: InfluencerDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "influencerDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "InfluencerGridView",
            content: InfluencerGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "influencerGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "InfluencerListView",
            content: InfluencerListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "influencerListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "InfluencerAutoComplete",
            content: InfluencerAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "influencerAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "InfluencerQueryBuilder",
            content: InfluencerQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "influencerQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Influencer",
      content: Influencer,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "influencer",
      icon_name: "ApiIcon",
    },
  },
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CollaborationDropDownList",
            content: CollaborationDropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "collaborationDropDownList",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CollaborationGridView",
            content: CollaborationGridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "collaborationGridView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CollaborationListView",
            content: CollaborationListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "collaborationListView",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CollaborationAutoComplete",
            content: CollaborationAutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "collaborationAutoComplete",
            icon_name: "ApiIcon",
        },
    },*/
  /*{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {
            type: "CollaborationQueryBuilder",
            content: CollaborationQueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: "collaborationQueryBuilder",
            icon_name: "ApiIcon",
        },
    },*/
  {
    id: shortid.generate(),
    type: SIDEBAR_ITEM,
    component: {
      type: "Collaboration",
      content: Collaboration,
      icon: <ApiIcon className="dnd sidebarIcon" />,
      component_name: "collaboration",
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

export const functionTOmap = (
  component,
  config,
  openLink?,
  id?,
  handleConfigurationChange?
) => {
  ////console.log("component from functionToMap :", component)
  switch (component) {
    case "Heading":
      return <Heading config={config} />;
    case "Card":
      return <Card config={config} />;
    case "SyncFusion GRID":
      return <Grid />;
    /*case "S3bucketDropDownList":
            return < S3bucketDropDownList config={config} />
            break;*/
    /*case "S3bucketGridView":
            return < S3bucketGridView config={config} />
            break; */
    /*case "S3bucketListView":
            return < S3bucketListView config={config} />
            break; */
    /*case "S3bucketAutoComplete":
            return < S3bucketAutoComplete config={config} />
            break; */
    /*case "S3bucketQueryBuilder":
            return < S3bucketQueryBuilder config={config} />
            break;*/
    case "S3bucket":
      return (
        <S3bucket
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "EngagementtypeDropDownList":
            return < EngagementtypeDropDownList config={config} />
            break;*/
    /*case "EngagementtypeGridView":
            return < EngagementtypeGridView config={config} />
            break; */
    /*case "EngagementtypeListView":
            return < EngagementtypeListView config={config} />
            break; */
    /*case "EngagementtypeAutoComplete":
            return < EngagementtypeAutoComplete config={config} />
            break; */
    /*case "EngagementtypeQueryBuilder":
            return < EngagementtypeQueryBuilder config={config} />
            break;*/
    case "Engagementtype":
      return (
        <Engagementtype
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "ContentstatusDropDownList":
            return < ContentstatusDropDownList config={config} />
            break;*/
    /*case "ContentstatusGridView":
            return < ContentstatusGridView config={config} />
            break; */
    /*case "ContentstatusListView":
            return < ContentstatusListView config={config} />
            break; */
    /*case "ContentstatusAutoComplete":
            return < ContentstatusAutoComplete config={config} />
            break; */
    /*case "ContentstatusQueryBuilder":
            return < ContentstatusQueryBuilder config={config} />
            break;*/
    case "Contentstatus":
      return (
        <Contentstatus
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "UsersDropDownList":
            return < UsersDropDownList config={config} />
            break;*/
    /*case "UsersGridView":
            return < UsersGridView config={config} />
            break; */
    /*case "UsersListView":
            return < UsersListView config={config} />
            break; */
    /*case "UsersAutoComplete":
            return < UsersAutoComplete config={config} />
            break; */
    /*case "UsersQueryBuilder":
            return < UsersQueryBuilder config={config} />
            break;*/
    case "Users":
      return (
        <Users
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "SocialmediaaccountsDropDownList":
            return < SocialmediaaccountsDropDownList config={config} />
            break;*/
    /*case "SocialmediaaccountsGridView":
            return < SocialmediaaccountsGridView config={config} />
            break; */
    /*case "SocialmediaaccountsListView":
            return < SocialmediaaccountsListView config={config} />
            break; */
    /*case "SocialmediaaccountsAutoComplete":
            return < SocialmediaaccountsAutoComplete config={config} />
            break; */
    /*case "SocialmediaaccountsQueryBuilder":
            return < SocialmediaaccountsQueryBuilder config={config} />
            break;*/
    case "Socialmediaaccounts":
      return (
        <Socialmediaaccounts
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "ContentDropDownList":
            return < ContentDropDownList config={config} />
            break;*/
    /*case "ContentGridView":
            return < ContentGridView config={config} />
            break; */
    /*case "ContentListView":
            return < ContentListView config={config} />
            break; */
    /*case "ContentAutoComplete":
            return < ContentAutoComplete config={config} />
            break; */
    /*case "ContentQueryBuilder":
            return < ContentQueryBuilder config={config} />
            break;*/
    case "Content":
      return (
        <Content
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "EngagementDropDownList":
            return < EngagementDropDownList config={config} />
            break;*/
    /*case "EngagementGridView":
            return < EngagementGridView config={config} />
            break; */
    /*case "EngagementListView":
            return < EngagementListView config={config} />
            break; */
    /*case "EngagementAutoComplete":
            return < EngagementAutoComplete config={config} />
            break; */
    /*case "EngagementQueryBuilder":
            return < EngagementQueryBuilder config={config} />
            break;*/
    case "Engagement":
      return (
        <Engagement
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "AnalyticsDropDownList":
            return < AnalyticsDropDownList config={config} />
            break;*/
    /*case "AnalyticsGridView":
            return < AnalyticsGridView config={config} />
            break; */
    /*case "AnalyticsListView":
            return < AnalyticsListView config={config} />
            break; */
    /*case "AnalyticsAutoComplete":
            return < AnalyticsAutoComplete config={config} />
            break; */
    /*case "AnalyticsQueryBuilder":
            return < AnalyticsQueryBuilder config={config} />
            break;*/
    case "Analytics":
      return (
        <Analytics
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "InfluencerDropDownList":
            return < InfluencerDropDownList config={config} />
            break;*/
    /*case "InfluencerGridView":
            return < InfluencerGridView config={config} />
            break; */
    /*case "InfluencerListView":
            return < InfluencerListView config={config} />
            break; */
    /*case "InfluencerAutoComplete":
            return < InfluencerAutoComplete config={config} />
            break; */
    /*case "InfluencerQueryBuilder":
            return < InfluencerQueryBuilder config={config} />
            break;*/
    case "Influencer":
      return (
        <Influencer
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );
    /*case "CollaborationDropDownList":
            return < CollaborationDropDownList config={config} />
            break;*/
    /*case "CollaborationGridView":
            return < CollaborationGridView config={config} />
            break; */
    /*case "CollaborationListView":
            return < CollaborationListView config={config} />
            break; */
    /*case "CollaborationAutoComplete":
            return < CollaborationAutoComplete config={config} />
            break; */
    /*case "CollaborationQueryBuilder":
            return < CollaborationQueryBuilder config={config} />
            break;*/
    case "Collaboration":
      return (
        <Collaboration
          config={config}
          openLink={openLink}
          id={id}
          handleConfigurationChange={handleConfigurationChange}
        />
      );

    case "Button":
      return <CustomButton config={config} />;
    case "input":
      return <CustomTextField config={config} />;
    case "MenuBar":
      return <MenuBar />;
    case "GridList":
      return <DataGridlist />;
    case "List":
      return <DropDownList />;
    case "name":
      return <h3>Some Name</h3>;
    case "email":
      return "Some email";
    case "phone":
      return "Some phone";
    case "image":
      return "Some image";
    default:
      return <p>Cant't find any Component</p>;
  }
};
export const mapNametoComponent = {
  Grid: Grid,
  S3bucketDropDownList: S3bucketDropDownList,
  S3bucketGridView: S3bucketGridView,
  S3bucketListView: S3bucketListView,
  S3bucketAutoComplete: S3bucketAutoComplete,
  S3bucketQueryBuilder: S3bucketQueryBuilder,
  S3bucket: S3bucket,
  EngagementtypeDropDownList: EngagementtypeDropDownList,
  EngagementtypeGridView: EngagementtypeGridView,
  EngagementtypeListView: EngagementtypeListView,
  EngagementtypeAutoComplete: EngagementtypeAutoComplete,
  EngagementtypeQueryBuilder: EngagementtypeQueryBuilder,
  Engagementtype: Engagementtype,
  ContentstatusDropDownList: ContentstatusDropDownList,
  ContentstatusGridView: ContentstatusGridView,
  ContentstatusListView: ContentstatusListView,
  ContentstatusAutoComplete: ContentstatusAutoComplete,
  ContentstatusQueryBuilder: ContentstatusQueryBuilder,
  Contentstatus: Contentstatus,
  UsersDropDownList: UsersDropDownList,
  UsersGridView: UsersGridView,
  UsersListView: UsersListView,
  UsersAutoComplete: UsersAutoComplete,
  UsersQueryBuilder: UsersQueryBuilder,
  Users: Users,
  SocialmediaaccountsDropDownList: SocialmediaaccountsDropDownList,
  SocialmediaaccountsGridView: SocialmediaaccountsGridView,
  SocialmediaaccountsListView: SocialmediaaccountsListView,
  SocialmediaaccountsAutoComplete: SocialmediaaccountsAutoComplete,
  SocialmediaaccountsQueryBuilder: SocialmediaaccountsQueryBuilder,
  Socialmediaaccounts: Socialmediaaccounts,
  ContentDropDownList: ContentDropDownList,
  ContentGridView: ContentGridView,
  ContentListView: ContentListView,
  ContentAutoComplete: ContentAutoComplete,
  ContentQueryBuilder: ContentQueryBuilder,
  Content: Content,
  EngagementDropDownList: EngagementDropDownList,
  EngagementGridView: EngagementGridView,
  EngagementListView: EngagementListView,
  EngagementAutoComplete: EngagementAutoComplete,
  EngagementQueryBuilder: EngagementQueryBuilder,
  Engagement: Engagement,
  AnalyticsDropDownList: AnalyticsDropDownList,
  AnalyticsGridView: AnalyticsGridView,
  AnalyticsListView: AnalyticsListView,
  AnalyticsAutoComplete: AnalyticsAutoComplete,
  AnalyticsQueryBuilder: AnalyticsQueryBuilder,
  Analytics: Analytics,
  InfluencerDropDownList: InfluencerDropDownList,
  InfluencerGridView: InfluencerGridView,
  InfluencerListView: InfluencerListView,
  InfluencerAutoComplete: InfluencerAutoComplete,
  InfluencerQueryBuilder: InfluencerQueryBuilder,
  Influencer: Influencer,
  CollaborationDropDownList: CollaborationDropDownList,
  CollaborationGridView: CollaborationGridView,
  CollaborationListView: CollaborationListView,
  CollaborationAutoComplete: CollaborationAutoComplete,
  CollaborationQueryBuilder: CollaborationQueryBuilder,
  Collaboration: Collaboration,

  DataGridlist: <DataGridlist />,
  NestedList: <NestedList />,
  CustomButton: <CustomButton />,
  CustomTextFields: <CustomTextField />,
  "Some name": "Some name",
  "Some email": "Some email",
  "Some phone": "Some phone",
  "Some image": "Some image",
};

export const SyncFusion_Component_List = ["SyncFusion GRID"];
export const MUI_Component_List = [
  "DataGridlist",
  "NestedList",
  "CustomButton",
  "CustomTextFields",
];

export const Configurations = {
  Button: {
    innerContent: {
      "input-type": "text",
    },
    type: {
      "input-type": "list",
      options: ["Button", "Submit", "Reset"],
    },
    cssClass: {
      "input-type": "list",
      options: ["e-primary", "e-success", "e-info", "e-warning", "e-danger"],
    },
    onClick: {
      "input-type": "text",
    },
  },
  input: {
    innerContent: {
      "input-type": "text",
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
    innerContent: {
      "input-type": "text",
    },
    backgroundColor: {
      "input-type": "global-configuration",
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
      "input-type": "global-configuration",
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
    SidebarMenuTextColor: {
      "input-type": "global-configuration",
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
    SidebarMenuTextBackgroundColor: {
      "input-type": "global-configuration",
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
    SidebarSubMenuTextColor: {
      "input-type": "global-configuration",
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
    SidebarSubMenuTextBackgroundColor: {
      "input-type": "global-configuration",
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

  List: {
    primaryKeyList: {
      "input-type": "list",
      options: [
        "backend_stack_id",
        "backend_stack_name",
        "createdBy",
        "modifiedBy",
        "createdAt",
        "modifiedAt",
        "isActive",
      ],
    },
    secondaryKeyList: {
      "input-type": "list",
      options: [
        "backend_stack_id",
        "backend_stack_name",
        "createdBy",
        "modifiedBy",
        "createdAt",
        "modifiedAt",
        "isActive",
      ],
    },
    functionName: {
      "input-type": "list",
      options: [
        "getAllBackend_Stacks",
        "getOneBackend_Stacks",
        "searchBackend_Stacks",
        "addBackend_Stacks",
        "updateBackend_Stacks",
        "deleteBackend_Stacks",
      ],
    },
  },
  "SyncFusion GRID": {
    primaryKeyList: {
      "input-type": "list",
      options: [
        "backend_stack_id",
        "backend_stack_name",
        "createdBy",
        "modifiedBy",
        "createdAt",
        "modifiedAt",
        "isActive",
      ],
    },
    modelName: {
      "input-type": "list",
      options: ["", "Backend_Stacks"],
    },
    tableName: {
      "input-type": "list",
      options: ["", "backend_stacks"],
    },
    functionName: {
      "input-type": "list",
      options: [
        "getAllBackend_Stacks",
        "getOneBackend_Stacks",
        "searchBackend_Stacks",
        "addBackend_Stacks",
        "updateBackend_Stacks",
        "deleteBackend_Stacks",
      ],
    },
  },
  Card: {
    header: {
      "input-type": "text",
    },
    content: {
      "input-type": "text",
    },
  },
  S3bucket: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "bucket_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "bucket_name",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "bucket_url",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": ["bucket_id", "bucket_name", "bucket_url"],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": ["bucket_id", "bucket_name", "bucket_url"],
      "nav-list": getNavNameList(),
    },
  },
  Engagementtype: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "engagement_type_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "type_name",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": ["engagement_type_id", "type_name"],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": ["engagement_type_id", "type_name"],
      "nav-list": getNavNameList(),
    },
  },
  Contentstatus: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "content_status_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "status_name",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": ["content_status_id", "status_name"],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": ["content_status_id", "status_name"],
      "nav-list": getNavNameList(),
    },
  },
  Users: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "user_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "username",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "email",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "password",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "user_id",
        "username",
        "email",
        "password",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "user_id",
        "username",
        "email",
        "password",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
  Socialmediaaccounts: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "account_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "user_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "platform",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "access_token",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "username",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "profile_url",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "account_id",
        "user_id",
        "platform",
        "access_token",
        "username",
        "profile_url",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "account_id",
        "user_id",
        "platform",
        "access_token",
        "username",
        "profile_url",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
  Content: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "content_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "user_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "text",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "media_url",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "schedule_date_time",
          pkey: false,
          fkey: false,
          icontrol: getList("date"),
          type: "date",
          slice: "",
        },
        {
          name: "engagement_type_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "engagementtype",
        },
        {
          name: "content_status_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "contentstatus",
        },
        {
          name: "account_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "socialmediaaccounts",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "content_id",
        "user_id",
        "text",
        "media_url",
        "schedule_date_time",
        "engagement_type_id",
        "content_status_id",
        "account_id",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "content_id",
        "user_id",
        "text",
        "media_url",
        "schedule_date_time",
        "engagement_type_id",
        "content_status_id",
        "account_id",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
  Engagement: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "engagement_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "content_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "content",
        },
        {
          name: "engagement_type_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "engagementtype",
        },
        {
          name: "date_time",
          pkey: false,
          fkey: false,
          icontrol: getList("date"),
          type: "date",
          slice: "",
        },
        {
          name: "user_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "engagement_id",
        "content_id",
        "engagement_type_id",
        "date_time",
        "user_id",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "engagement_id",
        "content_id",
        "engagement_type_id",
        "date_time",
        "user_id",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
  Analytics: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "analytics_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "content_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "content",
        },
        {
          name: "views",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "likes",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "comments",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "shares",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "clicks",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "impressions",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "analytics_id",
        "content_id",
        "views",
        "likes",
        "comments",
        "shares",
        "clicks",
        "impressions",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "analytics_id",
        "content_id",
        "views",
        "likes",
        "comments",
        "shares",
        "clicks",
        "impressions",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
  Influencer: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "influencer_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "username",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "email",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "profile_url",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "follower_count",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "influencer_id",
        "username",
        "email",
        "profile_url",
        "follower_count",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "influencer_id",
        "username",
        "email",
        "profile_url",
        "follower_count",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
  Collaboration: {
    innerContent: { "input-type": "text" },
    tableHeading: { "input-type": "text" },
    addFormHeading: { "input-type": "text" },
    editFormHeading: { "input-type": "text" },
    headerSize: {
      "input-type": "list",
      options: ["h1", "h2", "h3", "h4", "h5", "h6"],
    },
    backgroundColor: { "input-type": "heading-color" },
    color: { "input-type": "heading-color" },
    tableHeadBackgroundColor: { "input-type": "table-head-color" },
    HeadColor: { "input-type": "table-head-color" },
    tableBackgroundColor: { "input-type": "table-color" },
    HeadRowBackgroundColor: { "input-type": "table-color" },
    HeadRowColor: { "input-type": "row-color" },
    RowBackgroundColor: { "input-type": "row-color" },
    RowColor: { "input-type": "row-color" },
    fontFamily: {
      "input-type": "list",
      options: [
        "Arial",
        "Helvetica",
        "Verdana",
        "Georgia",
        "CourierNew",
        "cursive",
      ],
    },
    columns: {
      "input-type": "group",
      "columns-list": [
        {
          name: "collaboration_id",
          pkey: true,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "user_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "influencer_id",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "influencer",
        },
        {
          name: "campaign_name",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "campaign_description",
          pkey: false,
          fkey: false,
          icontrol: getList("varchar"),
          type: "varchar",
          slice: "",
        },
        {
          name: "start_date",
          pkey: false,
          fkey: false,
          icontrol: getList("date"),
          type: "date",
          slice: "",
        },
        {
          name: "end_date",
          pkey: false,
          fkey: false,
          icontrol: getList("date"),
          type: "date",
          slice: "",
        },
        {
          name: "created_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "modified_at",
          pkey: false,
          fkey: false,
          icontrol: getList("timestamp"),
          type: "timestamp",
          slice: "",
        },
        {
          name: "is_Active",
          pkey: false,
          fkey: false,
          icontrol: getList("int"),
          type: "int",
          slice: "",
        },
        {
          name: "created_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
        {
          name: "modified_by",
          pkey: false,
          fkey: true,
          icontrol: getList("int", true),
          type: "int",
          slice: "users",
        },
      ],
      "error-control-list": ["password", "email", "text", "number", "url"],
    },
    row: {
      "input-type": "filter-form",
      "columns-list": [
        "collaboration_id",
        "user_id",
        "influencer_id",
        "campaign_name",
        "campaign_description",
        "start_date",
        "end_date",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "column-condition": ["==", "!=", ">", "<"],
    },
    navConfig: {
      "input-type": "nav",
      "columns-list": [
        "collaboration_id",
        "user_id",
        "influencer_id",
        "campaign_name",
        "campaign_description",
        "start_date",
        "end_date",
        "created_at",
        "modified_at",
        "is_Active",
        "created_by",
        "modified_by",
      ],
      "nav-list": getNavNameList(),
    },
  },
};

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
