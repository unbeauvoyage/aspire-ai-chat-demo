"use client";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { getApiBase } from "../../../../data/queries/weather";
import Link from "next/link";

type Concept = { id: number; goalId: number; title: string; content: string };

export default function ConceptsPage() {
    const { goalId } = useParams<{ goalId: string }>();
    const api = getApiBase();
    const [concepts, setConcepts] = useState<Concept[]>([]);
    const [loading, setLoading] = useState(false);

    async function generate() {
        setLoading(true);
        const res = await fetch(`${api}/api/concepts/${goalId}`, { method: "POST" });
        const data = await res.json();
        setConcepts(data);
        setLoading(false);
    }

    useEffect(() => { /* noop: only generate on button */ }, []);

    return (
        <div className="p-6 space-y-4">
            <h1 className="text-xl font-semibold">Concepts for goal {goalId}</h1>
            <button className="px-4 py-2 rounded bg-black text-white disabled:opacity-50" onClick={generate} disabled={loading}>{loading ? "Generating…" : "Generate concepts"}</button>
            <div className="space-y-3">
                {concepts.map(c => (
                    <div key={c.id} className="border rounded p-3">
                        <div className="font-medium">{c.title}</div>
                        <div className="text-sm whitespace-pre-wrap">{c.content}</div>
                        <div className="mt-2"><Link href={`/tutor/quiz/${c.id}`}>Make quiz</Link></div>
                    </div>
                ))}
            </div>
        </div>
    );
}


