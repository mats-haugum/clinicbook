import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { getMyAppointments, cancelAppointment, type AppointmentResponse } from '../api/appointments'
import RescheduleModal from '../components/appointments/RescheduleModal'
import { extractApiError } from '../utils/apiError'

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('en-ZA', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

export default function HomePage() {
  const { isLoggedIn, user } = useAuth()

  const [appointments, setAppointments] = useState<AppointmentResponse[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [cancelling, setCancelling]     = useState<number | null>(null)

  // The appointment currently open in the reschedule modal (null = modal closed)
  const [rescheduling, setRescheduling] = useState<AppointmentResponse | null>(null)

  if (!isLoggedIn) return <Navigate to="/book" replace />

  useEffect(() => {
    async function load() {
      try {
        const data = await getMyAppointments()
        setAppointments(data)
      } catch {
        setError('Could not load your appointments.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  async function handleCancel(id: number) {
    setCancelling(id)
    try {
      await cancelAppointment(id)
      setAppointments(prev => prev.filter(a => a.id !== id))
    } catch (err: unknown) {
      setError(extractApiError(err, 'Could not cancel that appointment. Please try again.'))
    } finally {
      setCancelling(null)
    }
  }

  // Called by the modal when the reschedule API call succeeds
  function handleRescheduleSuccess(updated: AppointmentResponse) {
    setAppointments(prev => prev.map(a => a.id === updated.id ? updated : a))
    setRescheduling(null)
  }

  return (
    <div className="max-w-3xl mx-auto px-6 py-12">
      <h1 className="text-3xl font-bold text-primary mb-1">My Appointments</h1>
      <p className="text-gray-500 mb-8">Welcome back, {user?.firstName}.</p>

      {loading && (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {error && (
        <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{error}</p>
      )}

      {!loading && appointments.length === 0 && !error && (
        <div className="text-center py-16 text-gray-500">
          <p className="text-lg mb-4">You have no upcoming appointments.</p>
          <Link
            to="/book"
            className="bg-accent text-white px-6 py-2 rounded-lg font-medium hover:bg-primary transition-colors"
          >
            Book an Appointment
          </Link>
        </div>
      )}

      {appointments.length > 0 && (
        <ul className="space-y-4">
          {appointments.map(appointment => (
            <li
              key={appointment.id}
              className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm flex justify-between items-start gap-4"
            >
              <div>
                <p className="text-lg font-semibold text-primary">{appointment.doctorFullName}</p>
                <p className="text-sm text-gray-500 mt-1">
                  {appointment.categoryName} &middot; {appointment.clinicName}
                </p>
                <p className="text-sm text-dark mt-2">
                  {formatDate(appointment.startTime)} &rarr; {formatDate(appointment.endTime)}
                </p>
              </div>

              <div className="flex flex-col gap-2 shrink-0">
                <button
                  onClick={() => setRescheduling(appointment)}
                  className="text-sm text-accent border border-accent rounded-lg px-4 py-2 hover:bg-mint transition-colors"
                >
                  Reschedule
                </button>
                <button
                  onClick={() => handleCancel(appointment.id)}
                  disabled={cancelling === appointment.id}
                  className="text-sm text-red-500 border border-red-200 rounded-lg px-4 py-2 hover:bg-red-50 transition-colors disabled:opacity-50"
                >
                  {cancelling === appointment.id ? 'Cancelling...' : 'Cancel'}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {/* Reschedule modal — rendered at the root level so it overlays everything */}
      {rescheduling && (
        <RescheduleModal
          appointment={rescheduling}
          onSuccess={handleRescheduleSuccess}
          onClose={() => setRescheduling(null)}
        />
      )}
    </div>
  )
}
