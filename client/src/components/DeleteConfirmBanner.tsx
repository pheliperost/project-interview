import { Button } from '@/components/ui/button';

interface DeleteConfirmBannerProps {
  onCancel: () => void;
  onConfirm: () => void;
}

export function DeleteConfirmBanner({ onCancel, onConfirm }: DeleteConfirmBannerProps) {
  return (
    <div
      role="alertdialog"
      aria-labelledby="delete-confirm-title"
      className="mb-3 rounded-xl border border-destructive/30 bg-destructive/10 p-3"
    >
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <h2 id="delete-confirm-title" className="text-sm font-medium">
            Delete task?
          </h2>
          <p className="text-sm text-muted-foreground">This action cannot be undone.</p>
        </div>
        <div className="flex shrink-0 gap-2">
          <Button type="button" variant="outline" size="sm" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="button" variant="destructive" size="sm" onClick={onConfirm}>
            Delete
          </Button>
        </div>
      </div>
    </div>
  );
}
