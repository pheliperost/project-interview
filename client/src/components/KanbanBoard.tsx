import {
  DndContext,
  DragOverlay,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import { forwardRef, useMemo, useState } from 'react';
import { toast } from 'sonner';
import type { KanbanStatus, Task } from '@/api/types';
import { COLUMNS } from '@/api/types';
import { KanbanColumn } from './KanbanColumn';
import { TaskCard } from './TaskCard';

interface KanbanBoardProps {
  tasks: Task[];
  onStatusChange: (taskId: string, status: KanbanStatus) => Promise<void>;
  onEdit: (task: Task) => void;
  onMoveTo: (taskId: string, status: KanbanStatus) => void;
  onDelete: (taskId: string) => void;
}

export const KanbanBoard = forwardRef<HTMLDivElement, KanbanBoardProps>(function KanbanBoard(
  { tasks, onStatusChange, onEdit, onMoveTo, onDelete },
  ref,
) {
  const [activeTask, setActiveTask] = useState<Task | null>(null);
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }));

  const grouped = useMemo(() => {
    const map = Object.fromEntries(COLUMNS.map((c) => [c.status, [] as Task[]])) as Record<
      KanbanStatus,
      Task[]
    >;
    tasks.forEach((t) => map[t.status]?.push(t));
    return map;
  }, [tasks]);

  function handleWheel(e: React.WheelEvent<HTMLDivElement>) {
    const board = e.currentTarget;
    const canScrollX = board.scrollWidth > board.clientWidth;

    if (e.shiftKey && canScrollX) {
      board.scrollLeft += e.deltaY;
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    if (canScrollX && Math.abs(e.deltaX) > Math.abs(e.deltaY)) {
      e.stopPropagation();
    }
  }

  async function handleDragEnd(event: DragEndEvent) {
    const taskId = String(event.active.id);
    const newStatus = event.over?.id as KanbanStatus | undefined;
    setActiveTask(null);
    if (!newStatus) return;

    const task = tasks.find((t) => t.id === taskId);
    if (!task || task.status === newStatus) return;

    try {
      await onStatusChange(taskId, newStatus);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to move task');
    }
  }

  function handleDragStart(event: DragStartEvent) {
    const task = tasks.find((t) => t.id === event.active.id);
    setActiveTask(task ?? null);
  }

  return (
    <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <div className="flex min-h-0 flex-1 flex-col">
        <div
          ref={ref}
          data-kanban-scroll
          onWheel={handleWheel}
          className="kanban-scroll flex h-full min-h-0 flex-1 gap-3 overflow-x-auto overflow-y-hidden overscroll-x-contain overscroll-y-none sm:gap-4"
        >
          {COLUMNS.map((col) => (
            <KanbanColumn
              key={col.status}
              status={col.status}
              label={col.label}
              terminal={col.terminal}
              tasks={grouped[col.status]}
              onEdit={onEdit}
              onMoveTo={onMoveTo}
              onDelete={onDelete}
            />
          ))}
        </div>
      </div>
      <DragOverlay>
        {activeTask ? (
          <div className="w-72 rotate-2 opacity-90">
            <TaskCard task={activeTask} onEdit={() => {}} onDelete={() => {}} />
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
});

/** Scroll board horizontally to center a task card — never touches page scroll. */
export function scrollBoardToTask(board: HTMLDivElement, taskId: string) {
  const card = board.querySelector<HTMLElement>(`[data-task-id="${taskId}"]`);
  if (!card) return;

  const boardRect = board.getBoundingClientRect();
  const cardRect = card.getBoundingClientRect();
  const cardCenter = cardRect.left - boardRect.left + board.scrollLeft + cardRect.width / 2;

  board.scrollLeft = Math.max(0, cardCenter - board.clientWidth / 2);
}
