import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { toast } from 'sonner';
import {
  api,
  clearAuth,
  getEmail,
  isTokenValid,
  setAuth,
  setUnauthorizedHandler,
} from '@/api/client';

interface AuthContextValue {
  email: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readInitialSession() {
  if (!isTokenValid()) {
    clearAuth();
    return { email: null as string | null, isAuthenticated: false };
  }
  return { email: getEmail(), isAuthenticated: true };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState(readInitialSession);

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setSession({ email: null, isAuthenticated: false });
      toast.error('Session expired. Please sign in again.');
    });
    return () => setUnauthorizedHandler(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      email: session.email,
      isAuthenticated: session.isAuthenticated,
      async login(loginEmail, password) {
        const auth = await api.login(loginEmail, password);
        setAuth(auth.token, auth.email, auth.expiresAt);
        setSession({ email: auth.email, isAuthenticated: true });
      },
      async register(registerEmail, password) {
        const auth = await api.register(registerEmail, password);
        setAuth(auth.token, auth.email, auth.expiresAt);
        setSession({ email: auth.email, isAuthenticated: true });
      },
      async logout() {
        try {
          await api.logout();
        } finally {
          clearAuth();
          setSession({ email: null, isAuthenticated: false });
        }
      },
    }),
    [session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
