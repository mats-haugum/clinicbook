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

// Endpoints where a 401 means "wrong credentials", not "expired access token".
// The interceptor must let these errors through untouched so the calling page
// (e.g. LoginPage) can catch them and show an error message to the user.
const CREDENTIAL_ENDPOINTS = ['/auth/login', '/admin/auth/login', '/auth/register']

// window.location bypasses React Router, so it does not know about the router's basename.
// BASE_URL is Vite's `base` setting: '/' in dev, '/projects/clinicbook/' in production.
const LOGIN_URL = `${import.meta.env.BASE_URL}login`

// Response interceptor — catches 401 errors and attempts a silent token refresh.
// If the refresh succeeds, the original request is retried with the new token.
// If the refresh fails, the user is logged out and redirected to /login.
client.interceptors.response.use(
  response => response,
  async error => {
    const original = error.config

    // original.url is the path the request was sent to, e.g. '/auth/login'
    const isCredentialRequest = CREDENTIAL_ENDPOINTS.includes(original?.url)

    // _retry flag prevents an infinite loop if the refresh request itself returns 401
    if (error.response?.status === 401 && !original._retry && !isCredentialRequest) {
      original._retry = true

      const storedRefreshToken = localStorage.getItem('refreshToken')

      if (!storedRefreshToken) {
        localStorage.removeItem('token')
        window.location.href = LOGIN_URL
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
        window.location.href = LOGIN_URL
        return Promise.reject(error)
      }
    }

    return Promise.reject(error)
  }
)

export default client
