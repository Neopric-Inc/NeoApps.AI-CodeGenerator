# Route map (custom screen sample)

Draws a recorded route with react-leaflet: GPS points as a line, start/finish dots, and stops as markers, on a page
opened from a "Route map" button on the parent record. Written for a walk tracker with tables `walks`,
`walk_points (walk_id, point_number, latitude, longitude)` and `walk_events (walk_id, event_type, latitude, longitude)`;
rename the tables and columns for your app.

Wire it up in `custom/index.tsx`:

```tsx
import { WalkMap, WalkMapButton } from "./RouteMap";
export const extensions: Extensions = {
  routes: [{ path: "/walks/:id/map", element: <WalkMap /> }],
  recordActions: WalkMapButton,
};
```

Tiles come from OpenStreetMap (keep the attribution). Heavy traffic needs a tile provider instead.
