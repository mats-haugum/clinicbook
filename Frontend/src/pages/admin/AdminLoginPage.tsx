import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { adminLogin } from '../../api/auth'
import { useAuth } from '../../context/AuthContext'
import { extractApiError } from '../../utils/apiError'

export default function AdminLoginPage() {
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading]   = useState(false)
  const [error, setError]       = useState<string | null>(null)

  const auth     = useAuth()
  const navigate = useNavigate()

  async function handleSubmit() {
    setLoading(true)
    setError(null)
    try {
      const response = await adminLogin(email, password)
      // No refresh token for admins — login() accepts it as optional
      auth.login(response.token)
      navigate('/admin')
    } catch (err: unknown) {
      const status = (err as { response?: { status: number } }).response?.status
      if (status === 401) {
        setError(extractApiError(err, 'Invalid email or password.'))
      } else {
        setError('Something went wrong. Please try again.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="w-full max-w-sm bg-white border border-gray-200 rounded-2xl shadow-sm p-8">
        <Link to="/" className="text-xs text-gray-400 hover:text-accent transition-colors mb-4 inline-block">
          ← Back to patient site
        </Link>
        <div className="flex items-center gap-2 mb-6">
          <svg width="28" height="28" viewBox="0 0 36 36" fill="none" aria-hidden="true">
            <rect width="36" height="36" rx="8" fill="#00b2a9" />
            <rect x="15" y="7" width="6" height="22" rx="2" fill="white" />
            <rect x="7" y="15" width="22" height="6" rx="2" fill="white" />
          </svg>
          <span className="text-lg font-bold text-primary">ClinicBook Admin</span>
        </div>

        <h1 className="text-xl font-bold text-dark mb-1">Admin Sign In</h1>
        <p className="text-sm text-gray-400 mb-6">Restricted to authorised staff only.</p>

        <form onSubmit={e => { e.preventDefault(); handleSubmit() }} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-dark mb-1">Email</label>
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent" />
          </div>
          <div>
            <label className="block text-sm font-medium text-dark mb-1">Password</label>
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent" />
          </div>

          {error && (
            <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm">{error}</p>
          )}

          <button type="submit" disabled={loading}
            className="w-full bg-accent text-white py-2 rounded-lg font-medium hover:bg-primary transition-colors disabled:opacity-50">
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  )
}
