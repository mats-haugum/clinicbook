import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* BrowserRouter enables client-side routing.
        basename matches Vite's `base` config (import.meta.env.BASE_URL is
        set automatically from it) so routes still resolve correctly when
        the app is served under /projects/clinicbook/ instead of "/". */}
    <BrowserRouter basename={import.meta.env.BASE_URL}>
      {/* AuthProvider broadcasts auth state to every component below it */}
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
