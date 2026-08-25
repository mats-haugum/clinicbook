import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// The function form of defineConfig lets us read the current Vite command
// (`serve` for `npm run dev`, `build` for `npm run build`).
export default defineConfig(({ command }) => ({
  // In production this app is served at app.matshaugum.com/projects/clinicbook/
  // (see deploy/edge/Caddyfile), so every asset URL Vite emits into index.html
  // must be prefixed with that path. Local dev keeps serving from the domain
  // root ("/") so `npm run dev` needs no extra path in the browser.
  base: command === 'build' ? '/projects/clinicbook/' : '/',
  plugins: [
    react(),
    // Tailwind CSS v4 — processed as a Vite plugin, no tailwind.config.js needed
    tailwindcss(),
  ],
}))
