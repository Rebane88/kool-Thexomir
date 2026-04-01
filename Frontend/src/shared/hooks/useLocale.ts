import { useTranslation } from 'react-i18next';

export function useLocale() {
  const { t, i18n } = useTranslation();
  return {
    t,
    language: i18n.language,
    changeLanguage: (lng: string) => i18n.changeLanguage(lng),
  };
}
