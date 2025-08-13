import { NextRequest } from 'next/server';
import { fetchWeatherForecast } from '../../../data/queries/weather';

export async function GET(_req: NextRequest) {
  try {
    const data = await fetchWeatherForecast();
    return Response.json(data, { status: 200 });
  } catch (err: any) {
    const message = err?.message || 'Failed to fetch weather from backend';
    return Response.json({ error: message }, { status: 500 });
  }
}


