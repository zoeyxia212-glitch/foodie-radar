import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { ApiError, api, tokenStore } from './api';
import type { AuthResponse, User } from './types';

interface AuthState {
  user: User | null;
  /** True while checking a saved token on page load. */
  checking: boolean;
  signIn: (res: AuthResponse) => void;
  signOut: () => void;
  /** Opens the login dialog (rendered in App). */
  requestLogin: () => void;
  loginOpen: boolean;
  closeLogin: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [checking, setChecking] = useState(true);
  const [loginOpen, setLoginOpen] = useState(false);

  useEffect(() => {
    if (!tokenStore.get()) {
      setChecking(false);
      return;
    }
    api
      .me()
      .then(setUser)
      .catch((e) => {
        // Expired or invalid token: forget it quietly.
        if (e instanceof ApiError && e.status === 401) tokenStore.set(null);
      })
      .finally(() => setChecking(false));
  }, []);

  const signIn = useCallback((res: AuthResponse) => {
    tokenStore.set(res.token);
    setUser(res.user);
    setLoginOpen(false);
  }, []);

  const signOut = useCallback(() => {
    tokenStore.set(null);
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        checking,
        signIn,
        signOut,
        requestLogin: () => setLoginOpen(true),
        loginOpen,
        closeLogin: () => setLoginOpen(false),
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
