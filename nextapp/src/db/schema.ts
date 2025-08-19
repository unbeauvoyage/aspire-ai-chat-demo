import { pgTable, serial, integer, varchar, date } from "drizzle-orm/pg-core";

export const weatherForecasts = pgTable("WeatherForecasts", {
    id: serial("Id").primaryKey(),
    date: date("Date").notNull(),
    temperatureC: integer("TemperatureC").notNull(),
    summary: varchar("Summary", { length: 128 }),
});


