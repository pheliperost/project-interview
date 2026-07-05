import { useDraggable } from '@dnd-kit/core';
import { AlertCircle, Calendar, MoreHorizontal } from 'lucide-react';
import { useState } from 'react';
import type { KanbanStatus, Task } from '@/api/types';
import { COLUMNS, PRIORITY_COLORS } from '@/api/types';
import { DUE_DATE_STYLES, formatDate, getDueDateUrgency } from '@/lib/utils';

interface TaskCardProps {
  task: Task;
  draggable?: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onMoveTo?: (status: KanbanStatus) => void;
}

export function TaskCard({ task, draggable = true, onEdit, onDelete, onMoveTo }: TaskCardProps) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: task.id,
    disabled: !draggable,
  });
  const [menuOpen, setMenuOpen] = useState(false);

  const dueUrgency = getDueDateUrgency(task.dueDate);
  const dueClass = dueUrgency ? DUE_DATE_STYLES[dueUrgency] : '';
  const moveTargets = COLUMNS.filter((col) => col.status !== task.status);

  const style = transform
    ? { transform: `translate(${transform.x}px, ${transform.y}px)`, opacity: isDragging ? 0.5 : 1 }
    : undefined;

  return (
    <article
      ref={setNodeRef}
      data-task-id={task.id}
      style={style}
      className={`rounded-xl border bg-zinc-950 p-3 shadow-sm transition-colors ${
        dueUrgency === 'overdue'
          ? 'border-red-900/60'
          : dueUrgency === 'soon'
            ? 'border-amber-900/40'
            : 'border-zinc-800'
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1" {...(draggable ? { ...listeners, ...attributes } : {})}>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <span
              className={`rounded px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide ${PRIORITY_COLORS[task.priority]}`}
            >
              {task.priority}
            </span>
            {task.dueDate && (
              <span className={`flex items-center gap-1 text-xs ${dueClass}`}>
                {dueUrgency === 'overdue' ? (
                  <AlertCircle size={12} aria-hidden />
                ) : (
                  <Calendar size={12} aria-hidden />
                )}
                {formatDate(task.dueDate)}
                {dueUrgency === 'overdue' && <span className="sr-only">overdue</span>}
              </span>
            )}
          </div>
          <h3 className="font-medium leading-snug">{task.title}</h3>
          {task.description && (
            <p className="mt-1 line-clamp-2 text-sm text-zinc-400">{task.description}</p>
          )}
        </div>
        <div className="relative">
          <button
            type="button"
            onClick={() => setMenuOpen((v) => !v)}
            className="rounded p-1 text-zinc-400 hover:bg-zinc-800"
            aria-label="Task actions"
          >
            <MoreHorizontal size={16} />
          </button>
          {menuOpen && (
            <div className="absolute right-0 z-10 mt-1 w-40 rounded-lg border border-zinc-700 bg-zinc-900 py-1 shadow-lg">
              <button
                type="button"
                className="block w-full px-3 py-1.5 text-left text-sm hover:bg-zinc-800"
                onClick={() => {
                  setMenuOpen(false);
                  onEdit();
                }}
              >
                Edit
              </button>
              {onMoveTo &&
                moveTargets.map((col) => (
                  <button
                    key={col.status}
                    type="button"
                    className="block w-full px-3 py-1.5 text-left text-sm hover:bg-zinc-800"
                    onClick={() => {
                      setMenuOpen(false);
                      onMoveTo(col.status);
                    }}
                  >
                    Move to {col.label}
                  </button>
                ))}
              <button
                type="button"
                className="block w-full px-3 py-1.5 text-left text-sm text-red-400 hover:bg-zinc-800"
                onClick={() => {
                  setMenuOpen(false);
                  onDelete();
                }}
              >
                Delete
              </button>
            </div>
          )}
        </div>
      </div>
    </article>
  );
}
