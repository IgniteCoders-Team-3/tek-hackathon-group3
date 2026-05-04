"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { WeatherResponse } from "@/lib/types";
import { Card } from "@/components/UI";

/** Maps WMO weather code → emoji for visual display. */
function weatherEmoji(code: number): string {
  if (code === 0) return "☀️";
  if (code <= 2) return "⛅";
  if (code === 3) return "☁️";
  if (code <= 48) return "🌫️";
  if (code <= 57) return "🌦️";
  if (code <= 67) return "🌧️";
  if (code <= 77) return "❄️";
  if (code <= 82) return "🌦️";
  if (code <= 86) return "🌨️";
  if (code <= 99) return "⛈️";
  return "🌡️";
}

/** Format a date string like "2025-05-04" → "Mon" */
function shortDay(dateStr: string): string {
  try {
    return new Date(dateStr + "T00:00:00").toLocaleDateString("en", { weekday: "short" });
  } catch {
    return dateStr;
  }
}

export function WeatherWidget({ city = "Lagos" }: { city?: string }) {
  const [data, setData] = useState<WeatherResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let alive = true;
    setLoading(true);
    setError(false);
    api.weather(city)
      .then((r) => {
        if (alive) {
          setData(r);
          setLoading(false);
        }
      })
      .catch(() => {
        // Fallback: If Open-Meteo doesn't recognize the specific region (e.g. "Lagos West"), fallback to "Lagos"
        if (city !== "Lagos" && alive) {
          api.weather("Lagos")
            .then((r) => { if (alive) setData(r); })
            .catch(() => { if (alive) setError(true); })
            .finally(() => { if (alive) setLoading(false); });
        } else {
          if (alive) {
            setError(true);
            setLoading(false);
          }
        }
      });
    return () => { alive = false; };
  }, [city]);

  if (loading) {
    return (
      <Card pad={14}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ fontSize: 18, animation: "pulse 1.5s ease-in-out infinite" }}>🌤️</span>
          <span className="mono" style={{ fontSize: 10, color: "var(--ink-3)", letterSpacing: ".1em" }}>
            LOADING WEATHER…
          </span>
        </div>
      </Card>
    );
  }

  if (error || !data) {
    return (
      <Card pad={14}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ fontSize: 16 }}>⚠️</span>
          <span style={{ fontSize: 11, color: "var(--ink-3)" }}>Weather unavailable</span>
        </div>
      </Card>
    );
  }

  const { current, forecast } = data;

  return (
    <Card pad={0} style={{ overflow: "hidden" }}>
      {/* Current weather header */}
      <div style={{
        padding: "14px 16px",
        background: "linear-gradient(135deg, rgba(0,210,120,.06), rgba(0,180,255,.06))",
        borderBottom: "1px solid var(--line)",
      }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
          <div>
            <div className="mono uppr" style={{
              fontSize: 9.5, color: "var(--ink-3)", letterSpacing: ".14em", marginBottom: 6,
              display: "flex", alignItems: "center", gap: 6,
            }}>
              <span style={{
                display: "inline-block", width: 6, height: 6, borderRadius: "50%",
                background: "var(--accent)", boxShadow: "0 0 8px var(--accent)",
              }} />
              WEATHER · {current.city.toUpperCase()}
            </div>
            <div style={{ display: "flex", alignItems: "baseline", gap: 4 }}>
              <span style={{ fontSize: 32, fontWeight: 700, lineHeight: 1 }}>
                {Math.round(current.temperature)}
              </span>
              <span style={{ fontSize: 14, color: "var(--ink-3)", fontWeight: 400 }}>°C</span>
            </div>
            <div style={{ fontSize: 12, color: "var(--ink-2)", marginTop: 4 }}>
              {current.condition}
            </div>
          </div>
          <div style={{ fontSize: 42, lineHeight: 1, marginTop: -2 }}>
            {weatherEmoji(current.weatherCode)}
          </div>
        </div>

        {/* Mini stats */}
        <div style={{
          display: "flex", gap: 16, marginTop: 12,
          paddingTop: 10, borderTop: "1px solid var(--line)",
        }}>
          <MiniStat icon="💧" label="HUMIDITY" value={`${current.humidity}%`} />
          <MiniStat icon="💨" label="WIND" value={`${current.windSpeed} km/h`} />
          <MiniStat icon="🌍" label="COUNTRY" value={current.country} />
        </div>
      </div>

      {/* 5-day forecast strip */}
      <div style={{
        display: "grid",
        gridTemplateColumns: `repeat(${forecast.length}, 1fr)`,
        gap: 0,
      }}>
        {forecast.map((day, i) => (
          <div key={day.date} style={{
            padding: "10px 8px",
            textAlign: "center",
            borderRight: i < forecast.length - 1 ? "1px solid var(--line)" : "none",
            transition: "background .15s",
          }}
            onMouseEnter={e => (e.currentTarget.style.background = "var(--bg-3)")}
            onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
          >
            <div className="mono" style={{ fontSize: 9.5, color: "var(--ink-3)", marginBottom: 4, letterSpacing: ".08em" }}>
              {shortDay(day.date)}
            </div>
            <div style={{ fontSize: 20, lineHeight: 1.2 }}>
              {weatherEmoji(day.weatherCode)}
            </div>
            <div className="mono" style={{ fontSize: 11, fontWeight: 600, marginTop: 4 }}>
              {Math.round(day.tempMax)}°
            </div>
            <div className="mono" style={{ fontSize: 10, color: "var(--ink-3)" }}>
              {Math.round(day.tempMin)}°
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

function MiniStat({ icon, label, value }: { icon: string; label: string; value: string }) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
      <span style={{ fontSize: 13 }}>{icon}</span>
      <div>
        <div className="mono uppr" style={{ fontSize: 8, color: "var(--ink-3)", letterSpacing: ".12em" }}>{label}</div>
        <div className="mono" style={{ fontSize: 11, fontWeight: 500 }}>{value}</div>
      </div>
    </div>
  );
}
