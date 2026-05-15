import { Routes, Route } from 'react-router-dom'
import Layout from './components/layout/Layout'
import BookPage from './pages/BookPage'
import SearchPage from './pages/SearchPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'

// Routes defines the URL map for the whole app.
// The Layout route wraps all pages so they share the same Header and Footer.
function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/"        element={<BookPage />} />
        <Route path="/book"    element={<BookPage />} />
        <Route path="/search"  element={<SearchPage />} />
        <Route path="/login"   element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>
    </Routes>
  )
}

export default App
