import axios from 'axios'

// import.meta.env is Vite's way of reading .env variables at build time.
// The VITE_ prefix is required — Vite strips variables without it for security.
const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
})

export default client
