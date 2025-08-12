"use client";
import { useQuery } from "@tanstack/react-query";
import { weatherQuery } from "../data/queries/weather";

export default function WeatherPanel() {
  const { data, isLoading, error } = useQuery(weatherQuery());

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
    <div className="w-full max-w-md p-4 border rounded-lg">
      <h2 className="text-lg font-semibold mb-4 text-center">Weather</h2>
      <div className="space-y-2">
        {list.map((f, i) => (
          <div key={i} className="flex justify-between items-center p-2 bg-gray-50 dark:bg-gray-800 rounded">
            <span className="text-sm">{f.date}</span>
            <span className="text-sm font-medium">{f.temperatureC}°C ({f.temperatureF}°F)</span>
            <span className="text-sm text-gray-600 dark:text-gray-400">{f.summary}</span>
          </div>
        ))}
      </div>
    </div>
  );
}


