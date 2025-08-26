"use client";
import { useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { getApiBase } from "../../../../data/queries/weather";

type Quiz = { id: number; conceptId: number; question: string; answer: string };

export default function QuizPage() {
    const { conceptId } = useParams<{ conceptId: string }>();
    const api = getApiBase();
    const [quiz, setQuiz] = useState<Quiz[]>([]);
    const [idx, setIdx] = useState(0);
    const [choice, setChoice] = useState<string | null>(null);

    async function generate() {
        const res = await fetch(`${api}/api/quiz/${conceptId}`, { method: "POST" });
        const data = await res.json();
        setQuiz(data);
        setIdx(0);
        setChoice(null);
    }

    const current = quiz[idx];
    const options = useMemo(() => {
        if (!current) return [] as { key: string; text: string }[];
        const lines = current.question.split("\n");
        return lines
            .map(l => l.trim())
            .filter(l => /^(A|B|C|D)\)/.test(l))
            .map(l => ({ key: l.substring(0, 1), text: l }));
    }, [current]);

    const passageAndStem = useMemo(() => {
        if (!current) return "";
        const parts = current.question.split("\n");
        const splitIndex = parts.findIndex(p => /^(A|B|C|D)\)/.test(p.trim()));
        return splitIndex >= 0 ? parts.slice(0, splitIndex).join("\n") : current.question;
    }, [current]);

    return (
        <div className="p-6 space-y-4">
            <h1 className="text-xl font-semibold">Quiz for concept {conceptId}</h1>
            <div className="flex items-center gap-2">
                <button className="px-4 py-2 rounded bg-black text-white" onClick={generate}>Generate quiz</button>
                {quiz.length > 0 && (
                    <div className="text-sm text-gray-600">Question {idx + 1} / {quiz.length}</div>
                )}
            </div>

            {current && (
                <div className="border rounded p-3 space-y-3">
                    <div className="whitespace-pre-wrap">{passageAndStem}</div>
                    <div className="space-y-2">
                        {options.map(opt => (
                            <label key={opt.key} className="flex items-center gap-2">
                                <input
                                    type="radio"
                                    name="mcq"
                                    value={opt.key}
                                    checked={choice === opt.key}
                                    onChange={() => setChoice(opt.key)}
                                />
                                <span>{opt.text}</span>
                            </label>
                        ))}
                    </div>
                    {choice && (
                        <div className="text-sm">
                            {choice === current.answer ? (
                                <span className="text-green-600">Correct</span>
                            ) : (
                                <span className="text-red-600">Incorrect (Correct: {current.answer})</span>
                            )}
                        </div>
                    )}
                    <div className="flex justify-between pt-2">
                        <button
                            className="px-3 py-2 border rounded disabled:opacity-50"
                            onClick={() => { setIdx(i => Math.max(0, i - 1)); setChoice(null); }}
                            disabled={idx === 0}
                        >Prev</button>
                        <button
                            className="px-3 py-2 border rounded disabled:opacity-50"
                            onClick={() => { setIdx(i => Math.min(quiz.length - 1, i + 1)); setChoice(null); }}
                            disabled={idx >= quiz.length - 1}
                        >Next</button>
                    </div>
                </div>
            )}
        </div>
    );
}


