"use client";
import { useState } from "react";
import { useParams } from "next/navigation";
import { getApiBase } from "../../../../data/queries/weather";

type Quiz = { id: number; conceptId: number; question: string; answer: string };

export default function QuizPage() {
    const { conceptId } = useParams<{ conceptId: string }>();
    const api = getApiBase();
    const [quiz, setQuiz] = useState<Quiz[]>([]);

    async function generate() {
        const res = await fetch(`${api}/api/quiz/${conceptId}`, { method: "POST" });
        const data = await res.json();
        setQuiz(data);
    }

    return (
        <div className="p-6 space-y-4">
            <h1 className="text-xl font-semibold">Quiz for concept {conceptId}</h1>
            <button className="px-4 py-2 rounded bg-black text-white" onClick={generate}>Generate quiz</button>
            <div className="space-y-3">
                {quiz.slice(0, 1).map(q => (
                    <div key={q.id} className="border rounded p-3">
                        <div className="font-medium">{q.question}</div>
                        <input className="mt-2 border rounded px-3 py-2 w-full" placeholder="Your answer" />
                        <div className="mt-2 text-sm text-gray-600">Correct: {q.answer}</div>
                    </div>
                ))}
            </div>
        </div>
    );
}


