"use client";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { weatherQuery, createWeatherApi } from "../data/queries/weather";

export default function WeatherPanel() {
  const { data, isLoading, error } = useQuery(weatherQuery());
  const [prompt, setPrompt] = useState("");

  const studyMutation = useMutation({
    mutationFn: async (userPrompt: string) => {
      const api = createWeatherApi();
      const forecasts = (data ?? []).map(f => ({
        date: f.date,
        temperatureC: f.temperatureC,
        temperatureF: f.temperatureF,
        summary: f.summary,
        icon: (f as any).icon ?? null,
      }));
      return api.weatherStudy({ weatherStudyRequest: { forecasts, userPrompt } });
    },
  });

  if (isLoading) {
    return (
      <div className="w-full max-w-md p-4 border rounded-lg">
        <h2 className="text-lg font-semibold mb-4 text-center">Weather (loading…)</h2>
        <div className="space-y-2">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="animate-pulse h-8 bg-gray-200 dark:bg-gray-800 rounded" />
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return <div className="w-full max-w-md p-4 border rounded-lg">Failed to load</div>;
  }

  const list = data ?? [];
  return (
    <div className="w-full max-w-2xl p-4 border rounded-lg space-y-6">
      <h2 className="text-lg font-semibold">Weather</h2>
      <div className="space-y-2">
        {list.map((f, i) => (
          <div key={i} className="flex justify-between items-center p-2 bg-gray-50 dark:bg-gray-800 rounded">
            <span className="text-sm">{f.date}</span>
            <span className="text-sm font-medium">{f.temperatureC}°C ({f.temperatureF}°F)</span>
            <span className="text-sm text-gray-600 dark:text-gray-400">{f.summary}</span>
          </div>
        ))}
      </div>

      <div className="space-y-2">
        <h3 className="font-medium">Interactive study</h3>
        <div className="flex gap-2">
          <input
            className="flex-1 border rounded px-3 py-2 bg-white dark:bg-gray-900"
            placeholder="Ask for trends, outfit advice, outdoor plan, or packing list..."
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
          />
          <button
            className="px-3 py-2 border rounded bg-black text-white dark:bg-white dark:text-black disabled:opacity-50"
            disabled={studyMutation.isLoading || list.length === 0}
            onClick={() => studyMutation.mutate(prompt)}
          >
            {studyMutation.isLoading ? "Studying…" : "Study"}
          </button>
        </div>
        {studyMutation.data && (
          <div className="mt-2 p-3 rounded bg-gray-50 dark:bg-gray-800">
            {studyMutation.data.llmAnalysis && (
              <div className="whitespace-pre-wrap text-sm">{studyMutation.data.llmAnalysis}</div>
            )}
            {Array.isArray(studyMutation.data.availableFunctions) && studyMutation.data.availableFunctions.length > 0 && (
              <div className="mt-2 text-xs text-gray-600 dark:text-gray-400">
                Available tools: {studyMutation.data.availableFunctions.join(", ")}
              </div>
            )}
          </div>
        )}
        {studyMutation.isError && (
          <div className="text-sm text-red-600">Failed to study the weather.</div>
        )}
      </div>
    </div>
  );
}


