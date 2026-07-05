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
    <div className="fixed inset-0 z-[9999] flex flex-col justify-end overflow-hidden md:items-center md:justify-center md:p-4">
      <button
        type="button"
        className="absolute inset-0 bg-black/60"
        aria-label="Close"
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="relative z-10 flex max-h-[92svh] w-full min-w-0 flex-col overflow-hidden rounded-t-2xl border border-border bg-popover text-popover-foreground shadow-2xl md:max-h-[min(90svh,640px)] md:max-w-lg md:rounded-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="flex shrink-0 items-start justify-between gap-3 border-b border-border px-4 py-4">
          <h2 id={titleId} className="text-base font-medium leading-snug">
            {dialogTitle}
          </h2>
          <Button type="button" variant="ghost" size="icon-sm" onClick={onClose} aria-label="Close">
            <X size={16} />
          </Button>
        </header>

        <form onSubmit={handleSubmit} className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
          <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-x-hidden overflow-y-auto overscroll-contain px-4 py-4">
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
                  <SelectContent>
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
                <Input
                  id="task-due-date"
                  type="date"
                  min={task ? undefined : todayLocalDateString()}
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  className="task-date-input w-full"
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
                  <SelectContent>
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

          <footer className="flex shrink-0 flex-col gap-2 border-t border-border bg-muted/50 px-4 py-4 pb-[max(1rem,env(safe-area-inset-bottom))] md:flex-row md:justify-end">
            <Button type="button" variant="outline" className="w-full md:w-auto" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" className="w-full md:w-auto" disabled={loading}>
              {loading ? 'Saving…' : 'Save'}
            </Button>
          </footer>
        </form>
      </div>
    </div>,
    document.body,
  );
}
