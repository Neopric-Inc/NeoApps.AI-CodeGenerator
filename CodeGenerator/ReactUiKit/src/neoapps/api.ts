// Thin client for the generated .NET API (responses are { code, message, document }).
// The session is an HttpOnly cookie set by POST /Token: scripts never see the token, so nothing is kept in localStorage.
const LEGACY_TOKEN_KEY = "neoapps.token";

export const apiBase = (): string => {
  const base = process.env.REACT_APP_API_BASE_URL || "http://localhost:5000/v1/api/";
  return base.endsWith("/") ? base : base + "/";
};

/** @deprecated The session lives in an HttpOnly cookie; kept so older custom code still compiles. Always null. */
export const getToken = (): string | null => null;

/** @deprecated Clears a token left in localStorage by older versions. */
export const setToken = (_token?: string | null) => {
  try {
    localStorage.removeItem(LEGACY_TOKEN_KEY);
  } catch {
    /* storage unavailable */
  }
};
setToken(null);

/** fetch() against the API with the session cookie and the client header (required for writes). */
export function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers || {});
  headers.set("X-NeoApps-Client", "web");
  return fetch(path.startsWith("http") || path.startsWith("/") ? path : apiBase() + path, { ...init, headers, credentials: "include" });
}

export class ApiError extends Error {
  status: number;
  fieldErrors?: Record<string, string[]>;
  constructor(status: number, message: string, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export async function api<T = any>(method: string, path: string, body?: any): Promise<T> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  const res = await apiFetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text();
  let json: any = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = null;
  }
  if (res.status === 401 && path !== "Token") {
    throw new ApiError(401, "You don't have permission for this action.");
  }
  // data-rule violations (tenancy, lifecycle steps, read-only data, plan limits) come with a readable message
  if (res.status === 403) throw new ApiError(403, (json && typeof json.message === "string" && json.message) || "Administrators only.");
  if (res.status === 400 && json && json.errors && typeof json.errors === "object") {
    // ASP.NET model validation: {"errors": {"field": ["The field is required."]}}
    const names = Object.keys(json.errors).join(", ");
    throw new ApiError(400, `Please check: ${names}`, json.errors);
  }
  if (!res.ok) {
    const msg = (json && (json.message === "Exception" ? json.document : json.message)) || res.statusText || "Request failed";
    throw new ApiError(res.status, typeof msg === "string" ? msg : JSON.stringify(msg));
  }
  return json as T;
}

export interface Page<T = Record<string, any>> {
  records: T[];
  total: number;
}

/** Lists records (page is 1-based). Missing data comes back as code 0 "No Record Found". */
export async function listRecords(table: string, page = 1, pageSize = 25, orderBy?: string, search?: string): Promise<Page> {
  const params = new URLSearchParams({ page: String(page), itemsPerPage: String(pageSize) });
  if (orderBy) params.set("orderBy", orderBy);
  let path = `${table}?${params}`;
  if (search && search.trim()) {
    params.set("searchKey", search.trim());
    path = `${table}/search?${params}`;
  }
  const res = await api<any>("GET", path);
  const doc = res && res.code === 1 ? res.document : null;
  return { records: doc?.records || [], total: doc?.totalRecords || 0 };
}

export const getRecord = async (table: string, id: any) => {
  const res = await api<any>("GET", `${table}/${encodeURIComponent(id)}`);
  return res?.code === 1 ? res.document : null;
};
export const createRecord = (table: string, data: any) => api<any>("POST", table, data);
export const updateRecord = (table: string, id: any, data: any) => api<any>("PUT", `${table}/${encodeURIComponent(id)}`, data);
export const deleteRecord = (table: string, id: any) => api<any>("DELETE", `${table}/${encodeURIComponent(id)}`);

/** Number of active rows in `table` where `column` equals `value` (generated /filter endpoint, condition 5 = Equals). */
export async function countWhere(table: string, column: string, value: any): Promise<number> {
  const res = await api<any>("POST", `${table}/filter?page=1&itemsPerPage=1`, [{ columnName: column, columnCondition: 5, columnValue: String(value) }]).catch(() => null);
  return res && res.code === 1 ? res.document?.totalRecords || 0 : 0;
}

export async function login(username: string, password: string): Promise<void> {
  const res = await api<any>("POST", "Token", { username, password });
  if (res?.code !== 1) throw new ApiError(401, "Invalid username or password."); // the cookie is set by the response
}
