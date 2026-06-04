import axios from 'axios'
import client from './client'

// Mirrors AuthResponse.cs
export interface AuthResponse {
  token: string
  refreshToken: string
  firstName: string
  lastName: string
  email: string
}

// POST /auth/login
export async function login(email: string, password: string): Promise<AuthResponse> {
  const response = await client.post<AuthResponse>('/auth/login', { email, password })
  return response.data
}

// Mirrors RegisterRequest.cs — required fields plus optional sensitive PII
export interface RegisterPayload {
  firstName: string
  lastName: string
  email: string
  password: string
  birthdate: string
  gender: string
  // Optional — only collected for registered patients, not guests
  ssn?: string
  taxNumber?: string
  religion?: string
  driversLicenseNumber?: string
  insuranceMemberNumber?: string
}

// POST /auth/register
export async function register(payload: RegisterPayload): Promise<AuthResponse> {
  const response = await client.post<AuthResponse>('/auth/register', payload)
  return response.data
}

// Mirrors AdminAuthResponse.cs
export interface AdminAuthResponse {
  token: string
  firstName: string
  lastName: string
  email: string
}

// POST /admin/auth/login
export async function adminLogin(email: string, password: string): Promise<AdminAuthResponse> {
  const response = await client.post<AdminAuthResponse>('/admin/auth/login', { email, password })
  return response.data
}

// Mirrors GuestPrefillResponse.cs — non-sensitive PII only
export interface GuestPrefillResponse {
  firstName: string
  lastName: string
  email: string
  birthdate: string
  gender: string
}

// GET /auth/guest-prefill?email=... — returns data for a guest booking to pre-fill the register form
export async function getGuestPrefill(email: string): Promise<GuestPrefillResponse> {
  const response = await client.get<GuestPrefillResponse>('/auth/guest-prefill', {
    params: { email },
  })
  return response.data
}

// POST /auth/refresh — called directly with axios (not through client) so the
// response interceptor does not intercept it and cause an infinite loop
export async function refreshTokens(refreshToken: string): Promise<AuthResponse> {
  const response = await axios.post<AuthResponse>(
    `${import.meta.env.VITE_API_URL}/auth/refresh`,
    { refreshToken }
  )
  return response.data
}
