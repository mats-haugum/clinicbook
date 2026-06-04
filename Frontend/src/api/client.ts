import axios from 'axios'
import { refreshTokens } from './auth'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
})

// Request interceptor — attaches the access token to every outgoing request
client.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Response interceptor — catches 401 errors and attempts a silent token refresh.
// If the refresh succeeds, the original request is retried with the new token.
// If the refresh fails, the user is logged out and redirected to /login.
client.interceptors.response.use(
  response => response,
  async error => {
    const original = error.config

    // _retry flag prevents an infinite loop if the refresh request itself returns 401
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true

      const storedRefreshToken = localStorage.getItem('refreshToken')

      if (!storedRefreshToken) {
        localStorage.removeItem('token')
        window.location.href = '/login'
        return Promise.reject(error)
      }

      try {
        // refreshTokens() uses raw axios (not this client) to avoid triggering this interceptor again
        const data = await refreshTokens(storedRefreshToken)

        localStorage.setItem('token', data.token)
        localStorage.setItem('refreshToken', data.refreshToken)

        // Update the Authorization header on the original failed request and retry it
        original.headers.Authorization = `Bearer ${data.token}`
        return client(original)
      } catch {
        // Refresh failed — the refresh token is also expired or revoked
        localStorage.removeItem('token')
        localStorage.removeItem('refreshToken')
        window.location.href = '/login'
        return Promise.reject(error)
      }
    }

    return Promise.reject(error)
  }
)

export default client
