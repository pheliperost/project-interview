import { useDroppable } from '@dnd-kit/core';
import type { KanbanStatus, Task } from '@/api/types';
import { TaskCard } from './TaskCard';

interface KanbanColumnProps {
  status: KanbanStatus;
  label: string;
  terminal?: boolean;
  tasks: Task[];
  onEdit: (task: Task) => void;
  onReactivate: (taskId: string) => Promise<void>;
  onDelete: (taskId: string) => void;
}

export function KanbanColumn({
  status,
  label,
  terminal,
  tasks,
  onEdit,
  onReactivate,
  onDelete,
}: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: status });

  return (
    <section
      ref={setNodeRef}
      className={`flex min-w-[280px] max-w-[320px] flex-shrink-0 flex-col rounded-xl border bg-zinc-900/60 ${
        isOver ? 'border-lime-400/60' : 'border-zinc-800'
      }`}
    >
      <header className="flex items-center justify-between border-b border-zinc-800 px-4 py-3">
        <h2 className="font-semibold">{label}</h2>
        <span className="rounded-full bg-zinc-800 px-2 py-0.5 text-xs text-zinc-400">
          {tasks.length}
        </span>
      </header>
      <div className="flex flex-1 flex-col gap-3 overflow-y-auto p-3 min-h-[120px]">
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
              onReactivate={terminal ? () => onReactivate(task.id) : undefined}
            />
          ))
        )}
      </div>
    </section>
  );
}
