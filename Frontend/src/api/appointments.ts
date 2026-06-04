import client from './client'

// Mirrors AppointmentResponse.cs
export interface AppointmentResponse {
  id: number
  doctorId: number
  clinicId: number
  categoryId: number
  startTime: string
  endTime: string
  doctorFullName: string
  clinicName: string
  categoryName: string
}

// GET /appointments/my — returns the logged-in patient's appointments
export async function getMyAppointments(): Promise<AppointmentResponse[]> {
  const response = await client.get<AppointmentResponse[]>('/appointments/my')
  return response.data
}

// DELETE /appointments/{id}/cancel — cancels a specific appointment
export async function cancelAppointment(id: number): Promise<void> {
  await client.delete(`/appointments/${id}/cancel`)
}

// PUT /appointments/{id}/reschedule — moves an appointment to a new slot,
// optionally changing the doctor, clinic, or category at the same time.
export async function rescheduleAppointment(
  id: number,
  startTime: string,
  endTime: string,
  doctorId?: number,
  clinicId?: number,
  categoryId?: number
): Promise<AppointmentResponse> {
  const response = await client.put<AppointmentResponse>(`/appointments/${id}/reschedule`, {
    startTime, endTime, doctorId, clinicId, categoryId,
  })
  return response.data
}

// POST /appointments/book — books as a registered patient (token required)
export async function bookAsPatient(
  doctorId: number,
  clinicId: number,
  categoryId: number,
  startTime: string,
  endTime: string
): Promise<AppointmentResponse> {
  const response = await client.post<AppointmentResponse>('/appointments/book', {
    doctorId, clinicId, categoryId, startTime, endTime,
  })
  return response.data
}

// POST /appointments/book/guest — books as a guest (no token required)
export async function bookAsGuest(
  firstName: string,
  lastName: string,
  email: string,
  birthdate: string,
  gender: string,
  doctorId: number,
  clinicId: number,
  categoryId: number,
  startTime: string,
  endTime: string
): Promise<AppointmentResponse> {
  const response = await client.post<AppointmentResponse>('/appointments/book/guest', {
    firstName, lastName, email, birthdate, gender,
    doctorId, clinicId, categoryId, startTime, endTime,
  })
  return response.data
}
