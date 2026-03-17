import { forwardRef, type SelectHTMLAttributes } from 'react';

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, className = '', id, children, ...props }, ref) => {
    return (
      <div>
        {label && (
          <label htmlFor={id} className="text-parchment-300 text-sm font-medium mb-1 block">
            {label}
          </label>
        )}
        <select
          ref={ref}
          id={id}
          className={`w-full bg-ash-700 border px-3 py-2 text-parchment-200 focus:outline-none focus:border-gold-500 transition-colors ${error ? 'border-blood-600' : 'border-bronze-700'} ${className}`}
          {...props}
        >
          {children}
        </select>
        {error && <p className="text-blood-500 text-xs mt-1">{error}</p>}
      </div>
    );
  }
);

Select.displayName = 'Select';
