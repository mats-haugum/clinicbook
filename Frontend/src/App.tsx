import { Routes, Route } from 'react-router-dom'
import Layout from './components/layout/Layout'
import HomePage from './pages/HomePage'
import BookPage from './pages/BookPage'
import SearchPage from './pages/SearchPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import AdminLoginPage from './pages/admin/AdminLoginPage'
import AdminDashboard from './pages/admin/AdminDashboard'

// Route map:
// /             → home (appointments view if logged in, redirect to /book if not)
// /book         → booking form (guests and registered patients)
// /search       → doctor search
// /login        → patient sign in
// /register     → patient register
// /admin/login  → admin sign in (standalone, no patient nav)
// /admin        → admin dashboard — redirects to /admin/login if not an admin
function App() {
  return (
    <Routes>
      {/* Patient-facing routes — wrapped in the shared Layout (header + footer) */}
      <Route element={<Layout />}>
        <Route path="/"         element={<HomePage />} />
        <Route path="/book"     element={<BookPage />} />
        <Route path="/search"   element={<SearchPage />} />
        <Route path="/login"    element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      {/* Admin routes — standalone pages with no patient nav */}
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin"       element={<AdminDashboard />} />
    </Routes>
  )
}

export default App
