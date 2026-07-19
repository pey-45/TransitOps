import type { ValidationDetails } from '../api/client'

export function fieldErrors(details?: ValidationDetails, name?: string) {
  if (!details || !name) return undefined
  const target = Object.keys(details).find(key => key.toLowerCase() === name.toLowerCase())
  return target ? details[target] : undefined
}
