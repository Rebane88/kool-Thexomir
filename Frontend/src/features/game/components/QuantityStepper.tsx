interface QuantityStepperProps {
  value: number;
  min: number;
  max: number;
  onChange: (value: number) => void;
}

export function QuantityStepper({ value, min, max, onChange }: QuantityStepperProps) {
  function handleInput(e: React.ChangeEvent<HTMLInputElement>) {
    const raw = parseInt(e.target.value, 10);
    if (isNaN(raw)) return;
    onChange(Math.max(min, Math.min(max, raw)));
  }

  return (
    <div className="flex items-center gap-1">
      <button
        type="button"
        disabled={value <= min}
        onClick={() => onChange(value - 1)}
        className="text-parchment-200 bg-ash-700 hover:bg-ash-600 px-2 py-0.5 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed"
      >
        -
      </button>
      <input
        type="number"
        min={min}
        max={max}
        value={value}
        onChange={handleInput}
        className="w-12 text-center bg-ash-700 text-parchment-200 rounded text-sm py-0.5 [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
      />
      <button
        type="button"
        disabled={value >= max}
        onClick={() => onChange(value + 1)}
        className="text-parchment-200 bg-ash-700 hover:bg-ash-600 px-2 py-0.5 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed"
      >
        +
      </button>
      <button
        type="button"
        onClick={() => onChange(max)}
        className="text-xs text-gold-400 hover:text-gold-300 ml-1"
      >
        Max
      </button>
    </div>
  );
}
