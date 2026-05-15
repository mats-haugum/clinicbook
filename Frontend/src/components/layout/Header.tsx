import { Link, NavLink } from 'react-router-dom'

// Placeholder logo — replace the SVG src once the real logo is available
function Logo() {
  return (
    <Link to="/" className="flex items-center gap-2">
      {/* Placeholder: a simple medical cross in accent colour */}
      <svg width="36" height="36" viewBox="0 0 36 36" fill="none" aria-hidden="true">
        <rect width="36" height="36" rx="8" fill="#00b2a9" />
        <rect x="15" y="7" width="6" height="22" rx="2" fill="white" />
        <rect x="7" y="15" width="22" height="6" rx="2" fill="white" />
      </svg>
      <span className="text-xl font-bold text-primary">ClinicBook</span>
    </Link>
  )
}

// NavLink is like Link but adds an "active" class when the URL matches.
// inactiveColor lets the caller override the default dark text (e.g. white for the top bar).
function NavItem({ to, label, inactiveColor = 'text-dark' }: { to: string; label: string; inactiveColor?: string }) {
  return (
    <NavLink
      to={to}
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
  return (
    <header>
      {/* Top utility bar */}
      <div className="bg-primary text-white text-xs py-2 px-6 flex justify-end gap-6">
        <NavItem to="/login"    label="Sign In"  inactiveColor="text-white" />
        <NavItem to="/register" label="Register" inactiveColor="text-white" />
      </div>

      {/* Main navigation bar */}
      <div className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
        <Logo />
        <nav className="flex items-center gap-8">
          <NavItem to="/"       label="Book Appointment" />
          <NavItem to="/search" label="Find a Doctor" />
        </nav>
      </div>
    </header>
  )
}
