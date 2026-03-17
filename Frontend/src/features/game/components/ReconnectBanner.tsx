export function ReconnectBanner() {
  return (
    <div
      className="absolute inset-x-0 top-0 z-10 border-b border-ember-500/30 bg-ember-500/20 px-4 py-2 text-center animate-pulse"
      role="alert"
      aria-live="assertive"
    >
      <span className="text-sm text-ember-400">
        Connection lost — reconnecting...
      </span>
    </div>
  );
}
