import client from './client'

// Mirrors AppointmentCategoryResponse.cs
export interface CategoryResponse {
  id: number
  name: string
}

export async function createCategory(name: string): Promise<CategoryResponse> {
  const response = await client.post<CategoryResponse>('/appointment-categories', { name })
  return response.data
}

export async function updateCategory(id: number, name: string): Promise<CategoryResponse> {
  const response = await client.put<CategoryResponse>(`/appointment-categories/${id}`, { name })
  return response.data
}

export async function deleteCategory(id: number): Promise<void> {
  await client.delete(`/appointment-categories/${id}`)
}

// Calls GET /appointment-categories — returns all categories for populating dropdowns
export async function getAllCategories(): Promise<CategoryResponse[]> {
  const response = await client.get<CategoryResponse[]>('/appointment-categories')
  return response.data
}
