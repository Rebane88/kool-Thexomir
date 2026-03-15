import { BrowserRouter, Routes, Route, Outlet } from 'react-router';
import { ProtectedRoute } from '@/shared/components/ProtectedRoute';
import { NavBar } from '@/shared/components/NavBar';
import { NotFoundPage } from '@/shared/components/NotFoundPage';
import { LoginPage, RegisterPage } from '@/features/auth';
import { LobbyHomePage, LobbyDetailPage, GamePage } from '@/features/lobby';

function LayoutWithNavBar() {
  return (
    <>
      <NavBar />
      <Outlet />
    </>
  );
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Auth pages — no NavBar */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* All other pages — with NavBar */}
        <Route element={<LayoutWithNavBar />}>
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<LobbyHomePage />} />
            <Route path="/lobby/:id" element={<LobbyDetailPage />} />
            <Route path="/game/:id" element={<GamePage />} />
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
