import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { login } from '../api/auth'
import { useAuth } from '../context/AuthContext'
import { extractApiError } from '../utils/apiError'

export default function LoginPage() {
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading]   = useState(false)
  const [error, setError]       = useState<string | null>(null)

  // useAuth gives us the login() function to save the token after a successful call
  const auth = useAuth()

  // useNavigate returns a function we can call to redirect the user programmatically
  const navigate = useNavigate()

  async function handleSubmit() {
    setLoading(true)
    setError(null)

    try {
      const response = await login(email, password)
      // Save the token in context + localStorage
      auth.login(response.token, response.refreshToken)
      // Redirect to the home page
      navigate('/')
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
    <div className="min-h-[70vh] flex items-center justify-center px-4">
      <div className="w-full max-w-md bg-white border border-gray-200 rounded-2xl shadow-sm p-8">
        <h1 className="text-2xl font-bold text-primary mb-1">Sign In</h1>
        <p className="text-gray-500 text-sm mb-6">
          Don't have an account?{' '}
          <Link to="/register" className="text-accent hover:underline">Register here</Link>
        </p>

        <form
          onSubmit={e => { e.preventDefault(); handleSubmit() }}
          className="space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-dark mb-1">Email</label>
            <input
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-dark mb-1">Password</label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent"
            />
          </div>

          {error && (
            <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-accent text-white py-2 rounded-lg font-medium hover:bg-primary transition-colors disabled:opacity-50"
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  )
}
