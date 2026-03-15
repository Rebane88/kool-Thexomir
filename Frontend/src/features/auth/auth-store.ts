import { create } from 'zustand';
import type { User } from '@/shared/types/api';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  isInitializing: boolean;
  setAuth: (user: User, accessToken: string) => void;
  clearAuth: () => void;
  setInitialized: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  isInitializing: true,
  setAuth: (user, accessToken) => set({ user, accessToken }),
  clearAuth: () => set({ user: null, accessToken: null }),
  setInitialized: () => set({ isInitializing: false }),
}));
