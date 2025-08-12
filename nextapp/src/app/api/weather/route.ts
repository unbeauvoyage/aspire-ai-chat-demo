import { NextRequest } from 'next/server';
import { Configuration, WeatherHandlersApi } from '../../../data/apiClient/openapi';

export async function GET(_req: NextRequest) {
  const apiBase = process.env.NEXT_PUBLIC_API_BASE || 'http://localhost:5100';

  try {
    const configuration = new Configuration({ basePath: apiBase });
    const api = new WeatherHandlersApi(configuration);
    const data = await api.getWeatherForecast();
    return Response.json(data, { status: 200 });
  } catch (err: any) {
    const message = err?.message || 'Failed to fetch weather from backend';
    return Response.json({ error: message }, { status: 500 });
  }
}


