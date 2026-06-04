import { useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

function Logo() {
  return (
    <Link to="/" className="flex items-center gap-2">
      <svg width="36" height="36" viewBox="0 0 36 36" fill="none" aria-hidden="true">
        <rect width="36" height="36" rx="8" fill="#00b2a9" />
        <rect x="15" y="7" width="6" height="22" rx="2" fill="white" />
        <rect x="7" y="15" width="22" height="6" rx="2" fill="white" />
      </svg>
      <span className="text-xl font-bold text-primary">ClinicBook</span>
    </Link>
  )
}

// onClick is optional — used in the mobile menu to close it after navigating
function NavItem({ to, label, inactiveColor = 'text-dark', onClick }: {
  to: string; label: string; inactiveColor?: string; onClick?: () => void
}) {
  return (
    <NavLink
      to={to}
      onClick={onClick}
      className={({ isActive }) =>
        `text-sm font-medium transition-colors ${
          isActive
            ? 'text-accent border-b-2 border-accent'
            : `${inactiveColor} hover:text-accent`
        }`
      }
    >
      {label}
    </NavLink>
  )
}

export default function Header() {
  const { isLoggedIn, user, logout } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)

  function handleLogout() {
    logout()
    navigate('/book')
  }

  function closeMenu() {
    setMenuOpen(false)
  }

  return (
    <header>
      {/* Top utility bar */}
      <div className="bg-primary text-white text-xs py-2 px-6">
        <div className="max-w-5xl mx-auto flex justify-end items-center gap-4">
          {isLoggedIn
            && <span className="text-white/70">Welcome back, {user?.firstName}</span>
          }
          <div className="flex items-center gap-6">
            {isLoggedIn ? (
              <button
                onClick={handleLogout}
                className="text-white hover:text-accent transition-colors font-medium"
              >
                Sign Out
              </button>
            ) : (
              <>
                <NavItem to="/login"    label="Sign In"  inactiveColor="text-white" />
                <NavItem to="/register" label="Register" inactiveColor="text-white" />
              </>
            )}
          </div>
        </div>
      </div>

      {/* Main navigation bar */}
      <div className="bg-white border-b border-gray-200 px-6 py-5">
        <div className="max-w-5xl mx-auto flex items-center justify-between">
          <Logo />

          {/* Desktop nav — hidden below the md breakpoint (768px) */}
          <nav className="hidden md:flex items-center gap-8">
            {isLoggedIn && <NavItem to="/" label="My Appointments" />}
            <NavItem to="/book"   label="Book Appointment" />
            <NavItem to="/search" label="Find a Doctor" />
          </nav>

          {/* Hamburger button — only visible below the md breakpoint */}
          <button
            onClick={() => setMenuOpen(prev => !prev)}
            className="md:hidden p-2 rounded-lg hover:bg-gray-100 transition-colors"
            aria-label="Toggle menu"
            aria-expanded={menuOpen}
          >
            {menuOpen ? (
              // X icon when the menu is open
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            ) : (
              // Three-line hamburger icon when the menu is closed
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            )}
          </button>
        </div>
      </div>

      {/* Mobile dropdown — only rendered when menuOpen is true, hidden on md+ */}
      {menuOpen && (
        <div className="md:hidden bg-white border-b border-gray-200 px-6 py-4">
          <nav className="flex flex-col gap-4">
            {isLoggedIn && <NavItem to="/" label="My Appointments" onClick={closeMenu} />}
            <NavItem to="/book"   label="Book Appointment" onClick={closeMenu} />
            <NavItem to="/search" label="Find a Doctor"    onClick={closeMenu} />
          </nav>
        </div>
      )}
    </header>
  )
}
