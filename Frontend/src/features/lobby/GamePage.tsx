import { useParams, Link } from 'react-router';

export function GamePage() {
  const { id } = useParams();
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-2xl font-bold text-amber-500 mb-2">
          Game in Progress
        </h1>
        <p className="text-gray-400 mb-4">Game ID: {id}</p>
        <Link to="/" className="text-amber-500 hover:text-amber-400">
          Back to Home
        </Link>
      </div>
    </div>
  );
}
