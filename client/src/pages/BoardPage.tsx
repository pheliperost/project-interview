import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { api, buildDateRange } from '@/api/client';
import type { KanbanStatus, Task, TaskFilters } from '@/api/types';
import { ALL_STATUSES } from '@/api/types';
import { useAuth } from '@/auth/AuthContext';
import { KanbanBoard } from '@/components/KanbanBoard';
import { BoardSkeleton } from '@/components/BoardSkeleton';
import { SidebarFilters } from '@/components/SidebarFilters';
import { TaskDialog } from '@/components/TaskDialog';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

const defaultFilters: TaskFilters = {
  search: '',
  statuses: ALL_STATUSES,
  createdPreset: 'any',
  updatedPreset: 'any',
};

export function BoardPage() {
  const { email, logout } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<TaskFilters>(defaultFilters);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);
  const [showTip, setShowTip] = useState(() => !localStorage.getItem('scroll_tip_dismissed'));

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

  return (
    <div className="flex h-screen overflow-hidden">
      <SidebarFilters
        filters={filters}
        onChange={setFilters}
        email={email}
        onLogout={handleLogout}
      />
      <main className="flex flex-1 flex-col overflow-hidden p-4">
        <header className="mb-4 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">My Kanban Board</h1>
            {showTip && (
              <p className="mt-1 text-xs text-zinc-500">
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
          {(isLoading || isFetching) && (
            <span className="text-sm text-zinc-500">Loading…</span>
          )}
        </header>

        {isLoading ? (
          <BoardSkeleton />
        ) : (
          <KanbanBoard
            tasks={tasks}
            onStatusChange={handleStatusChange}
            onEdit={(task) => {
              setEditingTask(task);
              setDialogOpen(true);
            }}
            onReactivate={async (id) => {
              await reactivateMutation.mutateAsync(id);
            }}
            onDelete={(id) => setDeleteConfirm(id)}
          />
        )}

        <Button
          type="button"
          size="icon-lg"
          onClick={() => {
            setEditingTask(null);
            setDialogOpen(true);
          }}
          className="fixed bottom-6 right-6 size-14 rounded-full shadow-lg"
          aria-label="Create task"
        >
          <Plus size={24} />
        </Button>
      </main>

      <TaskDialog
        open={dialogOpen}
        task={editingTask}
        onClose={() => setDialogOpen(false)}
        onSave={async (data) => {
          if (editingTask) {
            await updateMutation.mutateAsync({
              id: editingTask.id,
              body: {
                title: data.title,
                description: data.description,
                status: editingTask.status,
                priority: data.priority,
                dueDate: data.dueDate ?? null,
              },
            });
          } else {
            await createMutation.mutateAsync(data);
          }
        }}
      />

      <Dialog open={Boolean(deleteConfirm)} onOpenChange={(open) => !open && setDeleteConfirm(null)}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>Delete task?</DialogTitle>
            <DialogDescription>This action cannot be undone.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDeleteConfirm(null)}>
              Cancel
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={async () => {
                if (!deleteConfirm) return;
                await deleteMutation.mutateAsync(deleteConfirm);
                setDeleteConfirm(null);
              }}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
