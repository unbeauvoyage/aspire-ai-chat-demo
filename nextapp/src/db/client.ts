// app/db.ts (or wherever you keep your DB init)
import { drizzle } from "drizzle-orm/node-postgres";
import { Pool, PoolConfig } from "pg";
import * as schema from "./schema";

const raw =
    process.env["ConnectionStrings__weather"] ||
    process.env.WEATHER_DB_CONNECTION ||
    process.env.POSTGRES_URL ||
    process.env.DATABASE_URL;

if (!raw) {
    throw new Error(
        "No Postgres connection string found. Expected ConnectionStrings__weather (Aspire) or POSTGRES_URL/DATABASE_URL."
    );
}

function isAspireKvFormat(s: string): boolean {
    return s.includes("Host=") || s.includes("Server=") || s.includes(";Username=") || s.includes(";User Id=");
}

function parseConnectionString(s: string): PoolConfig {
    const kv = Object.fromEntries(
        s.split(";").filter(Boolean).map((p) => {
            const [k, ...rest] = p.split("=");
            return [k.trim().toLowerCase(), rest.join("=").trim()];
        })
    );
    const host = (kv["host"] ?? kv["server"]) as string | undefined;
    const port = kv["port"] ? Number(kv["port"]) : undefined;
    const user = (kv["username"] ?? kv["user id"] ?? kv["user"]) as string | undefined;
    const password = (kv["password"] as string | undefined) ?? undefined;
    const database = kv["database"] as string | undefined;

    if (!host || !user || !database) {
        throw new Error("Invalid Aspire connection string. Missing host/user/database.");
    }

    // Local Aspire PG is usually non-SSL. Enable SSL via env if needed.
    const sslEnv = process.env.PGSSLMODE || process.env.DATABASE_SSL;
    const ssl =
        sslEnv?.toLowerCase() === "require" || sslEnv === "1" ? { rejectUnauthorized: false } : undefined;

    return { host, port, user, password, database, ssl };
}

const pgConfig: PoolConfig | { connectionString: string } = isAspireKvFormat(raw)
    ? parseConnectionString(raw)
    : { connectionString: raw };

// Reuse a single Pool across hot reloads in dev
const g = globalThis as unknown as { _weatherPool?: Pool };
export const pool: Pool = g._weatherPool ?? new Pool(pgConfig);
if (!g._weatherPool) g._weatherPool = pool;

export const db = drizzle(pool, { schema });
