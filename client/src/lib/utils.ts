import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(value?: string) {
  if (!value) return '';
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
  }).format(new Date(value));
}

export type DueDateUrgency = 'overdue' | 'soon' | 'normal';

export function getDueDateUrgency(dueDate?: string): DueDateUrgency | null {
  if (!dueDate) return null;

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const due = new Date(dueDate);
  due.setHours(0, 0, 0, 0);

  if (due < today) return 'overdue';

  const soon = new Date(today);
  soon.setDate(soon.getDate() + 3);
  if (due <= soon) return 'soon';

  return 'normal';
}

export const DUE_DATE_STYLES: Record<DueDateUrgency, string> = {
  overdue: 'text-red-400 font-medium',
  soon: 'text-amber-400',
  normal: 'text-zinc-500',
};
