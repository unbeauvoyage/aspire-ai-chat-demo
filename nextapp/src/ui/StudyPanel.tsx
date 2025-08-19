"use client";
import { useState } from "react";
import { getApiBase } from "../data/queries/weather";

type StudyMessage = { role: string; content: string; timestampUtc: string };
type StudySession = { id: string; topic?: string | null; messages: StudyMessage[] };

export default function StudyPanel() {
    const [topic, setTopic] = useState("");
    const [level, setLevel] = useState("");
    const [exam, setExam] = useState("");
    const [session, setSession] = useState<StudySession | null>(null);
    const [message, setMessage] = useState("");
    const [loading, setLoading] = useState(false);

    const apiBase = getApiBase();

    async function start() {
        setLoading(true);
        try {
            const res = await fetch(`${apiBase}/study/start`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ topic, level: level || null, exam: exam || null }),
            });
            if (!res.ok) throw new Error(await res.text());
            const s = (await res.json()) as StudySession;
            setSession(s);
        } catch (e) {
            console.error(e);
            alert("Failed to start session");
        } finally {
            setLoading(false);
        }
    }

    async function send() {
        if (!session) return;
        const text = message.trim();
        if (!text) return;
        setMessage("");
        setLoading(true);
        try {
            const res = await fetch(`${apiBase}/study/send`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ sessionId: session.id, message: text }),
            });
            if (!res.ok) throw new Error(await res.text());
            const s = (await res.json()) as StudySession;
            setSession(s);
        } catch (e) {
            console.error(e);
            alert("Failed to send message");
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="w-full max-w-2xl mx-auto p-4 space-y-4">
            <h1 className="text-xl font-semibold">Study Assistant (prototype)</h1>
            {!session ? (
                <div className="space-y-3">
                    <input
                        className="w-full border rounded px-3 py-2"
                        placeholder="What do you want to learn? e.g., English B2, SAT, CFA Level 1"
                        value={topic}
                        onChange={(e) => setTopic(e.target.value)}
                    />
                    <div className="flex gap-2">
                        <input
                            className="flex-1 border rounded px-3 py-2"
                            placeholder="Current level (optional)"
                            value={level}
                            onChange={(e) => setLevel(e.target.value)}
                        />
                        <input
                            className="flex-1 border rounded px-3 py-2"
                            placeholder="Exam (optional)"
                            value={exam}
                            onChange={(e) => setExam(e.target.value)}
                        />
                    </div>
                    <button
                        className="px-4 py-2 rounded bg-black text-white disabled:opacity-50"
                        onClick={start}
                        disabled={loading || !topic.trim()}
                    >
                        {loading ? "Starting…" : "Start"}
                    </button>
                </div>
            ) : (
                <div className="space-y-3">
                    <div className="border rounded p-3 h-96 overflow-auto bg-gray-50 dark:bg-gray-900">
                        {session.messages.map((m, i) => (
                            <div key={i} className="mb-2">
                                <div className="text-xs text-gray-500">{m.role}</div>
                                <div className="whitespace-pre-wrap text-sm">{m.content}</div>
                            </div>
                        ))}
                    </div>
                    <div className="flex gap-2">
                        <input
                            className="flex-1 border rounded px-3 py-2"
                            placeholder="Type your reply…"
                            value={message}
                            onChange={(e) => setMessage(e.target.value)}
                            onKeyDown={(e) => {
                                if (e.key === "Enter" && !e.shiftKey) {
                                    e.preventDefault();
                                    send();
                                }
                            }}
                        />
                        <button
                            className="px-4 py-2 rounded bg-black text-white disabled:opacity-50"
                            onClick={send}
                            disabled={loading || !message.trim()}
                        >
                            {loading ? "Sending…" : "Send"}
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}


