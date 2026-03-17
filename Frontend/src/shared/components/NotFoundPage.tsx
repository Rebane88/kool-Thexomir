import { Link } from 'react-router';

export function NotFoundPage() {
  return (
    <div className="flex-1 flex flex-col items-center justify-center gap-4 px-4">
      <h1 className="text-6xl font-heading font-bold text-gold-500">404</h1>
      <p className="text-xl text-parchment-400">Page not found</p>
      <Link to="/" className="text-gold-500 hover:text-gold-300 underline transition-colors">
        Return home
      </Link>
    </div>
  );
}
