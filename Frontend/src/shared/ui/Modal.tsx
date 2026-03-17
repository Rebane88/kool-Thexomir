import type { ReactNode } from 'react';
import { Panel } from './Panel';

interface ModalProps {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
}

export function Modal({ open, onClose, children }: ModalProps) {
  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60"
      onClick={onClose}
    >
      <Panel
        className="relative max-w-md w-full mx-4 p-6"
        onClick={(e) => e.stopPropagation()}
      >
        {children}
      </Panel>
    </div>
  );
}
