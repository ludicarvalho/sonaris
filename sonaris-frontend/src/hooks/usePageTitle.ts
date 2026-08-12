import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const routeTitles: Record<string, string> = {
  '/musicas': 'Músicas',
};

const APP_NAME = 'Sonaris';

export function usePageTitle(subtitle?: string) {
  const { pathname } = useLocation();

  useEffect(() => {
    const pageTitle = subtitle ?? routeTitles[pathname];
    document.title = pageTitle ? `${APP_NAME} | ${pageTitle}` : APP_NAME;
  }, [pathname, subtitle]);
}
