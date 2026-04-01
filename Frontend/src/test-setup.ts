import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';
import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';

// Initialize i18next with English for tests so t() returns readable strings
i18next.use(initReactI18next).init({
  lng: 'en',
  fallbackLng: 'en',
  resources: {
    en: { translation: en },
  },
  interpolation: { escapeValue: false },
});

afterEach(() => {
  cleanup();
});
