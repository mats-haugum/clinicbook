import { createContext, useContext, useState, type ReactNode } from 'react'

interface AuthUser {
  id: number
  email: string
  firstName: string
  lastName: string
  role: string
}

interface AuthContextType {
  token: string | null
  user: AuthUser | null
  isLoggedIn: boolean
  isAdmin: boolean
  // refreshToken is optional — admins receive no refresh token
  login: (token: string, refreshToken?: string) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

function decodeToken(token: string): AuthUser {
  const payload = JSON.parse(atob(token.split('.')[1]))
  return {
    id:        Number(payload.sub),
    email:     payload.email,
    firstName: payload.given_name,
    lastName:  payload.family_name,
    role:      payload.role ?? 'Patient',
  }
}

// Checks the exp (expiry) claim in the JWT.
// exp is a Unix timestamp in seconds — Date.now() is in milliseconds, hence the divide.
function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return payload.exp < Date.now() / 1000
  } catch {
    return true
  }
}

// Reads the access token from localStorage, but discards it if it has already expired.
// This handles the case where the user returns after a long absence.
// Note: the response interceptor in client.ts handles expiry that happens mid-session.
function readStoredToken(): string | null {
  const stored = localStorage.getItem('token')
  if (!stored || isTokenExpired(stored)) {
    localStorage.removeItem('token')
    return null
  }
  return stored
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(readStoredToken)
  const [user, setUser]   = useState<AuthUser | null>(() => {
    const t = readStoredToken()
    return t ? decodeToken(t) : null
  })

  // Accepts both tokens — the access token drives React state, the refresh token
  // is stored in localStorage so the interceptor can read it for silent refresh.
  // refreshToken is optional because admin logins do not issue a refresh token.
  function login(newToken: string, newRefreshToken?: string) {
    localStorage.setItem('token', newToken)
    if (newRefreshToken) {
      localStorage.setItem('refreshToken', newRefreshToken)
    } else {
      localStorage.removeItem('refreshToken')
    }
    setToken(newToken)
    setUser(decodeToken(newToken))
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    setToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ token, user, isLoggedIn: user?.role === 'Patient', isAdmin: user?.role === 'Admin', login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
