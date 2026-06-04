import client from './client'

export interface SpecialityResponse {
  id: number
  name: string
}

export async function getAllSpecialities(): Promise<SpecialityResponse[]> {
  const response = await client.get<SpecialityResponse[]>('/specialities')
  return response.data
}

export async function createSpeciality(name: string): Promise<SpecialityResponse> {
  const response = await client.post<SpecialityResponse>('/specialities', { name })
  return response.data
}

export async function updateSpeciality(id: number, name: string): Promise<SpecialityResponse> {
  const response = await client.put<SpecialityResponse>(`/specialities/${id}`, { name })
  return response.data
}

export async function deleteSpeciality(id: number): Promise<void> {
  await client.delete(`/specialities/${id}`)
}
