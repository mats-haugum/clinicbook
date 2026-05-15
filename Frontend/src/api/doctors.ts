import client from './client'

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
