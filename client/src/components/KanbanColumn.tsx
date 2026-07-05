import { useDroppable } from '@dnd-kit/core';
import type { KanbanStatus, Task } from '@/api/types';
import { TaskCard } from './TaskCard';

interface KanbanColumnProps {
  status: KanbanStatus;
  label: string;
  terminal?: boolean;
  tasks: Task[];
  onEdit: (task: Task) => void;
  onMoveTo: (taskId: string, status: KanbanStatus) => void;
  onDelete: (taskId: string) => void;
}

export function KanbanColumn({
  status,
  label,
  terminal,
  tasks,
  onEdit,
  onMoveTo,
  onDelete,
}: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: status });

  return (
    <section
      ref={setNodeRef}
      className={`kanban-column flex h-full min-h-0 flex-shrink-0 flex-col rounded-xl border bg-zinc-900/60 ${
        isOver ? 'border-lime-400/60' : 'border-zinc-800'
      }`}
    >
      <header className="flex shrink-0 items-center justify-between border-b border-zinc-800 px-3 py-3 sm:px-4">
        <h2 className="truncate text-sm font-semibold sm:text-base">{label}</h2>
        <span className="rounded-full bg-zinc-800 px-2 py-0.5 text-xs text-zinc-400">
          {tasks.length}
        </span>
      </header>
      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto overscroll-y-contain p-3">
        {tasks.length === 0 ? (
          <p className="py-8 text-center text-xs text-zinc-600">No tasks in this column</p>
        ) : (
          tasks.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              draggable={!terminal}
              onEdit={() => onEdit(task)}
              onDelete={() => onDelete(task.id)}
              onMoveTo={(status) => onMoveTo(task.id, status)}
            />
          ))
        )}
      </div>
    </section>
  );
}
