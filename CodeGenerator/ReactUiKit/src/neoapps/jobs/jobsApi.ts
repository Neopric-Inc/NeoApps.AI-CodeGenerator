// Client for background jobs (/v1/api/Jobs): start, follow the live feed (long-poll), approve/reject, cancel, list.
// Custom screens (src/custom) can use these too, together with useJobFeed and <JobFeed/>.
import { useEffect, useRef, useState } from "react";
import { apiBase, ApiError, getToken } from "../api";

export type JobStatus = "queued" | "running" | "awaiting_approval" | "approved" | "completed" | "failed" | "cancelled" | "rejected";

export interface Job {
  job_id: string;
  kind: string;
  title?: string;
  status: JobStatus;
  progress?: number | null;
  username: string;
  error?: string | null;
  attempts: number;
  createdAt: string;
  startedAt?: string | null;
  finishedAt?: string | null;
  input?: Record<string, any> | null;
  /** The proposal while awaiting approval, the outcome when completed. */
  result?: any;
}

export interface JobEvent {
  id: number;
  level: "info" | "warn" | "error" | "approval" | "done";
  message: string;
  progress?: number | null;
  data?: any;
  at: string;
}

export const FINISHED: JobStatus[] = ["completed", "failed", "cancelled", "rejected"];

async function call<T>(method: string, path: string, body?: any, signal?: AbortSignal): Promise<T> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  const res = await fetch(apiBase() + "Jobs" + path, { method, headers, signal, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text();
  let json: any = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = null;
  }
  if (res.status === 401) throw new ApiError(401, "Your session has expired. Sign in again.");
  if (!res.ok) throw new ApiError(res.status, json?.message || res.statusText || "Request failed");
  return json as T;
}

export const startJob = (kind: string, input: Record<string, any> = {}) => call<Job>("POST", "", { kind, input });
export const getJob = (id: string) => call<Job>("GET", `/${id}`);
export const listJobs = (status?: string) => call<Job[]>("GET", status ? `?status=${encodeURIComponent(status)}` : "");
export const jobEvents = (id: string, after: number, wait: number, signal?: AbortSignal) =>
  call<{ job: Job; events: JobEvent[] }>("GET", `/${id}/events?after=${after}&wait=${wait}`, undefined, signal);
export const approveJob = (id: string, decision: Record<string, any>) => call<Job>("POST", `/${id}/approve`, { decision });
export const rejectJob = (id: string, reason?: string) => call<Job>("POST", `/${id}/reject`, { reason });
export const cancelJob = (id: string) => call<Job>("POST", `/${id}/cancel`);

/**
 * Follows a job live: long-polls its feed (the API answers the moment something happens) until the job finishes or the
 * component unmounts. Keeps following while it waits for approval, so an approval from another tab shows up too.
 */
export function useJobFeed(jobId?: string) {
  const [job, setJob] = useState<Job>();
  const [events, setEvents] = useState<JobEvent[]>([]);
  const [error, setError] = useState<string>();
  const after = useRef(0);

  useEffect(() => {
    if (!jobId) return;
    const ctl = new AbortController();
    after.current = 0;
    setEvents([]);
    setJob(undefined);
    setError(undefined);
    (async () => {
      let failures = 0;
      while (!ctl.signal.aborted) {
        try {
          const res = await jobEvents(jobId, after.current, 20, ctl.signal);
          failures = 0;
          setError(undefined);
          setJob(res.job);
          if (res.events.length) {
            after.current = res.events[res.events.length - 1].id;
            setEvents((e) => [...e, ...res.events]);
          }
          if (FINISHED.includes(res.job.status)) return;
        } catch (e: any) {
          if (ctl.signal.aborted) return;
          if (e instanceof ApiError && e.status !== 0 && e.status < 500) {
            setError(e.message);
            return;
          }
          failures++;
          setError(failures > 2 ? "Lost connection to the server; retrying…" : undefined);
          const delay = Math.min(10000, 1000 * failures);
          await new Promise((r) => setTimeout(r, delay));
        }
      }
    })();
    return () => ctl.abort();
  }, [jobId]);

  return { job, events, error };
}
