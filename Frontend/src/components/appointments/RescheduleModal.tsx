import { useEffect, useState } from 'react'
import { getAllDoctors, type DoctorResponse } from '../../api/doctors'
import { getAllClinics, type ClinicResponse } from '../../api/clinics'
import { getAllCategories, type CategoryResponse } from '../../api/categories'
import { rescheduleAppointment, type AppointmentResponse } from '../../api/appointments'
import SlotPicker from './SlotPicker'
import { extractApiError } from '../../utils/apiError'

interface RescheduleModalProps {
  appointment: AppointmentResponse
  // Called with the updated appointment when the reschedule succeeds
  onSuccess: (updated: AppointmentResponse) => void
  onClose: () => void
}

// Builds an end time ISO string by adding 30 minutes (local wall-clock time, no UTC conversion)
function addThirtyMinutes(localIso: string): string {
  const d = new Date(localIso)
  d.setMinutes(d.getMinutes() + 30)
  const pad = (n: number) => String(n).padStart(2, '0')
  return (
    `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
    `T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
  )
}

export default function RescheduleModal({ appointment, onSuccess, onClose }: RescheduleModalProps) {
  // Dropdown data
  const [doctors, setDoctors]       = useState<DoctorResponse[]>([])
  const [clinics, setClinics]       = useState<ClinicResponse[]>([])
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [loadingData, setLoadingData] = useState(true)

  // Form state — pre-filled with the current appointment values
  const [doctorId, setDoctorId]     = useState(String(appointment.doctorId))
  const [clinicId, setClinicId]     = useState(String(appointment.clinicId))
  const [categoryId, setCategoryId] = useState(String(appointment.categoryId))
  const [newSlot, setNewSlot]       = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError]           = useState<string | null>(null)

  // Load all dropdown options once when the modal mounts
  useEffect(() => {
    Promise.all([getAllDoctors(), getAllClinics(), getAllCategories()])
      .then(([d, c, cat]) => { setDoctors(d); setClinics(c); setCategories(cat) })
      .catch(() => setError('Could not load options. Please close and try again.'))
      .finally(() => setLoadingData(false))
  }, [])

  // When the doctor changes, clear the selected slot because availability differs per doctor
  function handleDoctorChange(id: string) {
    setDoctorId(id)
    setNewSlot('')
  }

  async function handleConfirm() {
    if (!newSlot) return
    setSubmitting(true)
    setError(null)
    try {
      const updated = await rescheduleAppointment(
        appointment.id,
        newSlot,
        addThirtyMinutes(newSlot),
        Number(doctorId) !== appointment.doctorId     ? Number(doctorId)     : undefined,
        Number(clinicId) !== appointment.clinicId     ? Number(clinicId)     : undefined,
        Number(categoryId) !== appointment.categoryId ? Number(categoryId)   : undefined
      )
      onSuccess(updated)
    } catch (err: unknown) {
      const status = (err as { response?: { status: number } }).response?.status
      if (status === 409 || status === 404) {
        setError(extractApiError(err, 'Something went wrong. Please try again.'))
      } else if (status === 400) {
        setError(extractApiError(err, 'Please check all fields and try again.'))
      } else {
        setError('Something went wrong. Please try again.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  // Close when clicking the dark backdrop (but not the modal card itself)
  function handleBackdropClick(e: React.MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget) onClose()
  }

  return (
    // Fixed overlay covering the full viewport — clicking outside the card closes the modal
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4"
      onClick={handleBackdropClick}
    >
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">

        {/* Modal header */}
        <div className="flex items-center justify-between px-6 py-5 border-b border-gray-100">
          <div>
            <h2 className="text-xl font-bold text-primary">Reschedule Appointment</h2>
            <p className="text-sm text-gray-500 mt-0.5">
              Currently with Dr. {appointment.doctorFullName} at {appointment.clinicName}
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-dark transition-colors"
            aria-label="Close"
          >
            {/* X icon */}
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Modal body */}
        <div className="px-6 py-5 space-y-5">
          {loadingData ? (
            <div className="flex justify-center py-8">
              <div className="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
            </div>
          ) : (
            <>
              {/* Doctor */}
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Doctor</label>
                <select
                  value={doctorId}
                  onChange={e => handleDoctorChange(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white"
                >
                  {doctors.map(d => (
                    <option key={d.id} value={d.id}>
                      {d.firstName} {d.lastName} — {d.specialityName}
                    </option>
                  ))}
                </select>
              </div>

              {/* Clinic */}
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Clinic</label>
                <select
                  value={clinicId}
                  onChange={e => setClinicId(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white"
                >
                  {clinics.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>

              {/* Category */}
              <div>
                <label className="block text-sm font-medium text-dark mb-1">Appointment Type</label>
                <select
                  value={categoryId}
                  onChange={e => setCategoryId(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent bg-white"
                >
                  {categories.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>

              {/* Slot picker */}
              <SlotPicker
                doctorId={doctorId}
                selectedStartTime={newSlot}
                onSlotSelect={setNewSlot}
              />

              {error && (
                <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-2">
                  {error}
                </p>
              )}
            </>
          )}
        </div>

        {/* Modal footer */}
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="text-sm text-gray-500 hover:text-dark px-4 py-2 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={!newSlot || submitting || loadingData}
            className="bg-accent text-white px-6 py-2 rounded-lg text-sm font-medium hover:bg-primary transition-colors disabled:opacity-50"
          >
            {submitting ? 'Saving...' : 'Confirm Reschedule'}
          </button>
        </div>
      </div>
    </div>
  )
}
