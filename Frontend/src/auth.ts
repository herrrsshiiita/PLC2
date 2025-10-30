const TOKEN_KEY = "mpm_token";
const USERNAME_KEY = "mpm_username";

export function saveToken(token: string) { localStorage.setItem(TOKEN_KEY, token); }
export function getToken(): string | null { return localStorage.getItem(TOKEN_KEY); }
export function clearToken() { localStorage.removeItem(TOKEN_KEY); localStorage.removeItem(USERNAME_KEY); }
export function saveUsername(username: string) { localStorage.setItem(USERNAME_KEY, username); }
export function getUsername(): string | null { return localStorage.getItem(USERNAME_KEY); }
