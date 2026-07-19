import { useMemo, useState, type PropsWithChildren } from 'react'
import { login as requestLogin, type Session } from '../api/client'
import { AuthContext, type AuthValue } from './auth-state'

const STORAGE_KEY = 'transitops.session'
function storedSession(): Session | null {
  try {
    const session = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null') as Session | null
    if (!session || new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return session
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<Session | null>(storedSession)
  const value = useMemo<AuthValue>(() => ({
    session,
    login: async (username, password) => {
      const next = await requestLogin(username, password)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
      setSession(next)
    },
    logout: () => {
      localStorage.removeItem(STORAGE_KEY)
      setSession(null)
    },
  }), [session])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
