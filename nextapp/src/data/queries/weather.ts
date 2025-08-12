import { Configuration, WeatherHandlersApi } from "../../data/apiClient/openapi";

export function getApiBase() {
  // Web via Aspire sets NEXT_PUBLIC_API_BASE; mobile/desktop must set it in their runtime env/config
  return process.env.NEXT_PUBLIC_API_BASE || "http://localhost:5100";
}

export function createWeatherApi() {
  const apiBase = getApiBase();
  return new WeatherHandlersApi(new Configuration({ basePath: apiBase }));
}

export function weatherQuery() {
  const api = createWeatherApi();
  return {
    queryKey: ["weather"],
    queryFn: () => api.getWeatherForecast(),
  } as const;
}


