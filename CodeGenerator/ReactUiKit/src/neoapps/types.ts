// Shapes of the two runtime files the generator writes into public/:
//   schema.json     - tables, columns and relations read from the database (do not edit by hand)
//   ui.config.json  - optional overrides for labels, menus, list columns, widgets... (edit freely)
// Admins can also edit the UI config in the app (Customize UI); that copy is stored server-side and wins.

export interface ColumnSchema {
  name: string;
  dataType: string; // int, varchar, text, date, datetime, decimal, tinyint...
  columnType: string; // full type, e.g. tinyint(1), enum('a','b'), varchar(255)
  nullable: boolean;
  hasDefault: boolean;
  defaultValue?: string | null; // raw MySQL column default, e.g. 'pharma', 0, CURRENT_TIMESTAMP
  maxLength?: number | null;
  isPrimaryKey: boolean;
  isAutoIncrement: boolean;
  references?: { table: string; column: string } | null;
}

export interface TableSchema {
  name: string;
  primaryKey: string;
  columns: ColumnSchema[];
}

export interface AppSchema {
  app: string;
  generatedAt?: string;
  tables: TableSchema[];
}

export type Widget =
  | "text" | "textarea" | "number" | "currency" | "percent" | "date" | "datetime"
  | "checkbox" | "select" | "reference" | "email" | "url" | "color";

export interface FieldConfig {
  label?: string;
  widget?: Widget;
  options?: string[]; // for select
  colors?: Record<string, "default" | "primary" | "secondary" | "success" | "warning" | "error" | "info">;
  hidden?: boolean; // hidden everywhere
  hideInForm?: boolean;
  readOnly?: boolean;
  required?: boolean;
  help?: string;
  placeholder?: string;
  defaultValue?: any;
}

export interface TableConfig {
  label?: string;
  singular?: string;
  icon?: string;
  description?: string;
  displayField?: string;
  listColumns?: string[];
  formFields?: string[]; // order of fields in the form
  defaultSort?: string; // "column|ASC"
  children?: string[]; // child tables shown on the record page
  hidden?: boolean; // hide from the menu
  fields?: Record<string, FieldConfig>;
}

export interface MenuGroup {
  group: string;
  items: string[];
}

export interface DashboardCard {
  table: string;
  label?: string;
}

export interface UiConfig {
  appName?: string;
  tagline?: string;
  theme?: { primary?: string; secondary?: string };
  menu?: MenuGroup[];
  dashboard?: { cards?: DashboardCard[]; recentTable?: string };
  tables?: Record<string, TableConfig>;
  /** Roles seeded when the database is created: {"Viewer": {"*": "r"}} (c/r/u/d). Read by the generator only. */
  roles?: Record<string, Record<string, string>>;
  /** AI assistant settings, read by the API (instructions, tools, workflows) and the UI. */
  ai?: Record<string, any>;
}

// Fully-resolved view of a table (schema + defaults + config) used by the pages.
export interface ResolvedField extends FieldConfig {
  name: string;
  label: string;
  widget: Widget;
  column: ColumnSchema;
  required: boolean;
}

export interface ResolvedTable {
  name: string;
  primaryKey: string;
  label: string;
  singular: string;
  icon: string;
  description?: string;
  displayField: string;
  listColumns: string[];
  formFields: string[];
  defaultSort?: string;
  children: string[];
  hidden: boolean;
  fields: Record<string, ResolvedField>;
  schema: TableSchema;
}

export interface Permission {
  read: boolean;
  create: boolean;
  update: boolean;
  delete: boolean;
}

export interface Me {
  user_id: number;
  username: string;
  role_id: number;
  role_name: string;
  tenant: string;
  is_admin: boolean;
  permissions: Record<string, Permission>;
}
