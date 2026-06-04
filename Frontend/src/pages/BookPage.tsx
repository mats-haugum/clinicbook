import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { getAllDoctors, type DoctorResponse } from '../api/doctors'
import { getAllClinics, type ClinicResponse } from '../api/clinics'
import { getAllCategories, type CategoryResponse } from '../api/categories'
import { bookAsPatient, bookAsGuest, type AppointmentResponse } from '../api/appointments'
import SlotPicker from '../components/appointments/SlotPicker'
import { extractApiError } from '../utils/apiError'

// Formats a Date as 'YYYY-MM-DDTHH:MM:SS' using local time — no UTC conversion.
// The backend and the slot generator both use wall-clock time, so we must match that.
function formatLocalIso(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
         `T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

export default function BookPage() {
  const { isLoggedIn } = useAuth()

  // --- Dropdown data loaded from the API ---
  const [doctors, setDoctors]       = useState<DoctorResponse[]>([])
  const [clinics, setClinics]       = useState<ClinicResponse[]>([])
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [loadingData, setLoadingData] = useState(true)

  // --- Appointment fields (both guest and registered) ---
  const [doctorId, setDoctorId]     = useState('')
  const [clinicId, setClinicId]     = useState('')
  const [categoryId, setCategoryId] = useState('')
  // startTime is set by SlotPicker when the user clicks a slot ('YYYY-MM-DDTHH:MM')
  const [startTime, setStartTime]   = useState('')
  // All slots from the picker are exactly 30 minutes
  const [duration]                  = useState('30')

  // --- Guest-only fields ---
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName]   = useState('')
  const [email, setEmail]         = useState('')
  const [birthdate, setBirthdate] = useState('')
  const [gender, setGender]       = useState('')

  // --- Submission state ---
  const [submitting, setSubmitting]   = useState(false)
  const [error, setError]             = useState<string | null>(null)
  const [booked, setBooked]           = useState<AppointmentResponse | null>(null)

  // Load all dropdown data once when the component mounts
  useEffect(() => {
    async function loadOptions() {
      try {
        // Promise.all runs all three API calls in parallel instead of one-by-one,
        // which means the page loads faster
        const [doctors, clinics, categories] = await Promise.all([
          getAllDoctors(),
          getAllClinics(),
          getAllCategories(),
        ])
        setDoctors(doctors)
        setClinics(clinics)
        setCategories(categories)
      } catch {
        setError('Could not load booking options. Please refresh the page.')
      } finally {
        setLoadingData(false)
      }
    }
    loadOptions()
  }, [])

  async function handleSubmit() {
    setSubmitting(true)
    setError(null)

    // Calculate end time by adding the duration (in minutes) to the start time.
    // new Date(startTime) parses the slot string (e.g. "2026-05-22T08:00") as local time.
    // getTime() returns milliseconds — we add duration * 60 * 1000 to reach the end time.
    const start = new Date(startTime)
    const end   = new Date(start.getTime() + Number(duration) * 60 * 1000)

    // We intentionally send local wall-clock time (NOT UTC via toISOString) so that
    // the backend's slot generator — which also works in local time — can compare
    // correctly. toISOString() would shift the time to UTC and break the overlap check.
    const startIso = formatLocalIso(start)
    const endIso   = formatLocalIso(end)

    try {
      let result: AppointmentResponse

      if (isLoggedIn) {
        result = await bookAsPatient(
          Number(doctorId), Number(clinicId), Number(categoryId),
          startIso, endIso
        )
      } else {
        result = await bookAsGuest(
          firstName, lastName, email, birthdate, gender,
          Number(doctorId), Number(clinicId), Number(categoryId),
          startIso, endIso
        )
      }

      setBooked(result)
    } catch (err: unknown) {
      const status = (err as { response?: { status: number } }).response?.status
      if (status === 409 || status === 404) {
        // The backend sends { message: "..." } for business-logic errors —
        // display it directly so the user gets the exact reason (slot conflict,
        // email already registered, doctor not found, etc.)
        setError(extractApiError(err, 'Something went wrong. Please try again.'))
      } else if (status === 400) {
        // 400 can be either a backend message or an ASP.NET Core ProblemDetails
        // object (model-binding failure). extractApiError falls back to generic.
        setError(extractApiError(err, 'Please check all fields and try again.'))
      } else {
        setError('Something went wrong. Please try again.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  // Success screen — shown after a successful booking
  if (booked) {
    return (
      <div className="max-w-md mx-auto px-6 py-16 text-center">
        <div className="w-16 h-16 bg-mint rounded-full flex items-center justify-center mx-auto mb-4">
          <svg className="w-8 h-8 text-accent" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h1 className="text-2xl font-bold text-primary mb-2">Appointment Booked!</h1>
        <p className="text-gray-500 mb-1">Dr. {booked.doctorFullName}</p>
        <p className="text-gray-500 mb-1">{booked.clinicName} &middot; {booked.categoryName}</p>
        <p className="text-dark font-medium mt-3 mb-8">
          {new Date(booked.startTime).toLocaleString('en-ZA', { dateStyle: 'medium', timeStyle: 'short' })}
        </p>
        <div className="flex gap-3 justify-center">
          <button
            onClick={() => setBooked(null)}
            className="border border-accent text-accent px-5 py-2 rounded-lg hover:bg-mint transition-colors"
          >
            Book Another
          </button>
          {isLoggedIn && (
            <Link
              to="/"
              className="bg-accent text-white px-5 py-2 rounded-lg hover:bg-primary transition-colors"
            >
              My Appointments
            </Link>
          )}
        </div>

        {!isLoggedIn && (
          <div className="mt-6 border border-accent/25 bg-mint/40 rounded-xl p-5 text-center">
            <p className="text-sm font-semibold text-primary mb-1">No account yet?</p>
            <p className="text-xs text-gray-500 mb-4">
              Create a free account to view, reschedule, and cancel your appointments any time.
            </p>
            <Link
              to="/register"
              state={{ email }}
              className="inline-block bg-accent text-white px-6 py-2 rounded-lg font-medium hover:bg-primary transition-colors text-sm"
            >
              Create Account
            </Link>
          </div>
        )}
      </div>
    )
  }

  if (loadingData) {
    return (
      <div className="flex justify-center py-24">
        <div className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <div className="max-w-xl mx-auto px-6 py-12">
      <h1 className="text-3xl font-bold text-primary mb-2">Book an Appointment</h1>
      <p className="text-gray-500 mb-8">
        {isLoggedIn ? 'Fill in the appointment details below.' : 'No account needed — fill in your details to book.'}
      </p>

      <form onSubmit={e => { e.preventDefault(); handleSubmit() }} className="space-y-5">

        {/* Guest-only section — hidden when logged in */}
        {!isLoggedIn && (
          <fieldset className="border border-gray-200 rounded-xl p-5 space-y-4">
            <legend className="text-sm font-semibold text-primary px-1">Your Details</legend>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-dark mb-1">First Name</label>
                <input type="text" value={firstName} onChange={e => setFirstName(e.target.value)} required
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent" />
              </div>
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Last Name</label>
                <input type="text" value={lastName} onChange={e => setLastName(e.target.value)} required
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent" />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-dark mb-1">Email</label>
              <input type="email" value={email} onChange={e => setEmail(e.target.value)} required
                className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Date of Birth</label>
                <input type="date" value={birthdate} onChange={e => setBirthdate(e.target.value)} required
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent" />
              </div>
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Gender</label>
                <select value={gender} onChange={e => setGender(e.target.value)} required
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white">
                  <option value="">Select</option>
                  <option>Male</option>
                  <option>Female</option>
                  <option>Other</option>
                  <option value="Prefer not to say">Prefer not to say</option>
                </select>
              </div>
            </div>
          </fieldset>
        )}

        {/* Appointment details — shown to everyone */}
        <fieldset className="border border-gray-200 rounded-xl p-5 space-y-4">
          <legend className="text-sm font-semibold text-primary px-1">Appointment Details</legend>

          <div>
            <label className="block text-sm font-medium text-dark mb-1">Doctor</label>
            <select value={doctorId} onChange={e => setDoctorId(e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white">
              <option value="">Select a doctor</option>
              {doctors.map(d => (
                <option key={d.id} value={d.id}>
                  {d.firstName} {d.lastName} — {d.specialityName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-dark mb-1">Clinic</label>
            <select value={clinicId} onChange={e => setClinicId(e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white">
              <option value="">Select a clinic</option>
              {clinics.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-dark mb-1">Appointment Type</label>
            <select value={categoryId} onChange={e => setCategoryId(e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white">
              <option value="">Select a type</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          {/* SlotPicker replaces the datetime-local + duration inputs.
              It shows a date picker and a 4-column grid of 30-min slots (08:00–17:00),
              coloured by availability. Clicking a slot sets startTime in the parent. */}
          <SlotPicker
            doctorId={doctorId}
            selectedStartTime={startTime}
            onSlotSelect={setStartTime}
          />
        </fieldset>

        {error && (
          <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm">{error}</p>
        )}

        <button type="submit" disabled={submitting}
          className="w-full bg-accent text-white py-3 rounded-lg font-medium hover:bg-primary transition-colors disabled:opacity-50">
          {submitting ? 'Booking...' : 'Book Appointment'}
        </button>

        {!isLoggedIn && (
          <div className="border border-gray-200 rounded-xl p-4 text-center space-y-2">
            <p className="text-sm font-medium text-dark">Want to manage your appointments?</p>
            <p className="text-xs text-gray-400">Register for free or sign in to an existing account.</p>
            <div className="flex gap-3 justify-center pt-1">
              <Link
                to="/login"
                className="border border-accent text-accent px-4 py-1.5 rounded-lg text-sm hover:bg-mint transition-colors"
              >
                Sign In
              </Link>
              <Link
                to="/register"
                className="bg-accent text-white px-4 py-1.5 rounded-lg text-sm hover:bg-primary transition-colors"
              >
                Register
              </Link>
            </div>
          </div>
        )}
      </form>
    </div>
  )
}
