import { QueryClient, dehydrate, HydrationBoundary } from "@tanstack/react-query";
import WeatherPanel from "../ui/WeatherPanel";
import { weatherQuery } from "../data/queries/weather";

export default async function Home() {
  const qc = new QueryClient();
  await qc.prefetchQuery(weatherQuery());
  const state = dehydrate(qc);

  return (
    <HydrationBoundary state={state}>
      <div className="font-sans min-h-screen p-8 sm:p-20">
        <main className="flex flex-col gap-8">
          <WeatherPanel />
        </main>
      </div>
    </HydrationBoundary>
  );
}
