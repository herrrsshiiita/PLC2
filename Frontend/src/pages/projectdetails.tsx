import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getProject, createTask, toggleTask, deleteTask, updateTask, scheduleProject } from "../api";

export default function ProjectDetails() {
  const { id } = useParams<{ id: string }>();
  const pid = Number(id);
  const [project, setProject] = useState<any | null>(null);
  const [title, setTitle] = useState("");
  const [due, setDue] = useState<string | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [schedule, setSchedule] = useState<any[] | null>(null);

  useEffect(() => { load(); }, [id]);

  async function load() {
    setLoading(true);
    try {
      const res = await getProject(pid);
      setProject(res);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  }

  async function addTask(e?: React.FormEvent) {
    e?.preventDefault();
    if (!title.trim()) return;
    try {
      const res = await createTask(pid, title.trim(), due ?? null);
      setProject((p: any) => ({ ...p, tasks: [...p.tasks, res] }));
      setTitle(""); setDue(undefined);
    } catch (e) { console.error(e); }
  }

  async function onToggle(tid: number) {
    try {
      const updated = await toggleTask(tid);
      setProject((p: any) => ({ ...p, tasks: p.tasks.map((t: any) => t.id === tid ? updated : t) }));
    } catch (e) { console.error(e); }
  }

  async function onDelete(tid: number) {
    if (!confirm("Delete task?")) return;
    try {
      await deleteTask(tid);
      setProject((p: any) => ({ ...p, tasks: p.tasks.filter((t: any) => t.id !== tid) }));
    } catch (e) { console.error(e); }
  }

  async function runSchedule() {
    try {
      const res = await scheduleProject(pid, new Date().toISOString(), 1);
      setSchedule(res.schedule);
    } catch (e) { console.error(e); }
  }

  if (!project) return <div className="muted">Loading project...</div>;

  return (
    <div>
      <h2>{project.title}</h2>
      <p className="muted">{project.description}</p>

      <form onSubmit={addTask} className="small-form">
        <input placeholder="Task title" value={title} onChange={e => setTitle(e.target.value)} />
        <input type="date" onChange={e => setDue(e.target.value)} />
        <button type="submit">Add Task</button>
      </form>

      <h3>Tasks</h3>
      <ul className="list">
        {project.tasks.map((t: any) => (
          <li key={t.id} className={t.isCompleted ? "completed" : ""}>
            <label>
              <input type="checkbox" checked={t.isCompleted} onChange={() => onToggle(t.id)} />
              <span>{t.title}</span>
            </label>
            <div className="meta">{t.dueDate ? `Due: ${new Date(t.dueDate).toLocaleDateString()}` : ""}</div>
            <div className="actions">
              <button onClick={() => onDelete(t.id)}>Delete</button>
            </div>
          </li>
        ))}
        {project.tasks.length === 0 && <li className="muted">No tasks yet</li>}
      </ul>

      <div style={{ marginTop: 16 }}>
        <button onClick={runSchedule}>Generate Schedule</button>
        {schedule && (
          <div className="schedule">
            <h4>Schedule</h4>
            <ul>
              {schedule.map((s: any) => (
                <li key={s.taskId}>{new Date(s.scheduledDate).toLocaleDateString()} — {s.title}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}
