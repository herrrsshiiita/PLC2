import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../api";
import { saveToken, saveUsername } from "../auth";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const navigate = useNavigate();

  async function submit(e?: React.FormEvent) {
    e?.preventDefault();
    try {
      setErr(null);
      const res = await login(username, password);
      saveToken(res.token);
      saveUsername(username);
      navigate("/dashboard");
    } catch (e: any) {
      setErr("Invalid credentials");
    }
  }

  return (
    <div className="auth-card">
      <h2>Login</h2>
      <form onSubmit={submit}>
        <input placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} />
        <input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
        <button type="submit">Login</button>
        {err && <div className="error">{err}</div>}
      </form>
    </div>
  );
}
