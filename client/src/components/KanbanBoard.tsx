import {
  DndContext,
  DragOverlay,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import { useMemo, useRef, useState } from 'react';
import { toast } from 'sonner';
import type { KanbanStatus, Task } from '@/api/types';
import { COLUMNS } from '@/api/types';
import { KanbanColumn } from './KanbanColumn';
import { TaskCard } from './TaskCard';

interface KanbanBoardProps {
  tasks: Task[];
  onStatusChange: (taskId: string, status: KanbanStatus) => Promise<void>;
  onEdit: (task: Task) => void;
  onReactivate: (taskId: string) => Promise<void>;
  onDelete: (taskId: string) => void;
}

export function KanbanBoard({
  tasks,
  onStatusChange,
  onEdit,
  onReactivate,
  onDelete,
}: KanbanBoardProps) {
  const boardRef = useRef<HTMLDivElement>(null);
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

  function handleWheel(e: React.WheelEvent) {
    if (e.shiftKey && boardRef.current) {
      boardRef.current.scrollLeft += e.deltaY;
      e.preventDefault();
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
      <div
        ref={boardRef}
        onWheel={handleWheel}
        className="flex flex-1 gap-4 overflow-x-auto pb-4"
      >
        {COLUMNS.map((col) => (
          <KanbanColumn
            key={col.status}
            status={col.status}
            label={col.label}
            terminal={col.terminal}
            tasks={grouped[col.status]}
            onEdit={onEdit}
            onReactivate={onReactivate}
            onDelete={onDelete}
          />
        ))}
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
}
