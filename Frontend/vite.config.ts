import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [
    react(),
    // Tailwind CSS v4 — processed as a Vite plugin, no tailwind.config.js needed
    tailwindcss(),
  ],
})
