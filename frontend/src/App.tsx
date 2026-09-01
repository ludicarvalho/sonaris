import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { ThemeProvider } from './contexts/ThemeContext';
import { AuthProvider } from './contexts/AuthContext';
import { useAuth } from './contexts/useAuth';
import { Musicas } from './pages/Musicas/Musicas';
import Login from './pages/Login/Login';

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, autenticando } = useAuth();
  const location = useLocation();

  if (autenticando) {
    return null;
  }

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route
              path="/musicas"
              element={
                <RequireAuth>
                  <Musicas />
                </RequireAuth>
              }
            />
            <Route path="/" element={<Navigate to="/musicas" replace />} />
            <Route path="*" element={<Navigate to="/musicas" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
