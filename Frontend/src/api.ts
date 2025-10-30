import { getToken } from "./auth";

const API_BASE = "http://localhost:5000/api/v1";

async function request(path: string, opts: RequestInit = {}) {
  const headers = opts.headers ? new Headers(opts.headers as any) : new Headers();
  headers.set("Content-Type", "application/json");
  const token = getToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const res = await fetch(`${API_BASE}${path}`, { ...opts, headers });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  if (res.status === 204) return null;
  return res.json();
}

// Auth
export function register(username: string, password: string) {
  return request("/auth/register", { method: "POST", body: JSON.stringify({ username, password }) });
}
export function login(username: string, password: string) {
  return request("/auth/login", { method: "POST", body: JSON.stringify({ username, password }) });
}

// Projects
export function getProjects() { return request("/projects"); }
export function createProject(title: string, description?: string) {
  return request("/projects", { method: "POST", body: JSON.stringify({ title, description }) });
}
export function getProject(id: number) { return request(`/projects/${id}`); }
export function deleteProject(id: number) { return request(`/projects/${id}`, { method: "DELETE" }); }

// Tasks
export function createTask(projectId: number, title: string, dueDate?: string) {
  return request(`/projects/${projectId}/tasks`, { method: "POST", body: JSON.stringify({ title, dueDate }) });
}
export function updateTask(taskId: number, payload: any) {
  return request(`/tasks/${taskId}`, { method: "PUT", body: JSON.stringify(payload) });
}
export function deleteTask(taskId: number) { return request(`/tasks/${taskId}`, { method: "DELETE" }); }
export function toggleTask(taskId: number) { return request(`/tasks/${taskId}/toggle`, { method: "PUT" }); }

// Scheduler
export function scheduleProject(projectId: number, startDate?: string, daysPerTask?: number) {
  return request(`/projects/${projectId}/schedule`, { method: "POST", body: JSON.stringify({ startDate, daysPerTask }) });
}
