"use client";
import { useState } from "react";
import { getApiBase } from "../../../data/queries/weather";
import Link from "next/link";

export default function GoalPage() {
    const [desc, setDesc] = useState("");
    const [goalId, setGoalId] = useState<number | null>(null);
    const api = getApiBase();

    async function submit() {
        const res = await fetch(`${api}/api/goals`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ description: desc }) });
        const data = await res.json();
        setGoalId(data.goalId);
    }

    return (
        <div className="p-6 space-y-4">
            <h1 className="text-xl font-semibold">Create Goal</h1>
            <input className="border rounded px-3 py-2 w-full" placeholder="e.g., Learn TOEFL Reading" value={desc} onChange={(e) => setDesc(e.target.value)} />
            <button className="px-4 py-2 rounded bg-black text-white" onClick={submit} disabled={!desc.trim()}>Create</button>
            {goalId && (
                <div>
                    <Link href={`/tutor/concepts/${goalId}`}>Go to concepts</Link>
                </div>
            )}
        </div>
    );
}


