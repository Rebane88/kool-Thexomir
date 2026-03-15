import { BrowserRouter, Routes, Route, Outlet } from 'react-router';
import { ProtectedRoute } from '@/shared/components/ProtectedRoute';
import { NavBar } from '@/shared/components/NavBar';
import { NotFoundPage } from '@/shared/components/NotFoundPage';
import { LoginPage, RegisterPage } from '@/features/auth';

function HomePage() {
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex items-center justify-center">
      <h1 className="text-2xl">Home (Protected)</h1>
    </div>
  );
}

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
            <Route path="/" element={<HomePage />} />
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
