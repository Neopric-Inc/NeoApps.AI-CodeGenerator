/** Registers the service worker (public/sw.js) so the app can be installed on desktop and phone home screens.
 *  Production builds only: during development Vite serves files that must not be cached. */
export function registerServiceWorker() {
  if (!import.meta.env.PROD || !("serviceWorker" in navigator)) return;
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("/sw.js").catch(() => undefined); // installable is a bonus, never an error
  });
}
