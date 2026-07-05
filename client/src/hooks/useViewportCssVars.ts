import { useEffect } from 'react';

/** Keeps --vp-w / --vp-h in sync — required for DevTools device mode (visualViewport ≠ window). */
export function useViewportCssVars() {
  useEffect(() => {
    const root = document.documentElement;

    function sync() {
      const vv = window.visualViewport;
      const w = Math.round(vv?.width ?? window.innerWidth);
      const h = Math.round(vv?.height ?? window.innerHeight);
      root.style.setProperty('--vp-w', `${w}px`);
      root.style.setProperty('--vp-h', `${h}px`);
    }

    sync();
    window.addEventListener('resize', sync);
    window.visualViewport?.addEventListener('resize', sync);
    window.visualViewport?.addEventListener('scroll', sync);
    window.matchMedia('(max-width: 767px)').addEventListener('change', sync);
    window.matchMedia('(min-width: 768px)').addEventListener('change', sync);

    // DevTools device switch sometimes skips events — short poll as fallback
    const poll = window.setInterval(sync, 200);

    return () => {
      window.removeEventListener('resize', sync);
      window.visualViewport?.removeEventListener('resize', sync);
      window.visualViewport?.removeEventListener('scroll', sync);
      window.clearInterval(poll);
    };
  }, []);
}
