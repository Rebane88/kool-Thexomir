interface ResetCameraButtonProps {
  onReset: () => void;
}

export function ResetCameraButton({ onReset }: ResetCameraButtonProps) {
  return (
    <button
      type="button"
      onClick={onReset}
      title="Reset camera (Home)"
      className="absolute bottom-4 right-4 w-9 h-9 flex items-center justify-center rounded-md bg-black/40 hover:bg-black/60 text-parchment/70 hover:text-parchment transition-colors border border-parchment/20"
    >
      <svg
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
      >
        <line x1="8" y1="2" x2="8" y2="14" />
        <line x1="2" y1="8" x2="14" y2="8" />
        <circle cx="8" cy="8" r="2" />
      </svg>
    </button>
  );
}
