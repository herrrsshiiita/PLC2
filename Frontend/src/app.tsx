import React from "react";
import { Link, Outlet, useNavigate } from "react-router-dom";
import { getToken, clearToken, getUsername } from "./auth";

export default function App() {
  const navigate = useNavigate();
  const token = getToken();
  const username = getUsername();

  function logout() {
    clearToken();
    navigate("/login");
  }

  return (
    <div className="app">
      <header className="header">
        <h1 className="brand"><Link to="/">Mini Project Manager</Link></h1>
        <nav>
          {token ? (
            <>
              <span className="username">Hi, {username}</span>
              <Link to="/dashboard">Dashboard</Link>
              <button onClick={logout}>Logout</button>
            </>
          ) : (
            <>
              <Link to="/login">Login</Link>
              <Link to="/register">Register</Link>
            </>
          )}
        </nav>
      </header>

      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
