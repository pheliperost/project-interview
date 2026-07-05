import { useEffect, useId, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { lockPageScroll, unlockPageScroll } from '@/lib/scrollLock';
import { getOverlayRoot } from '@/lib/overlayRoot';

interface AppDrawerProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}

export function AppDrawer({ open, onClose, title, children }: AppDrawerProps) {
  const titleId = useId();

  useEffect(() => {
    if (!open) return;
    lockPageScroll();
    return () => unlockPageScroll();
  }, [open]);

  useEffect(() => {
    if (!open) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  return createPortal(
    <div className="app-drawer-shell">
      <button
        type="button"
        className="absolute inset-0 bg-black/60"
        aria-label="Close drawer"
        onClick={onClose}
      />
      <dialog
        aria-labelledby={titleId}
        className="app-drawer-panel"
        open
      >
        <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border px-4 py-4">
          <h2 id={titleId} className="text-base font-medium leading-none">
            {title}
          </h2>
          <Button type="button" variant="ghost" size="icon-sm" onClick={onClose} aria-label="Close">
            <X size={16} />
          </Button>
        </header>
        <div className="app-drawer-body">{children}</div>
      </dialog>
    </div>,
    getOverlayRoot(),
  );
}
