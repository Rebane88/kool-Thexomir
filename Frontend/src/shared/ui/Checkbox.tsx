import { forwardRef, type InputHTMLAttributes } from 'react';

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ label, className = '', id, ...props }, ref) => {
    const isChecked = props.checked ?? false;
    return (
      <label htmlFor={id} className={`inline-flex items-center gap-2 cursor-pointer group ${className}`}>
        <div className="relative">
          <input
            ref={ref}
            type="checkbox"
            id={id}
            className="peer sr-only"
            {...props}
          />
          <div className={`w-5 h-5 border-2 border-bronze-700 bg-ash-700 transition-all flex items-center justify-center peer-focus-visible:shadow-ember ${isChecked ? 'bg-gold-500 border-gold-500' : ''}`}>
            {isChecked && (
              <svg
                className="w-3 h-3 text-ash-950"
                viewBox="0 0 12 12"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="2,6 5,9 10,3" />
              </svg>
            )}
          </div>
        </div>
        {label && <span className="text-parchment-200 text-sm select-none">{label}</span>}
      </label>
    );
  }
);

Checkbox.displayName = 'Checkbox';
