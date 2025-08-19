import { NextRequest } from "next/server";
import { db } from "../../../../db/client";
import { weatherForecasts } from "../../../../db/schema";

export async function GET(_req: NextRequest) {
    try {
        const connectionString =
            process.env["ConnectionStrings__weather"] ||
            process.env.NEXT_PUBLIC_CONNECTION_STRING ||
            process.env.WEATHER_DB_CONNECTION ||
            process.env.POSTGRES_URL ||
            process.env.DATABASE_URL;
        console.log("connectionString-----:")
        console.log(connectionString);
        const rows = await db.select().from(weatherForecasts).orderBy(weatherForecasts.date);
        // const rows = await db.execute(sql`SELECT * FROM weather_forecasts ORDER BY date`);
        console.log("rows-----:")
        console.log(rows);
        return Response.json(rows, { status: 200 });
    } catch (err: any) {
        return Response.json({ error: err?.message || "Failed to fetch weather via Drizzle" }, { status: 500 });
    }
}


