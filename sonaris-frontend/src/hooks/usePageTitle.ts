import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const routeTitles: Record<string, string> = {
  '/musicas': 'Músicas',
};

const APP_NAME = 'Sonaris';

export function usePageTitle() {
  const { pathname } = useLocation();

  useEffect(() => {
    const pageTitle = routeTitles[pathname];
    document.title = pageTitle ? `${pageTitle} | ${APP_NAME}` : APP_NAME;
  }, [pathname]);
}
