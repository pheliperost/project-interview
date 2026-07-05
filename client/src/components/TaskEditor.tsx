import { useEffect, useId, useState } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { toast } from 'sonner';
import type { KanbanStatus, Task, TaskPriority } from '@/api/types';
import { COLUMNS } from '@/api/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { lockPageScroll, unlockPageScroll } from '@/lib/scrollLock';
import { openDatePicker } from '@/lib/openDatePicker';
import { getOverlayRoot } from '@/lib/overlayRoot';

interface TaskEditorProps {
  open: boolean;
  task?: Task | null;
  defaultStatus?: KanbanStatus;
  onClose: () => void;
  onSave: (data: {
    title: string;
    description: string;
    priority: TaskPriority;
    dueDate?: string;
    status?: KanbanStatus;
  }) => Promise<void>;
}

function todayLocalDateString() {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export function TaskEditor({ open, task, defaultStatus, onClose, onSave }: TaskEditorProps) {
  const titleId = useId();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<TaskPriority>('Medium');
  const [status, setStatus] = useState<KanbanStatus>('Todo');
  const [dueDate, setDueDate] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setTitle(task?.title ?? '');
    setDescription(task?.description ?? '');
    setPriority(task?.priority ?? 'Medium');
    setStatus(task?.status ?? defaultStatus ?? 'Todo');
    setDueDate(task?.dueDate ? task.dueDate.slice(0, 10) : '');
  }, [open, task, defaultStatus]);

  useEffect(() => {
    if (!open) return;
    lockPageScroll();

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }

    window.addEventListener('keydown', onKeyDown);

    return () => {
      unlockPageScroll();
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [open, onClose]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      await onSave({
        title,
        description,
        priority,
        dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
        status: task ? status : (defaultStatus ?? 'Todo'),
      });
      onClose();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to save task');
    } finally {
      setLoading(false);
    }
  }

  if (!open) return null;

  const dialogTitle = task ? 'Edit task' : 'New task';

  return createPortal(
    <div className="task-editor-shell">
      <button
        type="button"
        className="absolute inset-0 bg-black/60"
        aria-label="Close"
        onClick={onClose}
      />
      <aside
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="task-editor-panel"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border px-4 py-4">
          <h2 id={titleId} className="text-base font-medium leading-none">
            {dialogTitle}
          </h2>
          <Button type="button" variant="ghost" size="icon-sm" onClick={onClose} aria-label="Close">
            <X size={16} />
          </Button>
        </header>

        <form onSubmit={handleSubmit} className="task-editor-form">
          <div className="task-editor-body">
            <div className="space-y-2">
              <Label htmlFor="task-title">Title</Label>
              <Input
                id="task-title"
                required
                maxLength={200}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="task-description">Description</Label>
              <Textarea
                id="task-description"
                required
                maxLength={2000}
                rows={3}
                className="min-w-0"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div className="min-w-0 space-y-2">
                <Label>Priority</Label>
                <Select value={priority} onValueChange={(v) => setPriority(v as TaskPriority)}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent side="bottom">
                    {(['Low', 'Medium', 'High', 'Urgent'] as TaskPriority[]).map((p) => (
                      <SelectItem key={p} value={p}>
                        {p}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="min-w-0 space-y-2">
                <Label htmlFor="task-due-date">Due date</Label>
                <input
                  id="task-due-date"
                  type="date"
                  min={task ? undefined : todayLocalDateString()}
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  onClick={openDatePicker}
                  className="task-date-input h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30"
                />
              </div>
            </div>
            {task && (
              <div className="min-w-0 space-y-2">
                <Label>Status</Label>
                <Select value={status} onValueChange={(v) => setStatus(v as KanbanStatus)}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent side="top">
                    {COLUMNS.map((col) => (
                      <SelectItem key={col.status} value={col.status}>
                        {col.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>

          <footer className="task-editor-footer">
            <Button type="button" variant="outline" className="w-full md:w-auto" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" className="w-full md:w-auto" disabled={loading}>
              {loading ? 'Saving…' : 'Save'}
            </Button>
          </footer>
        </form>
      </aside>
    </div>,
    getOverlayRoot(),
  );
}
