import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { extractApiError } from '../../utils/apiError'

import {
  getAllDoctors, createDoctor, updateDoctor, deleteDoctor,
  type DoctorResponse, type CreateDoctorPayload,
} from '../../api/doctors'
import { getAllClinics, createClinic, updateClinic, deleteClinic, type ClinicResponse } from '../../api/clinics'
import { getAllSpecialities, createSpeciality, updateSpeciality, deleteSpeciality, type SpecialityResponse } from '../../api/specialities'
import { getAllCategories, createCategory, updateCategory, deleteCategory } from '../../api/categories'

type Tab = 'doctors' | 'clinics' | 'specialities' | 'categories'

// ─── shared small components ───────────────────────────────────────────────

function TabButton({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
        active ? 'bg-accent text-white' : 'text-gray-600 hover:bg-gray-100'
      }`}
    >
      {label}
    </button>
  )
}

function ActionButton({ label, variant, onClick, disabled }: {
  label: string; variant: 'edit' | 'delete' | 'confirm' | 'cancel'
  onClick: () => void; disabled?: boolean
}) {
  const styles = {
    edit:    'text-accent border border-accent hover:bg-mint',
    delete:  'text-red-500 border border-red-200 hover:bg-red-50',
    confirm: 'text-white bg-red-500 hover:bg-red-600',
    cancel:  'text-gray-500 border border-gray-200 hover:bg-gray-50',
  }
  return (
    <button onClick={onClick} disabled={disabled}
      className={`text-xs px-3 py-1 rounded-lg transition-colors disabled:opacity-40 ${styles[variant]}`}>
      {label}
    </button>
  )
}

function ErrorBanner({ message, onDismiss }: { message: string; onDismiss: () => void }) {
  return (
    <div className="flex items-center justify-between bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm text-red-600 mb-4">
      <span>{message}</span>
      <button onClick={onDismiss} className="ml-4 text-red-400 hover:text-red-600 font-bold">×</button>
    </div>
  )
}

const inputClass = 'border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-accent'

// ─── Doctors tab ───────────────────────────────────────────────────────────

function DoctorsTab({ specialities, clinics }: { specialities: SpecialityResponse[]; clinics: ClinicResponse[] }) {
  const [doctors, setDoctors]           = useState<DoctorResponse[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<number | null>(null)
  const [editingId, setEditingId]       = useState<number | null>(null)

  // New doctor form state
  const [newFirst, setNewFirst]         = useState('')
  const [newLast, setNewLast]           = useState('')
  const [newSpeciality, setNewSpeciality] = useState('')
  const [newClinics, setNewClinics]     = useState<number[]>([])
  const [saving, setSaving]             = useState(false)

  // Edit form state (mirrors the row being edited)
  const [editFirst, setEditFirst]       = useState('')
  const [editLast, setEditLast]         = useState('')
  const [editSpeciality, setEditSpeciality] = useState('')

  useEffect(() => {
    getAllDoctors().then(setDoctors).catch(() => setError('Could not load doctors.')).finally(() => setLoading(false))
  }, [])

  function startEdit(d: DoctorResponse) {
    setEditingId(d.id)
    setEditFirst(d.firstName)
    setEditLast(d.lastName)
    setEditSpeciality(String(specialities.find(s => s.name === d.specialityName)?.id ?? ''))
  }

  async function handleCreate() {
    if (!newFirst || !newLast || !newSpeciality || newClinics.length === 0) return
    setSaving(true); setError(null)
    try {
      const payload: CreateDoctorPayload = {
        firstName: newFirst, lastName: newLast,
        specialityId: Number(newSpeciality), clinicIds: newClinics,
      }
      const created = await createDoctor(payload)
      setDoctors(prev => [...prev, created])
      setNewFirst(''); setNewLast(''); setNewSpeciality(''); setNewClinics([])
    } catch (err) { setError(extractApiError(err, 'Could not create doctor.')) }
    finally { setSaving(false) }
  }

  async function handleUpdate(id: number) {
    if (!editFirst || !editLast || !editSpeciality) return
    setSaving(true); setError(null)
    try {
      const updated = await updateDoctor(id, { firstName: editFirst, lastName: editLast, specialityId: Number(editSpeciality) })
      setDoctors(prev => prev.map(d => d.id === id ? updated : d))
      setEditingId(null)
    } catch (err) { setError(extractApiError(err, 'Could not update doctor.')) }
    finally { setSaving(false) }
  }

  async function handleDelete(id: number) {
    setSaving(true); setError(null)
    try {
      await deleteDoctor(id)
      setDoctors(prev => prev.filter(d => d.id !== id))
      setConfirmDelete(null)
    } catch (err) { setError(extractApiError(err, 'Could not delete doctor.')); setConfirmDelete(null) }
    finally { setSaving(false) }
  }

  function toggleClinic(id: number) {
    setNewClinics(prev => prev.includes(id) ? prev.filter(c => c !== id) : [...prev, id])
  }

  if (loading) return <p className="text-gray-400 text-sm py-4">Loading...</p>

  return (
    <div className="space-y-4">
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* Create form */}
      <div className="border border-gray-200 rounded-xl p-4 bg-gray-50 space-y-3">
        <p className="text-sm font-semibold text-dark">Add Doctor</p>
        <div className="grid grid-cols-2 gap-2">
          <input placeholder="First name" value={newFirst} onChange={e => setNewFirst(e.target.value)} className={inputClass} />
          <input placeholder="Last name" value={newLast} onChange={e => setNewLast(e.target.value)} className={inputClass} />
        </div>
        <select value={newSpeciality} onChange={e => setNewSpeciality(e.target.value)} className={`${inputClass} w-full bg-white`}>
          <option value="">Select speciality</option>
          {specialities.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
        <div>
          <p className="text-xs text-gray-500 mb-1">Assign to clinics (select at least one)</p>
          <div className="flex flex-wrap gap-2">
            {clinics.map(c => (
              <label key={c.id} className="flex items-center gap-1 text-xs cursor-pointer">
                <input type="checkbox" checked={newClinics.includes(c.id)} onChange={() => toggleClinic(c.id)} />
                {c.name}
              </label>
            ))}
          </div>
        </div>
        <button onClick={handleCreate} disabled={saving || !newFirst || !newLast || !newSpeciality || newClinics.length === 0}
          className="bg-accent text-white text-sm px-4 py-1.5 rounded-lg hover:bg-primary transition-colors disabled:opacity-40">
          {saving ? 'Saving...' : 'Add Doctor'}
        </button>
      </div>

      {/* List */}
      <ul className="space-y-2">
        {doctors.map(d => (
          <li key={d.id} className="border border-gray-200 rounded-xl p-4 bg-white">
            {editingId === d.id ? (
              <div className="space-y-2">
                <div className="grid grid-cols-2 gap-2">
                  <input value={editFirst} onChange={e => setEditFirst(e.target.value)} className={inputClass} />
                  <input value={editLast} onChange={e => setEditLast(e.target.value)} className={inputClass} />
                </div>
                <select value={editSpeciality} onChange={e => setEditSpeciality(e.target.value)} className={`${inputClass} w-full bg-white`}>
                  <option value="">Select speciality</option>
                  {specialities.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
                <div className="flex gap-2">
                  <ActionButton label="Save" variant="confirm" onClick={() => handleUpdate(d.id)} disabled={saving} />
                  <ActionButton label="Cancel" variant="cancel" onClick={() => setEditingId(null)} />
                </div>
              </div>
            ) : (
              <div className="flex justify-between items-start">
                <div>
                  <p className="font-medium text-dark">{d.firstName} {d.lastName}</p>
                  <p className="text-xs text-gray-400 mt-0.5">{d.specialityName} · {d.clinicNames.join(', ')}</p>
                </div>
                <div className="flex gap-2 shrink-0">
                  {confirmDelete === d.id ? (
                    <>
                      <ActionButton label="Confirm" variant="confirm" onClick={() => handleDelete(d.id)} disabled={saving} />
                      <ActionButton label="Cancel" variant="cancel" onClick={() => setConfirmDelete(null)} />
                    </>
                  ) : (
                    <>
                      <ActionButton label="Edit" variant="edit" onClick={() => startEdit(d)} />
                      <ActionButton label="Delete" variant="delete" onClick={() => setConfirmDelete(d.id)} />
                    </>
                  )}
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>
    </div>
  )
}

// ─── Generic simple-entity tab (Clinics, Specialities, Categories) ─────────
// All three follow the same pattern: a name (+ optional address) field, list, edit, delete.

interface SimpleEntity { id: number; name: string; address?: string }

function SimpleEntityTab<T extends SimpleEntity>({
  label, hasAddress = false, load, create, update, remove,
}: {
  label: string
  hasAddress?: boolean
  load: () => Promise<T[]>
  create: (name: string, address?: string) => Promise<T>
  update: (id: number, name: string, address?: string) => Promise<T>
  remove: (id: number) => Promise<void>
}) {
  const [items, setItems]               = useState<T[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<number | null>(null)
  const [editingId, setEditingId]       = useState<number | null>(null)
  const [editName, setEditName]         = useState('')
  const [editAddress, setEditAddress]   = useState('')
  const [newName, setNewName]           = useState('')
  const [newAddress, setNewAddress]     = useState('')
  const [saving, setSaving]             = useState(false)

  useEffect(() => {
    load().then(setItems).catch(() => setError(`Could not load ${label.toLowerCase()}.`)).finally(() => setLoading(false))
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function startEdit(item: T) {
    setEditingId(item.id); setEditName(item.name); setEditAddress(item.address ?? '')
  }

  async function handleCreate() {
    if (!newName) return
    setSaving(true); setError(null)
    try {
      const created = await create(newName, hasAddress ? newAddress : undefined)
      setItems(prev => [...prev, created])
      setNewName(''); setNewAddress('')
    } catch (err) { setError(extractApiError(err, `Could not create ${label.toLowerCase()}.`)) }
    finally { setSaving(false) }
  }

  async function handleUpdate(id: number) {
    if (!editName) return
    setSaving(true); setError(null)
    try {
      const updated = await update(id, editName, hasAddress ? editAddress : undefined)
      setItems(prev => prev.map(i => i.id === id ? updated : i))
      setEditingId(null)
    } catch (err) { setError(extractApiError(err, `Could not update ${label.toLowerCase()}.`)) }
    finally { setSaving(false) }
  }

  async function handleDelete(id: number) {
    setSaving(true); setError(null)
    try {
      await remove(id)
      setItems(prev => prev.filter(i => i.id !== id))
      setConfirmDelete(null)
    } catch (err) { setError(extractApiError(err, `Could not delete ${label.toLowerCase()}.`)); setConfirmDelete(null) }
    finally { setSaving(false) }
  }

  if (loading) return <p className="text-gray-400 text-sm py-4">Loading...</p>

  return (
    <div className="space-y-4">
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* Create form */}
      <div className="border border-gray-200 rounded-xl p-4 bg-gray-50 space-y-2">
        <p className="text-sm font-semibold text-dark">Add {label.slice(0, -1)}</p>
        <input placeholder="Name" value={newName} onChange={e => setNewName(e.target.value)} className={`${inputClass} w-full`} />
        {hasAddress && (
          <input placeholder="Address" value={newAddress} onChange={e => setNewAddress(e.target.value)} className={`${inputClass} w-full`} />
        )}
        <button onClick={handleCreate} disabled={saving || !newName}
          className="bg-accent text-white text-sm px-4 py-1.5 rounded-lg hover:bg-primary transition-colors disabled:opacity-40">
          {saving ? 'Saving...' : `Add ${label.slice(0, -1)}`}
        </button>
      </div>

      {/* List */}
      <ul className="space-y-2">
        {items.map(item => (
          <li key={item.id} className="border border-gray-200 rounded-xl p-4 bg-white">
            {editingId === item.id ? (
              <div className="space-y-2">
                <input value={editName} onChange={e => setEditName(e.target.value)} className={`${inputClass} w-full`} />
                {hasAddress && (
                  <input value={editAddress} onChange={e => setEditAddress(e.target.value)} className={`${inputClass} w-full`} />
                )}
                <div className="flex gap-2">
                  <ActionButton label="Save" variant="confirm" onClick={() => handleUpdate(item.id)} disabled={saving} />
                  <ActionButton label="Cancel" variant="cancel" onClick={() => setEditingId(null)} />
                </div>
              </div>
            ) : (
              <div className="flex justify-between items-center">
                <div>
                  <p className="font-medium text-dark">{item.name}</p>
                  {item.address && <p className="text-xs text-gray-400 mt-0.5">{item.address}</p>}
                </div>
                <div className="flex gap-2 shrink-0">
                  {confirmDelete === item.id ? (
                    <>
                      <ActionButton label="Confirm" variant="confirm" onClick={() => handleDelete(item.id)} disabled={saving} />
                      <ActionButton label="Cancel" variant="cancel" onClick={() => setConfirmDelete(null)} />
                    </>
                  ) : (
                    <>
                      <ActionButton label="Edit" variant="edit" onClick={() => startEdit(item)} />
                      <ActionButton label="Delete" variant="delete" onClick={() => setConfirmDelete(item.id)} />
                    </>
                  )}
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>
    </div>
  )
}

// ─── Main dashboard ────────────────────────────────────────────────────────

export default function AdminDashboard() {
  const { isAdmin, user, logout } = useAuth()
  const navigate = useNavigate()
  const [tab, setTab] = useState<Tab>('doctors')

  // Shared data needed by the Doctors tab
  const [specialities, setSpecialities] = useState<SpecialityResponse[]>([])
  const [clinics, setClinics]           = useState<ClinicResponse[]>([])

  useEffect(() => {
    if (!isAdmin) { navigate('/admin/login', { replace: true }); return }
  }, [isAdmin, navigate])

  // Refetch specialities and clinics whenever the Doctors tab becomes active.
  // These are passed as props to DoctorsTab — without this, the speciality/clinic
  // dropdowns go stale after the admin adds or removes items in the other tabs.
  useEffect(() => {
    if (!isAdmin || tab !== 'doctors') return
    Promise.all([getAllSpecialities(), getAllClinics()]).then(([s, c]) => { setSpecialities(s); setClinics(c) })
  }, [isAdmin, tab])

  if (!isAdmin) return null

  const tabs: { key: Tab; label: string }[] = [
    { key: 'doctors',      label: 'Doctors' },
    { key: 'clinics',      label: 'Clinics' },
    { key: 'specialities', label: 'Specialities' },
    { key: 'categories',   label: 'Appointment Types' },
  ]

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top bar */}
      <header className="bg-primary text-white px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <svg width="28" height="28" viewBox="0 0 36 36" fill="none" aria-hidden="true">
            <rect width="36" height="36" rx="8" fill="white" fillOpacity="0.15" />
            <rect x="15" y="7" width="6" height="22" rx="2" fill="white" />
            <rect x="7" y="15" width="22" height="6" rx="2" fill="white" />
          </svg>
          <div>
            <p className="font-bold text-lg leading-tight">ClinicBook Admin</p>
            <Link to="/" className="text-white/50 text-xs hover:text-white/80 transition-colors">
              ← Back to patient site
            </Link>
          </div>
        </div>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-white/70">Welcome, {user?.firstName}</span>
          <button onClick={() => { logout(); navigate('/admin/login') }}
            className="bg-white/10 hover:bg-white/20 px-3 py-1.5 rounded-lg transition-colors">
            Sign Out
          </button>
        </div>
      </header>

      <main className="max-w-3xl mx-auto px-6 py-10">
        <h1 className="text-2xl font-bold text-primary mb-2">Dashboard</h1>
        <p className="text-gray-500 text-sm mb-6">Manage clinic data — changes take effect immediately.</p>

        {/* Tab bar */}
        <div className="flex gap-1 mb-6 bg-white border border-gray-200 rounded-xl p-1 w-fit">
          {tabs.map(t => (
            <TabButton key={t.key} label={t.label} active={tab === t.key} onClick={() => setTab(t.key)} />
          ))}
        </div>

        {/* Tab content */}
        {tab === 'doctors' && <DoctorsTab specialities={specialities} clinics={clinics} />}

        {tab === 'clinics' && (
          <SimpleEntityTab
            label="Clinics" hasAddress
            load={getAllClinics}
            create={(name, address) => createClinic(name, address ?? '')}
            update={(id, name, address) => updateClinic(id, name, address ?? '')}
            remove={deleteClinic}
          />
        )}

        {tab === 'specialities' && (
          <SimpleEntityTab
            label="Specialities"
            load={getAllSpecialities}
            create={name => createSpeciality(name)}
            update={(id, name) => updateSpeciality(id, name)}
            remove={deleteSpeciality}
          />
        )}

        {tab === 'categories' && (
          <SimpleEntityTab
            label="Appointment Types"
            load={getAllCategories}
            create={name => createCategory(name)}
            update={(id, name) => updateCategory(id, name)}
            remove={deleteCategory}
          />
        )}
      </main>
    </div>
  )
}
