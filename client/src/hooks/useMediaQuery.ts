import { useEffect, useState } from 'react';

const MOBILE_QUERY = '(max-width: 767px)';

/** Mobile detection — works in DevTools device mode via visualViewport + matchMedia. */
export function useIsMobile() {
  const [isMobile, setIsMobile] = useState(() => readIsMobile());

  useEffect(() => {
    function sync() {
      setIsMobile(readIsMobile());
    }

    sync();
    const mq = window.matchMedia(MOBILE_QUERY);
    mq.addEventListener('change', sync);
    window.addEventListener('resize', sync);
    window.visualViewport?.addEventListener('resize', sync);
    const poll = window.setInterval(sync, 200);

    return () => {
      mq.removeEventListener('change', sync);
      window.removeEventListener('resize', sync);
      window.visualViewport?.removeEventListener('resize', sync);
      window.clearInterval(poll);
    };
  }, []);

  return isMobile;
}

function readIsMobile() {
  if (typeof window === 'undefined') return false;
  const vvW = window.visualViewport?.width;
  const w = vvW == null ? window.innerWidth : Math.round(vvW);
  return w <= 767 || window.matchMedia(MOBILE_QUERY).matches;
}

export function useMediaQuery(query: string) {
  const [matches, setMatches] = useState(() => {
    if (typeof window === 'undefined') return false;
    return window.matchMedia(query).matches;
  });

  useEffect(() => {
    const media = window.matchMedia(query);
    const sync = () => setMatches(media.matches);

    sync();
    media.addEventListener('change', sync);
    window.addEventListener('resize', sync);
    window.visualViewport?.addEventListener('resize', sync);

    return () => {
      media.removeEventListener('change', sync);
      window.removeEventListener('resize', sync);
      window.visualViewport?.removeEventListener('resize', sync);
    };
  }, [query]);

  return matches;
}
