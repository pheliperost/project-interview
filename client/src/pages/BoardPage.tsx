import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Filter, Plus } from 'lucide-react';
import { useMemo, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { api, buildDateRange } from '@/api/client';
import type { KanbanStatus, Task, TaskFilters } from '@/api/types';
import { ALL_STATUSES } from '@/api/types';
import { useAuth } from '@/auth/AuthContext';
import { DeleteConfirmBanner } from '@/components/DeleteConfirmBanner';
import { FiltersDrawer } from '@/components/FiltersDrawer';
import { KanbanBoard, scrollBoardToTask } from '@/components/KanbanBoard';
import { BoardSkeleton } from '@/components/BoardSkeleton';
import { MobileSearchBar } from '@/components/MobileSearchBar';
import {
  DEFAULT_TASK_FILTERS,
  hasDrawerFiltersActive,
  SidebarFilters,
} from '@/components/SidebarFilters';
import { TaskEditor } from '@/components/TaskEditor';
import { Button } from '@/components/ui/button';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { resetPageScrollLock } from '@/lib/scrollLock';

const defaultFilters: TaskFilters = DEFAULT_TASK_FILTERS;

function isTerminalStatus(status: KanbanStatus) {
  return status === 'Completed' || status === 'Cancelled';
}

export function BoardPage() {
  const { email, logout } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const boardRef = useRef<HTMLDivElement>(null);
  const pendingScrollRestore = useRef<{ taskId: string | null; scrollLeft: number } | null>(null);

  const [filters, setFilters] = useState<TaskFilters>(defaultFilters);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);
  const [showTip, setShowTip] = useState(() => !localStorage.getItem('scroll_tip_dismissed'));
  const isLargeScreen = useMediaQuery('(min-width: 1024px)');

  const queryFilters = useMemo(() => {
    const created = buildDateRange(filters.createdPreset, filters.createdFrom, filters.createdTo);
    const updated = buildDateRange(filters.updatedPreset, filters.updatedFrom, filters.updatedTo);
    const statuses =
      filters.statuses.length === ALL_STATUSES.length ? ALL_STATUSES : filters.statuses;
    return {
      ...filters,
      statuses,
      createdFrom: created.from,
      createdTo: created.to,
      updatedFrom: updated.from,
      updatedTo: updated.to,
    };
  }, [filters]);

  const { data: tasks = [], isLoading, isFetching } = useQuery({
    queryKey: ['tasks', queryFilters],
    queryFn: () => api.getTasks(queryFilters),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['tasks'] });

  const createMutation = useMutation({
    mutationFn: api.createTask,
    onSuccess: () => {
      invalidate();
      toast.success('Task created');
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body: Parameters<typeof api.updateTask>[1] }) =>
      api.updateTask(id, body),
    onSuccess: invalidate,
    onError: (e: Error) => toast.error(e.message),
  });

  const deleteMutation = useMutation({
    mutationFn: api.deleteTask,
    onSuccess: () => {
      invalidate();
      toast.success('Task deleted');
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const reactivateMutation = useMutation({
    mutationFn: api.reactivate,
    onSuccess: () => {
      invalidate();
      toast.success('Task moved to To Do');
    },
    onError: (e: Error) => toast.error(e.message),
  });

  async function handleLogout() {
    await logout();
    navigate('/login');
  }

  async function handleStatusChange(taskId: string, status: KanbanStatus) {
    const task = tasks.find((t) => t.id === taskId);
    if (!task) return;
    await updateMutation.mutateAsync({
      id: taskId,
      body: {
        title: task.title,
        description: task.description,
        status,
        priority: task.priority,
        dueDate: task.dueDate ?? null,
      },
    });
  }

  async function handleMoveTo(taskId: string, status: KanbanStatus) {
    const task = tasks.find((t) => t.id === taskId);
    if (!task || task.status === status) return;

    if (isTerminalStatus(task.status) && status === 'Todo') {
      await reactivateMutation.mutateAsync(taskId);
      return;
    }

    if (isTerminalStatus(task.status)) {
      toast.error('Done/Canceled tasks can only move to To Do');
      return;
    }

    await handleStatusChange(taskId, status);
  }

  function openEditor(task: Task | null) {
    pendingScrollRestore.current = {
      taskId: task?.id ?? null,
      scrollLeft: boardRef.current?.scrollLeft ?? 0,
    };
    setEditingTask(task);
    setEditorOpen(true);
    setDeleteConfirm(null);
  }

  function closeEditor() {
    setEditorOpen(false);
    setEditingTask(null);
  }

  useEffect(() => {
    if (editorOpen || !pendingScrollRestore.current) return;

    const { taskId, scrollLeft } = pendingScrollRestore.current;
    pendingScrollRestore.current = null;

    const timer = window.setTimeout(() => {
      const board = boardRef.current;
      if (!board) return;
      board.scrollLeft = scrollLeft;
      if (taskId) {
        scrollBoardToTask(board, taskId);
      }
    }, 0);

    return () => window.clearTimeout(timer);
  }, [editorOpen]);

  useEffect(() => {
    if (isLargeScreen && filtersOpen) setFiltersOpen(false);
  }, [isLargeScreen, filtersOpen]);

  useEffect(() => {
    function onViewportChange() {
      const board = boardRef.current;
      if (board) {
        const maxScroll = Math.max(0, board.scrollWidth - board.clientWidth);
        if (board.scrollLeft > maxScroll) board.scrollLeft = maxScroll;
      }
      if (!editorOpen && !filtersOpen) resetPageScrollLock();
    }

    window.addEventListener('resize', onViewportChange);
    window.visualViewport?.addEventListener('resize', onViewportChange);
    return () => {
      window.removeEventListener('resize', onViewportChange);
      window.visualViewport?.removeEventListener('resize', onViewportChange);
    };
  }, [editorOpen, filtersOpen]);

  async function handleSaveTask(data: {
    title: string;
    description: string;
    priority: Task['priority'];
    dueDate?: string;
    status?: KanbanStatus;
  }) {
    if (editingTask) {
      const newStatus = data.status ?? editingTask.status;

      if (isTerminalStatus(editingTask.status) && newStatus !== editingTask.status) {
        if (newStatus === 'Todo') {
          await reactivateMutation.mutateAsync(editingTask.id);
        } else {
          toast.error('Done/Canceled tasks can only move to To Do');
          return;
        }
      }

      const statusForUpdate = isTerminalStatus(editingTask.status)
        ? newStatus === 'Todo'
          ? 'Todo'
          : editingTask.status
        : newStatus;

      await updateMutation.mutateAsync({
        id: editingTask.id,
        body: {
          title: data.title,
          description: data.description,
          status: statusForUpdate,
          priority: data.priority,
          dueDate: data.dueDate ?? null,
        },
      });
    } else {
      await createMutation.mutateAsync(data);
    }
  }

  return (
    <div className="app-shell flex h-full min-h-0 w-full overflow-hidden">
      <SidebarFilters
        filters={filters}
        onChange={setFilters}
        email={email}
        onLogout={handleLogout}
      />
      <FiltersDrawer
        open={filtersOpen}
        onOpenChange={setFiltersOpen}
        filters={filters}
        onChange={setFilters}
        email={email}
        onLogout={handleLogout}
      />
      <main className="board-main flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden p-3 sm:p-4">
        <header className="mb-4 flex shrink-0 flex-wrap items-center justify-between gap-2">
          <div className="min-w-0">
            <h1 className="text-xl font-bold sm:text-2xl">My Kanban Board</h1>
            {showTip && (
              <p className="mt-1 hidden text-xs text-zinc-500 md:block">
                Tip: Hold <kbd className="rounded bg-zinc-800 px-1">Shift</kbd> and scroll to move
                sideways across columns.{' '}
                <button
                  type="button"
                  className="text-lime-400 hover:underline"
                  onClick={() => {
                    localStorage.setItem('scroll_tip_dismissed', '1');
                    setShowTip(false);
                  }}
                >
                  Dismiss
                </button>
              </p>
            )}
          </div>
          <div className="flex shrink-0 items-center gap-2">
            {(isLoading || isFetching) && (
              <span className="text-sm text-zinc-500">Loading…</span>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="relative lg:hidden"
              onClick={() => setFiltersOpen(true)}
            >
              <Filter size={16} />
              Filters
              {hasDrawerFiltersActive(filters) && (
                <span className="absolute top-1.5 right-1.5 size-2 rounded-full bg-primary" aria-hidden />
              )}
            </Button>
            <Button type="button" size="sm" className="sm:hidden" onClick={() => openEditor(null)}>
              <Plus size={16} />
              New
            </Button>
          </div>
        </header>

        <MobileSearchBar filters={filters} onChange={setFilters} />

        {deleteConfirm && (
          <DeleteConfirmBanner
            onCancel={() => setDeleteConfirm(null)}
            onConfirm={async () => {
              await deleteMutation.mutateAsync(deleteConfirm);
              setDeleteConfirm(null);
            }}
          />
        )}

        {isLoading ? (
          <BoardSkeleton />
        ) : (
          <div className="flex min-h-0 flex-1 flex-col">
            <KanbanBoard
              ref={boardRef}
              tasks={tasks}
              onStatusChange={handleStatusChange}
              onMoveTo={handleMoveTo}
              onEdit={(task) => openEditor(task)}
              onDelete={(id) => setDeleteConfirm(id)}
            />
          </div>
        )}

        <Button
          type="button"
          size="icon-lg"
          onClick={() => openEditor(null)}
          className="fixed right-4 bottom-4 z-40 hidden size-14 rounded-full shadow-lg sm:inline-flex sm:right-6 sm:bottom-6"
          aria-label="Create task"
        >
          <Plus size={24} />
        </Button>
      </main>

      <TaskEditor
        open={editorOpen}
        task={editingTask}
        onClose={closeEditor}
        onSave={handleSaveTask}
      />
    </div>
  );
}
