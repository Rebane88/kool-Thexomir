import { BrowserRouter, Routes, Route } from 'react-router';
import { ProtectedRoute } from '@/shared/components/ProtectedRoute';
import { NavBar } from '@/shared/components/NavBar';
import { NotFoundPage } from '@/shared/components/NotFoundPage';

// Placeholder pages — real content comes in Phases 8 and 9
function LoginPage() {
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex items-center justify-center">
      <h1 className="text-2xl">Login Page</h1>
    </div>
  );
}

function RegisterPage() {
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex items-center justify-center">
      <h1 className="text-2xl">Register Page</h1>
    </div>
  );
}

function HomePage() {
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex items-center justify-center">
      <h1 className="text-2xl">Home (Protected)</h1>
    </div>
  );
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <NavBar />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<HomePage />} />
        </Route>
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
