import { useEffect, useMemo, useState, type PropsWithChildren } from 'react'
import {
  getCurrentSession,
  login as requestLogin,
  logout as requestLogout,
  type Session,
} from '../api/client'
import { AuthContext, type AuthValue } from './auth-state'

interface AuthProviderProps extends PropsWithChildren {
  initialSession?: Session | null
}

export function AuthProvider({ children, initialSession }: AuthProviderProps) {
  const rehydrate = initialSession === undefined
  const [session, setSession] = useState<Session | null>(initialSession ?? null)
  const [loading, setLoading] = useState(rehydrate)

  useEffect(() => {
    if (!rehydrate) return
    let ignore = false
    getCurrentSession()
      .then(current => { if (!ignore) setSession(current) })
      .catch(() => { if (!ignore) setSession(null) })
      .finally(() => { if (!ignore) setLoading(false) })
    return () => { ignore = true }
  }, [rehydrate])

  const value = useMemo<AuthValue>(() => ({
    session,
    loading,
    login: async (username, password) => {
      const next = await requestLogin(username, password)
      setSession(next)
    },
    logout: async () => {
      try {
        await requestLogout()
      } finally {
        setSession(null)
      }
    },
  }), [loading, session])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
