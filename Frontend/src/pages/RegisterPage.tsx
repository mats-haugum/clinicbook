import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { register, getGuestPrefill } from '../api/auth'
import { useAuth } from '../context/AuthContext'
import { extractApiError } from '../utils/apiError'

export default function RegisterPage() {
  // Required fields
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName]   = useState('')
  const [email, setEmail]         = useState('')
  const [password, setPassword]   = useState('')
  const [birthdate, setBirthdate] = useState('')
  const [gender, setGender]       = useState('')

  // Optional sensitive PII — stored only for registered patients
  const [ssn, setSsn]                               = useState('')
  const [taxNumber, setTaxNumber]                   = useState('')
  const [religion, setReligion]                     = useState('')
  const [driversLicense, setDriversLicense]         = useState('')
  const [insuranceMember, setInsuranceMember]       = useState('')

  const [loading, setLoading]       = useState(false)
  const [error, setError]           = useState<string | null>(null)
  const [prefillNote, setPrefillNote] = useState<string | null>(null)

  const auth     = useAuth()
  const navigate = useNavigate()
  // useLocation gives access to the navigation state passed via React Router's <Link state={...}>
  const location = useLocation()

  // Attempt to fetch guest data for the given email and pre-fill the form.
  // Called on mount (when navigating from the booking success screen) and on email blur.
  async function tryPrefill(emailValue: string) {
    if (!emailValue) return
    try {
      const data = await getGuestPrefill(emailValue)
      setFirstName(data.firstName)
      setLastName(data.lastName)
      setEmail(data.email)
      // data.birthdate comes as "2000-01-15T00:00:00" — take just the date part for the input
      setBirthdate(data.birthdate.split('T')[0])
      setGender(data.gender)
      setPrefillNote("We found your previous booking — some fields have been pre-filled.")
    } catch {
      // No guest record found — silently do nothing so the user can still register fresh
    }
  }

  // On mount: if the user came from the booking success screen, the email will be in
  // navigation state. Run the pre-fill lookup immediately with that email.
  useEffect(() => {
    const stateEmail = (location.state as { email?: string } | null)?.email
    if (stateEmail) {
      tryPrefill(stateEmail)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function handleSubmit() {
    setLoading(true)
    setError(null)

    try {
      const response = await register({
        firstName, lastName, email, password, birthdate, gender,
        // Only include optional fields when the user actually typed something.
        // Sending an empty string would store "" in the DB; undefined tells the
        // JSON serializer to omit the field, so the backend leaves the column null.
        ssn:                  ssn               || undefined,
        taxNumber:            taxNumber         || undefined,
        religion:             religion          || undefined,
        driversLicenseNumber: driversLicense    || undefined,
        insuranceMemberNumber: insuranceMember  || undefined,
      })
      auth.login(response.token, response.refreshToken)
      navigate('/')
    } catch (err: unknown) {
      const status = (err as { response?: { status: number } }).response?.status
      if (status === 409 || status === 400) {
        setError(extractApiError(err, 'Please check all fields and try again.'))
      } else {
        setError('Something went wrong. Please try again.')
      }
    } finally {
      setLoading(false)
    }
  }

  const inputClass = 'w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent'

  return (
    <div className="min-h-[70vh] flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md bg-white border border-gray-200 rounded-2xl shadow-sm p-8">
        <h1 className="text-2xl font-bold text-primary mb-1">Create an Account</h1>
        <p className="text-gray-500 text-sm mb-6">
          Already have an account?{' '}
          <Link to="/login" className="text-accent hover:underline">Sign in here</Link>
        </p>

        <form onSubmit={e => { e.preventDefault(); handleSubmit() }} className="space-y-4">

          {/* ── Required personal information ── */}
          <fieldset className="border border-gray-200 rounded-xl p-5 space-y-4">
            <legend className="text-sm font-semibold text-primary px-1">Personal Information</legend>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-dark mb-1">First Name</label>
                <input type="text" value={firstName} onChange={e => setFirstName(e.target.value)}
                  required className={inputClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Last Name</label>
                <input type="text" value={lastName} onChange={e => setLastName(e.target.value)}
                  required className={inputClass} />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-dark mb-1">Email</label>
              <input type="email" value={email}
                onChange={e => setEmail(e.target.value)}
                onBlur={e => tryPrefill(e.target.value)}
                required className={inputClass} />
            </div>

            <div>
              <label className="block text-sm font-medium text-dark mb-1">Password</label>
              <input type="password" value={password} onChange={e => setPassword(e.target.value)}
                required minLength={8} className={inputClass} />
              <p className="text-xs text-gray-400 mt-1">Minimum 8 characters</p>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Date of Birth</label>
                <input type="date" value={birthdate} onChange={e => setBirthdate(e.target.value)}
                  required className={inputClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Gender</label>
                <select value={gender} onChange={e => setGender(e.target.value)}
                  required className={`${inputClass} bg-white`}>
                  <option value="">Select</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                  <option value="Prefer not to say">Prefer not to say</option>
                </select>
              </div>
            </div>
          </fieldset>

          {/* ── Optional sensitive PII (registered patients only) ── */}
          <fieldset className="border border-gray-200 rounded-xl p-5 space-y-4">
            <legend className="text-sm font-semibold text-primary px-1">Additional Information</legend>
            <p className="text-xs text-gray-400 -mt-1">
              All fields below are optional. This information is only stored for registered patients and is never shared with third parties.
            </p>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-dark mb-1">SSN</label>
                <input type="text" value={ssn} onChange={e => setSsn(e.target.value)}
                  placeholder="e.g. 123-45-6789" className={inputClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Tax Number</label>
                <input type="text" value={taxNumber} onChange={e => setTaxNumber(e.target.value)}
                  className={inputClass} />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-dark mb-1">Religion</label>
              <input type="text" value={religion} onChange={e => setReligion(e.target.value)}
                className={inputClass} />
            </div>

            <div>
              <label className="block text-sm font-medium text-dark mb-1">Driver's License Number</label>
              <input type="text" value={driversLicense} onChange={e => setDriversLicense(e.target.value)}
                className={inputClass} />
            </div>

            <div>
              <label className="block text-sm font-medium text-dark mb-1">Medical Insurance Member Number</label>
              <input type="text" value={insuranceMember} onChange={e => setInsuranceMember(e.target.value)}
                className={inputClass} />
            </div>
          </fieldset>

          {prefillNote && (
            <p className="text-accent bg-mint border border-accent/20 rounded-lg px-4 py-3 text-sm">
              {prefillNote}
            </p>
          )}

          {error && (
            <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm">
              {error}
            </p>
          )}

          <button type="submit" disabled={loading}
            className="w-full bg-accent text-white py-2 rounded-lg font-medium hover:bg-primary transition-colors disabled:opacity-50">
            {loading ? 'Creating account...' : 'Create Account'}
          </button>
        </form>
      </div>
    </div>
  )
}
