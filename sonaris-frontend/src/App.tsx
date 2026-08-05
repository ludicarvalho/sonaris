import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Musicas } from './pages/Musicas/Musicas';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/musicas" element={<Musicas />} />
        <Route path="/" element={<Navigate to="/musicas" replace />} />
        <Route path="*" element={<Navigate to="/musicas" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
