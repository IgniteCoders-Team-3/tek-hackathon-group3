"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { TopBar } from "@/components/TopBar";
import { NetworkMap, type MapMode } from "@/components/NetworkMap";
import { Bar, Btn, Card, Pill, Section } from "@/components/UI";
import { api } from "@/lib/api";
import type { MapResponse, Tower } from "@/lib/types";
import { WeatherWidget } from "@/components/WeatherWidget";

export default function MapPage() {
  const router = useRouter();
  const [data, setData] = useState<MapResponse | null>(null);
  const [sel, setSel] = useState<Tower | null>(null);
  const [dispatched, setDispatched] = useState<string | null>(null);
  const [mode, setMode] = useState<MapMode>("engineer");
  const [mapLoading, setMapLoading] = useState(true);
  const [mapError, setMapError] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    setMapLoading(true);
    setMapError(null);
    api.map()
      .then((r) => {
        if (!alive) return;
        setData(r);
        setSel(
          r.towers.find((t) => t.status === "critical") ?? r.towers[0] ?? null,
        );
      })
      .catch((e) => {
        if (!alive) return;
        console.error("[map] api.map() failed:", e);
        setMapError(String(e?.message ?? "Failed to load map data"));
      })
      .finally(() => { if (alive) setMapLoading(false); });
    return () => {
      alive = false;
    };
  }, []);

  useEffect(() => {
    setDispatched(null);
  }, [sel?.id]);

  return (
    <>
      <TopBar
        title="Network Map"
        sub={`Lagos metro · ${data?.totalTowers ?? "—"} towers · live signal & outage overlay`}
        right={
          <div style={{ display: "flex", gap: 6, padding: 3, background: "var(--bg-1)", border: "1px solid var(--line)", borderRadius: 7 }}>
            {(["engineer", "public"] as const).map((k) => (
              <button
                key={k}
                onClick={() => setMode(k)}
                style={{
                  appearance: "none", border: 0,
                  padding: "5px 12px", borderRadius: 5,
                  fontSize: 11, fontWeight: 500,
                  background: mode === k ? "var(--bg-3)" : "transparent",
                  color: mode === k ? "var(--ink)" : "var(--ink-3)",
                  cursor: "pointer",
                }}
              >
                {k === "engineer" ? "Engineer" : "Public"}
              </button>
            ))}
          </div>
        }
      />
      <div
        style={{
          padding: 22,
          display: "grid",
          gridTemplateColumns: "1fr 320px",
          gap: 14,
          height: "calc(100vh - 67px)",
        }}
      >
        <div style={{ position: "relative", height: "100%", minHeight: 0 }}>
          {mapLoading && (
            <div style={{
              position: "absolute", inset: 0, display: "flex",
              alignItems: "center", justifyContent: "center",
              background: "var(--bg-map)", border: "1px solid var(--line)",
              borderRadius: 10, flexDirection: "column", gap: 10,
            }}>
              <div className="pulse" style={{ width: 10, height: 10, borderRadius: "50%", background: "var(--accent)" }} />
              <span className="mono" style={{ fontSize: 10, color: "var(--ink-3)", letterSpacing: ".14em" }}>LOADING MAP DATA…</span>
            </div>
          )}
          {mapError && !mapLoading && (
            <div style={{
              position: "absolute", inset: 0, display: "flex",
              alignItems: "center", justifyContent: "center",
              background: "var(--bg-map)", border: "1px solid var(--crit)",
              borderRadius: 10, flexDirection: "column", gap: 8,
            }}>
              <span style={{ fontSize: 20 }}>⚠️</span>
              <span className="mono" style={{ fontSize: 10, color: "var(--crit)", letterSpacing: ".12em" }}>MAP LOAD FAILED</span>
              <span style={{ fontSize: 11, color: "var(--ink-3)", maxWidth: 260, textAlign: "center" }}>{mapError}</span>
            </div>
          )}
          {!mapLoading && !mapError && data && (
            <NetworkMap
              towers={data.towers}
              onSelect={setSel}
              selectedId={sel?.id}
              mode={mode}
            />
          )}
        </div>
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            gap: 14,
            overflowY: "auto",
          }}
        >
          <WeatherWidget city={sel?.region || "Lagos"} />

          {sel && (
            <Card pad={14}>
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "flex-start",
                  marginBottom: 10,
                }}
              >
                <div>
                  <div
                    className="mono"
                    style={{
                      fontSize: 10,
                      color: "var(--accent)",
                      marginBottom: 3,
                    }}
                  >
                    {sel.id}
                  </div>
                  <div style={{ fontSize: 14, fontWeight: 600 }}>
                    {sel.name}
                  </div>
                  <div
                    className="mono"
                    style={{
                      fontSize: 10,
                      color: "var(--ink-3)",
                      marginTop: 2,
                    }}
                  >
                    {sel.region.toUpperCase()}
                  </div>
                </div>
                <Pill
                  tone={
                    sel.status === "critical"
                      ? "crit"
                      : sel.status === "warn"
                        ? "warn"
                        : "ok"
                  }
                  dot
                >
                  {sel.status}
                </Pill>
              </div>
              {sel.issue && (
                <div
                  style={{
                    padding: 10,
                    background: "var(--bg-3)",
                    borderRadius: 6,
                    fontSize: 11.5,
                    marginBottom: 10,
                    borderLeft: `2px solid ${sel.status === "critical" ? "var(--crit)" : "var(--warn)"}`,
                  }}
                >
                  {sel.issue}
                </div>
              )}
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "1fr 1fr",
                  gap: 10,
                  marginTop: 6,
                }}
              >
                <Metric
                  label="SIGNAL"
                  v={sel.signal}
                  unit="%"
                  tone={
                    sel.signal > 70 ? "ok" : sel.signal > 40 ? "warn" : "crit"
                  }
                />
                <Metric
                  label="LOAD"
                  v={sel.load}
                  unit="%"
                  tone={sel.load > 85 ? "crit" : sel.load > 70 ? "warn" : "ok"}
                />
              </div>
              <div style={{ display: "flex", gap: 6, marginTop: 14 }}>
                <Btn
                  primary
                  small
                  onClick={() =>
                    router.push(
                      `/copilot?q=${encodeURIComponent(`Diagnose tower ${sel.id} in ${sel.region}`)}`,
                    )
                  }
                >
                  Diagnose
                </Btn>
              </div>
            </Card>
          )}
          <Section label="REGIONS">
            <Card pad={0}>
              {(data?.regions ?? []).map((r, i, all) => {
                const tone =
                  r.critical > 0 ? "crit" : r.warn > 0 ? "warn" : "ok";
                return (
                  <div
                    key={r.name}
                    style={{
                      padding: "10px 14px",
                      borderBottom:
                        i < all.length - 1 ? "1px solid var(--line)" : 0,
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <div>
                      <div style={{ fontSize: 12.5 }}>{r.name}</div>
                      <div
                        className="mono"
                        style={{
                          fontSize: 10,
                          color: "var(--ink-3)",
                          marginTop: 2,
                        }}
                      >
                        {r.towers} towers
                      </div>
                    </div>
                    <Pill tone={tone} dot>
                      {r.critical
                        ? `${r.critical} crit`
                        : r.warn
                          ? `${r.warn} warn`
                          : "ok"}
                    </Pill>
                  </div>
                );
              })}
            </Card>
          </Section>

          <Section label="BEST SIGNAL ZONES">
            <Card pad={0}>
              {[...(data?.regions ?? [])]
                .sort((a, b) => b.avgSignal - a.avgSignal)
                .slice(0, 3)
                .map((r, i) => {
                  const tone =
                    r.avgSignal >= 80
                      ? "ok"
                      : r.avgSignal >= 55
                        ? "warn"
                        : "crit";
                  return (
                    <div
                      key={r.name}
                      style={{
                        padding: "10px 14px",
                        borderBottom: i < 2 ? "1px solid var(--line)" : 0,
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                      }}
                    >
                      <div>
                        <div style={{ fontSize: 12.5 }}>{r.name}</div>
                        <div
                          className="mono"
                          style={{
                            fontSize: 10,
                            color: "var(--ink-3)",
                            marginTop: 2,
                          }}
                        >
                          {r.towers} towers ·{" "}
                          {r.critical + r.warn === 0
                            ? "all ok"
                            : `${r.critical + r.warn} degraded`}
                        </div>
                      </div>
                      <Pill tone={tone} dot>
                        {r.avgSignal}%
                      </Pill>
                    </div>
                  );
                })}
            </Card>
          </Section>
        </div>
      </div>
    </>
  );
}

function Metric({
  label,
  v,
  unit,
  tone,
}: {
  label: string;
  v: number;
  unit: string;
  tone: "ok" | "warn" | "crit";
}) {
  return (
    <div>
      <div
        className="mono uppr"
        style={{
          fontSize: 9,
          color: "var(--ink-3)",
          letterSpacing: ".12em",
          marginBottom: 4,
        }}
      >
        {label}
      </div>
      <div
        className="mono"
        style={{
          fontSize: 18,
          fontWeight: 600,
          marginBottom: 5,
          color:
            tone === "crit"
              ? "var(--crit)"
              : tone === "warn"
                ? "var(--warn)"
                : "var(--ok)",
        }}
      >
        {v}
        <span style={{ color: "var(--ink-3)", fontSize: 11, marginLeft: 2 }}>
          {unit}
        </span>
      </div>
      <Bar pct={v} tone={tone} />
    </div>
  );
}
