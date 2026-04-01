import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';

export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <div className="flex-1 flex flex-col items-center justify-center gap-4 px-4">
      <h1 className="text-6xl font-heading font-bold text-gold-500">404</h1>
      <p className="text-xl text-parchment-400">{t('notFound.message')}</p>
      <Link to="/" className="text-gold-500 hover:text-gold-300 underline transition-colors">
        {t('notFound.goHome')}
      </Link>
    </div>
  );
}
