// Client for the AI endpoints of the generated API (/v1/api/Ai/...). Responses are plain JSON; errors are {message}.
import { apiBase, ApiError, getToken } from "../api";

export interface AiWorkflow {
  id: string;
  label: string;
  description?: string;
  /** Source table: the workflow runs on one record of it. Empty = runs globally. */
  table?: string;
  /** Shown only when the record's column matches, e.g. {"category": "Protocol"}. */
  when?: Record<string, string>;
  targets: string[];
}

export interface AiStatus {
  enabled: boolean;
  /** File uploads stay available when AI is limited to other roles (ai.roles). */
  files?: boolean;
  configured: boolean;
  model: string;
  assistantName: string;
  documentTables: string[];
  suggestions: string[];
  workflows: AiWorkflow[];
}

export interface Citation {
  id: string;
  file_id: number;
  file_name: string;
  page: number;
  table: string;
  record_id: string;
  record_label: string;
  excerpt: string;
}

export interface AiMessage {
  message_id?: number;
  role: "user" | "assistant";
  content: string;
  citations?: Citation[];
  tools?: string[];
  pending?: boolean;
  error?: boolean;
}

export interface AiFile {
  file_id: number;
  file_name: string;
  content_type?: string;
  size_bytes: number;
  pages?: number;
  status: "indexing" | "indexed" | "needs_ocr" | "reading" | "stored" | "no_text" | "failed";
  error?: string;
  createdBy: string;
  createdAt: string;
}

export interface ProposedRow {
  ref: string;
  table: string;
  values: Record<string, any>;
}

export interface WorkflowPreview {
  workflow: string;
  record_id?: string;
  summary: string;
  records: ProposedRow[];
  warnings: string[];
}

async function call<T>(method: string, path: string, body?: any, form?: FormData): Promise<T> {
  const headers: Record<string, string> = {};
  if (!form) headers["Content-Type"] = "application/json";
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  const res = await fetch(apiBase() + "Ai/" + path, { method, headers, body: form ?? (body === undefined ? undefined : JSON.stringify(body)) });
  const text = await res.text();
  let json: any = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = null;
  }
  if (res.status === 401) throw new ApiError(401, "Your session has expired. Sign in again.");
  if (res.status === 413) throw new ApiError(413, "That file is too large (250 MB max). Split it or upload a ZIP of smaller files.");
  if (!res.ok) throw new ApiError(res.status, json?.message || res.statusText || "Request failed");
  return json as T;
}

export const aiStatus = () => call<AiStatus>("GET", "status");

export const ask = (question: string, conversationId?: number, table?: string, recordId?: string) =>
  call<{ conversation_id: number; answer: string; citations: Citation[]; tools: string[] }>("POST", "ask", {
    question, conversationId, table, recordId,
  });

export const conversations = () =>
  call<{ conversation_id: number; title: string; context_table?: string; context_record?: string; modifiedAt: string }[]>("GET", "conversations");
export const conversationMessages = (id: number) => call<AiMessage[]>("GET", `conversations/${id}`);
export const deleteConversation = (id: number) => call("DELETE", `conversations/${id}`);

export const listFiles = (table: string, recordId: string) =>
  call<AiFile[]>("GET", `files?table=${encodeURIComponent(table)}&recordId=${encodeURIComponent(recordId)}`);
export function uploadFile(table: string, recordId: string, file: File) {
  const form = new FormData();
  form.append("file", file);
  form.append("table", table);
  form.append("recordId", recordId);
  return call<AiFile>("POST", "files", undefined, form);
}
export const deleteFile = (id: number) => call("DELETE", `files/${id}`);

/** Opens a stored file in a new tab (fetched with the bearer token, so it can't be a plain link). */
/**
 * Opens an uploaded file. PDFs, images and text are shown in a new tab (embedded, so every browser displays them
 * instead of downloading); other types (Word, Excel, ZIP...) download.
 */
export async function openFile(id: number, name: string) {
  const tab = window.open("", "_blank");
  if (tab) { tab.document.title = name; tab.document.body.innerHTML = '<p style="font:14px system-ui,sans-serif;padding:16px;color:#555">Opening\u2026</p>'; }
  const res = await fetch(apiBase() + `Ai/files/${id}/content`, { headers: { Authorization: `Bearer ${getToken()}` } });
  if (!res.ok) {
    tab?.close();
    throw new ApiError(res.status, "Couldn't open the file.");
  }
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const type = blob.type || "";
  if (tab && /^(application\/pdf|image\/|text\/)/.test(type)) {
    const d = tab.document;
    d.title = name;
    d.body.innerHTML = "";
    d.body.style.margin = "0";
    const bar = d.createElement("div");
    bar.setAttribute("style", "font:13px system-ui,sans-serif;padding:8px 12px;border-bottom:1px solid #ddd;display:flex;gap:12px;align-items:center");
    const title = d.createElement("b");
    title.textContent = name;
    const dl = d.createElement("a");
    dl.href = url;
    dl.download = name;
    dl.textContent = "Download";
    bar.append(title, dl);
    const view = type.startsWith("image/") ? d.createElement("img") : d.createElement("iframe");
    view.setAttribute("src", url);
    view.setAttribute("style", type.startsWith("image/") ? "display:block;max-width:100%;margin:12px auto" : "border:0;width:100%;height:calc(100vh - 40px);display:block");
    d.body.append(bar, view);
    setTimeout(() => URL.revokeObjectURL(url), 30 * 60_000); // the tab needs it while open
    return;
  }
  tab?.close();
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

export const previewWorkflow = (id: string, recordId?: string, note?: string) =>
  call<WorkflowPreview>("POST", `workflows/${encodeURIComponent(id)}/preview`, { recordId, note });
export const applyWorkflow = (id: string, records: ProposedRow[], recordId?: string) =>
  call<{ created: Record<string, number>; summary: string }>("POST", `workflows/${encodeURIComponent(id)}/apply`, { recordId, records });
