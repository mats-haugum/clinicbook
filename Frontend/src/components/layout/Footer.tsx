import { Link } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

// A simple styled link for use inside the footer columns
function FooterLink({ to, label }: { to: string; label: string }) {
  return (
    <Link to={to} className="text-sm text-gray-600 hover:text-accent transition-colors">
      {label}
    </Link>
  )
}

export default function Footer() {
  const year = new Date().getFullYear()
  const { isLoggedIn } = useAuth()

  return (
    <footer>
      {/* Top tier — mint background with three columns */}
      <div className="bg-mint px-6 py-10">
        <div className="max-w-5xl mx-auto grid grid-cols-1 gap-8 sm:flex sm:justify-center sm:gap-24 text-center sm:text-left">

          {/* Column 1: Brand */}
          <div>
            <div className="flex items-center gap-2 mb-3 justify-center sm:justify-start">
              <svg width="32" height="32" viewBox="0 0 36 36" fill="none" aria-hidden="true">
                <rect width="36" height="36" rx="8" fill="#00b2a9" />
                <rect x="15" y="7" width="6" height="22" rx="2" fill="white" />
                <rect x="7" y="15" width="22" height="6" rx="2" fill="white" />
              </svg>
              <span className="text-lg font-bold text-primary">ClinicBook</span>
            </div>
            <p className="text-sm text-gray-600">Quality healthcare, closer to home.</p>
          </div>

          {/* Column 2: For Admins */}
          <div>
            <h3 className="text-xs font-semibold text-primary uppercase tracking-wider mb-3">
              For Admins
            </h3>
            <div className="flex flex-col gap-2">
              <FooterLink to="/admin/login"   label="Admin Dashboard" />

            </div>
          </div>

          {/* Column 3: For Patients — same links regardless of auth state */}
          <div>
            <h3 className="text-xs font-semibold text-primary uppercase tracking-wider mb-3">
              For Patients
            </h3>
            <div className="flex flex-col gap-2">
              <FooterLink to="/book"   label="Book an Appointment" />
              <FooterLink to="/search" label="Find a Doctor" />
            </div>
          </div>

          {/* Column 43: changes based on whether the user is logged in */}
          <div>
            <h3 className="text-xs font-semibold text-primary uppercase tracking-wider mb-3">
              Account
            </h3>
            <div className="flex flex-col gap-2">
              {isLoggedIn ? (
                <FooterLink to="/" label="My Appointments" />
              ) : (
                <>
                  <FooterLink to="/login"    label="Sign In" />
                  <FooterLink to="/register" label="Register" />
                </>
              )}
            </div>
          </div>

        </div>
      </div>

      {/* Bottom strip — dark teal with copyright */}
      <div className="bg-primary text-white py-4 px-6 text-center text-xs">
        <p>&copy; {year} ClinicBook. All rights reserved.</p>
      </div>
    </footer>
  )
}
