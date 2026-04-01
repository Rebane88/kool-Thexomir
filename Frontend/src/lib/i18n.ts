import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from '../locales/en.json';
import et from '../locales/et.json';

i18next
  .use(initReactI18next)
  .init({
    lng: localStorage.getItem('lang') || 'et',
    fallbackLng: 'en',
    resources: {
      en: { translation: en },
      et: { translation: et },
    },
    interpolation: {
      escapeValue: false,
    },
  });

i18next.on('languageChanged', (lng) => {
  localStorage.setItem('lang', lng);
});

export default i18next;
