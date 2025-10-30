import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { register, login } from "../api";
import { saveToken, saveUsername } from "../auth";

export default function Register() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const navigate = useNavigate();

  async function submit(e?: React.FormEvent) {
    e?.preventDefault();
    try {
      setErr(null);
      await register(username, password);
      // auto-login
      const res = await login(username, password);
      saveToken(res.token);
      saveUsername(username);
      navigate("/dashboard");
    } catch (e: any) {
      setErr("Registration failed - username might be taken");
    }
  }

  return (
    <div className="auth-card">
      <h2>Register</h2>
      <form onSubmit={submit}>
        <input placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} />
        <input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
        <button type="submit">Register</button>
        {err && <div className="error">{err}</div>}
      </form>
    </div>
  );
}
