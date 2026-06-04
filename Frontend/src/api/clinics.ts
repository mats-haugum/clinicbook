import client from './client'

// Mirrors ClinicResponse.cs
export interface ClinicResponse {
  id: number
  name: string
  address: string
}

export async function createClinic(name: string, address: string): Promise<ClinicResponse> {
  const response = await client.post<ClinicResponse>('/clinics', { name, address })
  return response.data
}

export async function updateClinic(id: number, name: string, address: string): Promise<ClinicResponse> {
  const response = await client.put<ClinicResponse>(`/clinics/${id}`, { name, address })
  return response.data
}

export async function deleteClinic(id: number): Promise<void> {
  await client.delete(`/clinics/${id}`)
}

// Calls GET /clinics — returns all clinics for populating dropdowns
export async function getAllClinics(): Promise<ClinicResponse[]> {
  const response = await client.get<ClinicResponse[]>('/clinics')
  return response.data
}
