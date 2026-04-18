import { createClient } from '@hey-api/client-fetch'

export const apiClient = createClient({
  baseUrl: import.meta.env.VITE_API_BASE_URL || '',
})
