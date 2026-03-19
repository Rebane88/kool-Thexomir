interface LoadingScreenProps {
  message?: string;
}

export function LoadingScreen({ message }: LoadingScreenProps) {
  return (
    <div
      className="flex flex-1 min-h-0 flex-col items-center justify-center bg-ash-950"
      role="status"
      aria-live="polite"
    >
      {/* Pulsing gold dot spinner */}
      <div className="mb-4 flex gap-1" aria-label="Loading">
        <span className="h-2 w-2 rounded-full bg-gold-500 animate-pulse" style={{ animationDelay: '0ms' }} />
        <span className="h-2 w-2 rounded-full bg-gold-500 animate-pulse" style={{ animationDelay: '300ms' }} />
        <span className="h-2 w-2 rounded-full bg-gold-500 animate-pulse" style={{ animationDelay: '600ms' }} />
      </div>
      <h2 className="font-heading text-xl font-bold text-gold-500">
        {message ?? 'Entering the realm...'}
      </h2>
      <p className="mt-2 text-sm text-parchment-400">
        {message ? 'Loading game assets' : 'Connecting to game server'}
      </p>
    </div>
  );
}
