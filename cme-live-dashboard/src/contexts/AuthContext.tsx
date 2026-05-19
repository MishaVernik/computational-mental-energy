import React, { createContext, useContext, useState, useCallback, type ReactNode } from 'react';

interface AuthState {
  isAuthenticated: boolean;
  email: string | null;
  login: (email: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState>({
  isAuthenticated: false,
  email: null,
  login: () => {},
  logout: () => {},
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('cmeflow_email'));

  const login = useCallback((e: string) => {
    localStorage.setItem('cmeflow_email', e);
    setEmail(e);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('cmeflow_email');
    setEmail(null);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated: !!email, email, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
