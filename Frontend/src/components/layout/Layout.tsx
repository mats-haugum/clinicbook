import { Outlet } from 'react-router-dom'
import Header from './Header'
import Footer from './Footer'

// Outlet is where the current page's content renders.
// Every page in App.tsx that is nested under this route gets injected here.
export default function Layout() {
  return (
    <div className="min-h-screen flex flex-col">
      <Header />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
