import { useTranslation } from 'react-i18next';

export function LanguageSwitcher() {
  const { i18n } = useTranslation();
  const currentLang = i18n.language.startsWith('et') ? 'et' : 'en';

  const toggleLanguage = () => {
    const next = currentLang === 'et' ? 'en' : 'et';
    i18n.changeLanguage(next);
  };

  return (
    <button
      type="button"
      onClick={toggleLanguage}
      className="px-2 py-0.5 text-xs font-heading font-bold border border-bronze-700 text-parchment-400 hover:text-gold-500 hover:border-bronze-500 transition-colors tracking-wider"
      title={currentLang === 'et' ? 'Switch to English' : 'Vaheta eesti keelele'}
    >
      {currentLang === 'et' ? 'ET' : 'EN'}
    </button>
  );
}
