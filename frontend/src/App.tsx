import { Navigate, Route, Routes } from 'react-router-dom';
import { InvitationPage } from './pages/InvitationPage';
import { GiftsPage } from './pages/GiftsPage';
import { AdminLoginPage } from './pages/AdminLoginPage';
import { AdminPage } from './pages/AdminPage';

export function App() {
  return (
    <Routes>
      <Route path="/" element={<InvitationPage />} />
      <Route path="/gifts" element={<GiftsPage />} />
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
