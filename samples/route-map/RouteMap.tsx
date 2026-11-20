import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { MapContainer, TileLayer, Polyline, CircleMarker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { getRecord, listWhere } from "../neoapps/api";

type Pt = [number, number];

/** Route of one walk: the GPS points as a line, start/end dots, and pee/poop stops. */
export function WalkMap() {
  const { id } = useParams();
  const [walk, setWalk] = useState<any>(null);
  const [route, setRoute] = useState<Pt[]>([]);
  const [stops, setStops] = useState<any[]>([]);
  const [msg, setMsg] = useState("Loading route…");

  useEffect(() => {
    (async () => {
      try {
        const [w, pts, evs] = await Promise.all([
          getRecord("walks", id),
          listWhere("walk_points", "walk_id", id, 1000, "point_number|ASC"),
          listWhere("walk_events", "walk_id", id),
        ]);
        setWalk(w);
        setRoute(pts.map((p) => [Number(p.latitude), Number(p.longitude)] as Pt));
        setStops(evs.filter((e) => e.latitude != null && e.longitude != null));
        setMsg(pts.length ? "" : "No GPS points were saved for this walk.");
      } catch (e: any) {
        setMsg("Could not load the route: " + e.message);
      }
    })();
  }, [id]);

  const center: Pt = route[0] ?? [38.25, -85.75];
  return (
    <div style={{ padding: 16 }}>
      <h2 style={{ margin: "0 0 4px" }}>Walk route</h2>
      {walk && (
        <div style={{ color: "#667085", marginBottom: 12 }}>
          {String(walk.started_at).replace("T", " ").slice(0, 16)} · {Number(walk.distance_miles).toFixed(2)} mi ·{" "}
          {Math.round(Number(walk.duration_seconds) / 60)} min · {stops.length} stop{stops.length === 1 ? "" : "s"}
        </div>
      )}
      {msg && <div style={{ marginBottom: 12 }}>{msg}</div>}
      {route.length > 0 && (
        <MapContainer bounds={route.length > 1 ? route : undefined} center={center} zoom={16} style={{ height: "65vh", borderRadius: 12 }}>
          <TileLayer url="https://tile.openstreetmap.org/{z}/{x}/{y}.png" attribution="&copy; OpenStreetMap contributors" />
          <Polyline positions={route} pathOptions={{ color: "#00B8D4", weight: 5 }} />
          <CircleMarker center={route[0]} radius={7} pathOptions={{ color: "#12b76a", fillOpacity: 1 }}><Popup>Start</Popup></CircleMarker>
          <CircleMarker center={route[route.length - 1]} radius={7} pathOptions={{ color: "#d92d20", fillOpacity: 1 }}><Popup>Finish</Popup></CircleMarker>
          {stops.map((s) => (
            <CircleMarker key={s.walk_event_id} center={[Number(s.latitude), Number(s.longitude)]} radius={6}
              pathOptions={{ color: s.event_type === "poop" ? "#7a4b2a" : "#f5b700", fillOpacity: 0.9 }}>
              <Popup>{s.event_type === "poop" ? "💩 Poop" : "💧 Pee"} · {String(s.recorded_at).replace("T", " ").slice(11, 16)}</Popup>
            </CircleMarker>
          ))}
        </MapContainer>
      )}
    </div>
  );
}

/** "Route map" button on a walk's page. */
export function WalkMapButton({ table, record }: { table: string; record: Record<string, any> }) {
  const nav = useNavigate();
  if (table !== "walks") return null;
  return <button onClick={() => nav(`/walks/${record.walk_id}/map`)} style={{ padding: "6px 12px", borderRadius: 8 }}>🗺️ Route map</button>;
}
