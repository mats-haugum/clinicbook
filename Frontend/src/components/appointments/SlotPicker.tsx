import { useEffect, useState } from 'react'
import { getDoctorAvailability, type DoctorAvailabilitySlot } from '../../api/doctors'

interface SlotPickerProps {
  // The currently selected doctor ID from the parent form (empty string = none selected)
  doctorId: string
  // The currently selected start time in 'YYYY-MM-DDTHH:MM' format — used to highlight the active slot
  selectedStartTime: string
  // Called with the slot's start time in 'YYYY-MM-DDTHH:MM' format when the user clicks a slot
  onSlotSelect: (startTime: string) => void
}

// Returns today's date as a YYYY-MM-DD string (used as the default date and as the min value)
function todayIso(): string {
  return new Date().toISOString().slice(0, 10)
}

// Extracts 'HH:MM' from an ISO datetime string like '2026-05-22T08:00:00'
function formatTime(iso: string): string {
  return iso.slice(11, 16)
}

// Extracts 'YYYY-MM-DDTHH:MM' from an ISO datetime string — the format used in the form state
function toFormValue(iso: string): string {
  return iso.slice(0, 16)
}

export default function SlotPicker({ doctorId, selectedStartTime, onSlotSelect }: SlotPickerProps) {
  const [date, setDate] = useState(todayIso)
  const [slots, setSlots] = useState<DoctorAvailabilitySlot[]>([])
  const [loading, setLoading] = useState(false)

  // Whenever the selected doctor or the chosen date changes, fetch availability from the API.
  // The dependency array [doctorId, date] means this effect re-runs only when either changes.
  useEffect(() => {
    if (!doctorId) {
      setSlots([])
      return
    }

    setLoading(true)
    getDoctorAvailability(Number(doctorId), date)
      .then(setSlots)
      .catch(() => setSlots([]))
      .finally(() => setLoading(false))
  }, [doctorId, date])

  return (
    <div className="space-y-3">
      {/* Date selector — only future dates (min = today) */}
      <div>
        <label className="block text-sm font-medium text-dark mb-1">Appointment Date</label>
        <input
          type="date"
          value={date}
          min={todayIso()}
          onChange={e => setDate(e.target.value)}
          className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent"
        />
      </div>

      {/* Prompt shown before a doctor is chosen */}
      {!doctorId && (
        <p className="text-sm text-gray-400 italic">Select a doctor above to see available times.</p>
      )}

      {/* Spinner while slots are loading */}
      {doctorId && loading && (
        <div className="flex justify-center py-4">
          <div className="w-5 h-5 border-2 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {/* Slot grid */}
      {doctorId && !loading && slots.length > 0 && (
        <div>
          <p className="text-sm font-medium text-dark mb-2">Available Times</p>

          {/* 4-column grid — 17 slots per day fits neatly in 5 rows */}
          <div className="grid grid-cols-4 gap-2">
            {slots.map(slot => {
              const isSelected = toFormValue(slot.startTime) === selectedStartTime

              return (
                <button
                  key={slot.startTime}
                  type="button"
                  disabled={!slot.isAvailable}
                  onClick={() => onSlotSelect(toFormValue(slot.startTime))}
                  className={[
                    'rounded-lg py-2 text-sm font-medium transition-colors',
                    // Three visual states: selected (dark), available (mint/accent), booked (gray)
                    slot.isAvailable
                      ? isSelected
                        ? 'bg-primary text-white ring-2 ring-primary ring-offset-1'
                        : 'bg-mint text-accent hover:bg-accent hover:text-white'
                      : 'bg-gray-100 text-gray-400 cursor-not-allowed line-through',
                  ].join(' ')}
                >
                  {formatTime(slot.startTime)}
                </button>
              )
            })}
          </div>

          {/* Legend */}
          <div className="flex gap-5 mt-3 text-xs text-gray-500">
            <span className="flex items-center gap-1.5">
              <span className="w-3 h-3 rounded-sm bg-mint border border-accent/30 inline-block" />
              Available
            </span>
            <span className="flex items-center gap-1.5">
              <span className="w-3 h-3 rounded-sm bg-primary inline-block" />
              Selected
            </span>
            <span className="flex items-center gap-1.5">
              <span className="w-3 h-3 rounded-sm bg-gray-200 inline-block" />
              Booked
            </span>
          </div>
        </div>
      )}

      {/* No slots (should not normally happen — doctor works 08:00-17:00 every day) */}
      {doctorId && !loading && slots.length === 0 && (
        <p className="text-sm text-gray-500">No time slots available for this date.</p>
      )}
    </div>
  )
}
