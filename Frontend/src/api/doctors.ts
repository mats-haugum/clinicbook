import client from './client'

// Mirrors DoctorResponse.cs
export interface DoctorResponse {
  id: number
  firstName: string
  lastName: string
  specialityName: string
  clinicNames: string[]
}

export interface CreateDoctorPayload {
  firstName: string
  lastName: string
  specialityId: number
  clinicIds: number[]
}

export interface UpdateDoctorPayload {
  firstName: string
  lastName: string
  specialityId: number
}

export async function createDoctor(payload: CreateDoctorPayload): Promise<DoctorResponse> {
  const response = await client.post<DoctorResponse>('/doctors', payload)
  return response.data
}

export async function updateDoctor(id: number, payload: UpdateDoctorPayload): Promise<DoctorResponse> {
  const response = await client.put<DoctorResponse>(`/doctors/${id}`, payload)
  return response.data
}

export async function deleteDoctor(id: number): Promise<void> {
  await client.delete(`/doctors/${id}`)
}

// Calls GET /doctors — returns all doctors for populating dropdowns
export async function getAllDoctors(): Promise<DoctorResponse[]> {
  const response = await client.get<DoctorResponse[]>('/doctors')
  return response.data
}

// Mirrors DoctorSearchResponse.cs — property names are camelCase because
// .NET's JSON serialiser lowercases the first letter by default
export interface DoctorSearchResult {
  fullName: string
  clinicName: string
  speciality: string
}

// Calls GET /doctors/search?name=<name>
// Throws an AxiosError on 400 (empty name) or 404 (no results)
export async function searchDoctors(name: string): Promise<DoctorSearchResult[]> {
  const response = await client.get<DoctorSearchResult[]>('/doctors/search', {
    params: { name },
  })
  return response.data
}

// Mirrors DoctorAvailabilitySlot.cs
export interface DoctorAvailabilitySlot {
  startTime: string   // ISO datetime string, no timezone — treat as wall-clock local time
  endTime: string
  isAvailable: boolean
}

// Calls GET /doctors/{id}/availability?date=YYYY-MM-DD
export async function getDoctorAvailability(doctorId: number, date: string): Promise<DoctorAvailabilitySlot[]> {
  const response = await client.get<DoctorAvailabilitySlot[]>(`/doctors/${doctorId}/availability`, {
    params: { date },
  })
  return response.data
}
