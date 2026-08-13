import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider } from './contexts/ThemeContext';
import { Musicas } from './pages/Musicas/Musicas';

function App() {
  return (
    <ThemeProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/musicas" element={<Musicas />} />
          <Route path="/" element={<Navigate to="/musicas" replace />} />
          <Route path="*" element={<Navigate to="/musicas" replace />} />
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
