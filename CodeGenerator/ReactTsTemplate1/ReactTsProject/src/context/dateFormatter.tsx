import React from "react";

type DateFormatOption =
  | "Month"
  | "Day"
  | "Day-Month"
  | "Day-Month-Year"
  | "Year"
  | "Month-Year"
  | "Month-Day-Year"
  | "Year-Month-Day"
  | "Month-Day"
  | "Year-Day"
  | "Month-Day-Year-Hour"
  | "Hour-Minute"
  | "Month-Year-Hour-Minute";

export const formatDate = (inputDate: string, format: DateFormatOption): string => {
  const dateParts = inputDate.split(/[^\d]+/).filter(Boolean);

  if (dateParts.length < 3) {
    return "Invalid Date";
  }

  const day = parseInt(dateParts[0], 10);
  const month = parseInt(dateParts[1], 10);
  const year = parseInt(dateParts[2], 10);

  if (isNaN(day) || isNaN(month) || isNaN(year)) {
    return "Invalid Date";
  }

  const date = new Date(year, month - 1, day);
  const hour = date.getHours();
  const minute = date.getMinutes();

  switch (format) {
    case "Month":
      return date.toLocaleString("default", { month: "short" }); // e.g., "Jan"
    case "Day":
      return day.toString().padStart(2, "0"); // e.g., "09"
    case "Day-Month":
      return `${day} ${date.toLocaleString("default", { month: "short" })}`; // e.g., "09 Jan"
    case "Day-Month-Year":
      return `${day} ${date.toLocaleString("default", {
        month: "short",
      })},${year}`; // e.g., "09 Jan, 2023"
    case "Year":
      return year.toString(); // e.g., "2023"
    case "Month-Year":
      return `${date.toLocaleString("default", { month: "short" })} ${year}`; // e.g., "Jan 2023"
    case "Month-Day-Year":
      return `${date.toLocaleString("default", {
        month: "short",
      })} ${day}, ${year}`; // e.g., "Jan 09, 2023"
    case "Year-Month-Day":
      return `${year}-${month.toString().padStart(2, "0")}-${day
        .toString()
        .padStart(2, "0")}`; // e.g., "2023-01-09"
    case "Month-Day":
      return `${date.toLocaleString("default", { month: "short" })} ${day}`; // e.g., "Jan 09"
    case "Year-Day":
      return `${year}-${day.toString().padStart(2, "0")}`; // e.g., "2023-09"
    case "Month-Day-Year-Hour":
      return `${date.toLocaleString("default", {
        month: "short",
      })} ${day}, ${year} ${hour}:${minute.toString().padStart(2, "0")}`; // e.g., "Jan 09, 2023 08:45"
    case "Hour-Minute":
      return `${hour}:${minute.toString().padStart(2, "0")}`; // e.g., "08:45"
    case "Month-Year-Hour-Minute":
      return `${date.toLocaleString("default", {
        month: "short",
      })} ${year} ${hour}:${minute.toString().padStart(2, "0")}`; // e.g., "Jan 2023 08:45"
    default:
      return "Invalid Format";
  }
};
