export type KanbanStatus =
  | 'Todo'
  | 'InProgress'
  | 'OnHold'
  | 'InReview'
  | 'Completed'
  | 'Cancelled';

export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export interface Task {
  id: string;
  title: string;
  description: string;
  status: KanbanStatus;
  priority: TaskPriority;
  dueDate?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
  resetLink?: string | null;
}

export interface ResetPasswordResponse {
  message: string;
}

export interface TaskListResponse {
  items: Task[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TaskFilters {
  search: string;
  statuses: KanbanStatus[];
  createdPreset: DatePreset;
  updatedPreset: DatePreset;
  createdFrom?: string;
  createdTo?: string;
  updatedFrom?: string;
  updatedTo?: string;
}

export type DatePreset = 'any' | 'today' | 'last7' | 'last30' | 'custom';

export const COLUMNS: { status: KanbanStatus; label: string; terminal?: boolean }[] = [
  { status: 'Todo', label: 'To Do' },
  { status: 'InProgress', label: 'In Progress' },
  { status: 'OnHold', label: 'On Hold' },
  { status: 'InReview', label: 'In Review' },
  { status: 'Completed', label: 'Done', terminal: true },
  { status: 'Cancelled', label: 'Canceled', terminal: true },
];

export const ALL_STATUSES: KanbanStatus[] = COLUMNS.map((c) => c.status);

export const PRIORITY_COLORS: Record<TaskPriority, string> = {
  Low: 'bg-zinc-600',
  Medium: 'bg-orange-500',
  High: 'bg-amber-400 text-black',
  Urgent: 'bg-purple-500',
};
