import { Link } from 'react-router';

export function NotFoundPage() {
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex flex-col items-center justify-center gap-4">
      <h1 className="text-6xl font-bold text-amber-500">404</h1>
      <p className="text-xl text-gray-400">Page not found</p>
      <Link to="/" className="text-amber-500 hover:text-amber-400 underline">
        Return home
      </Link>
    </div>
  );
}
