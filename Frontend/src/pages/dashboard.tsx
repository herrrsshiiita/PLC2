import React, { useEffect, useState } from "react";
import { getProjects, createProject, deleteProject } from "../api";
import { Link } from "react-router-dom";

export default function Dashboard() {
  const [projects, setProjects] = useState<any[]>([]);
  const [title, setTitle] = useState("");
  const [desc, setDesc] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => { load(); }, []);

  async function load() {
    setLoading(true);
    try {
      const res = await getProjects();
      setProjects(res);
    } catch (e) {
      console.error(e);
    } finally { setLoading(false); }
  }

  async function add(e?: React.FormEvent) {
    e?.preventDefault();
    if (!title.trim()) return;
    try {
      const res = await createProject(title.trim(), desc.trim());
      setProjects(prev => [res, ...prev]);
      setTitle(""); setDesc("");
    } catch (e) { console.error(e); }
  }

  async function remove(id: number) {
    if (!confirm("Delete project?")) return;
    try {
      await deleteProject(id);
      setProjects(prev => prev.filter(p => p.id !== id));
    } catch (e) { console.error(e); }
  }

  return (
    <div>
      <h2>Projects</h2>
      <form onSubmit={add} className="small-form">
        <input placeholder="Project title (3-100 chars)" value={title} onChange={e => setTitle(e.target.value)} />
        <input placeholder="Description (optional)" value={desc} onChange={e => setDesc(e.target.value)} />
        <button type="submit">Create Project</button>
      </form>

      {loading && <div className="muted">Loading...</div>}
      <ul className="list">
        {projects.map(p => (
          <li key={p.id}>
            <Link to={`/projects/${p.id}`}><strong>{p.title}</strong></Link>
            <div className="meta">{p.description}</div>
            <div className="actions">
              <Link to={`/projects/${p.id}`}>Open</Link>
              <button onClick={() => remove(p.id)}>Delete</button>
            </div>
          </li>
        ))}
        {projects.length === 0 && !loading && <li className="muted">No projects yet</li>}
      </ul>
    </div>
  );
}
