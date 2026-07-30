export function dayStart(value: string) {
  return value ? new Date(`${value}T00:00`).toISOString() : undefined
}

export function dayEnd(value: string) {
  if (!value) return undefined
  const date = new Date(`${value}T00:00`)
  date.setHours(23, 59, 59, 999)
  return date.toISOString()
}
