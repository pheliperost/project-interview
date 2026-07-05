import type { DatePreset, KanbanStatus, TaskFilters } from '@/api/types';
import { ALL_STATUSES, COLUMNS } from '@/api/types';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';

interface FilterPanelProps {
  filters: TaskFilters;
  onChange: (filters: TaskFilters) => void;
  email: string | null;
  onLogout: () => void;
  className?: string;
  showSearch?: boolean;
  variant?: 'sidebar' | 'drawer';
}

const DATE_PRESETS: { value: DatePreset; label: string }[] = [
  { value: 'any', label: 'Any' },
  { value: 'today', label: 'Today' },
  { value: 'last7', label: 'Last 7 days' },
  { value: 'last30', label: 'Last 30 days' },
  { value: 'custom', label: 'Custom range' },
];

export const DEFAULT_TASK_FILTERS: TaskFilters = {
  search: '',
  statuses: ALL_STATUSES,
  createdPreset: 'any',
  updatedPreset: 'any',
};

export function hasDrawerFiltersActive(filters: TaskFilters) {
  return (
    filters.statuses.length !== ALL_STATUSES.length ||
    filters.createdPreset !== 'any' ||
    filters.updatedPreset !== 'any'
  );
}

export function hasAnyFiltersActive(filters: TaskFilters) {
  return filters.search.trim() !== '' || hasDrawerFiltersActive(filters);
}

export function FilterPanel({
  filters,
  onChange,
  email,
  onLogout,
  className,
  showSearch = true,
  variant = 'sidebar',
}: FilterPanelProps) {
  function toggleStatus(status: KanbanStatus) {
    const selected = filters.statuses.includes(status)
      ? filters.statuses.filter((s) => s !== status)
      : [...filters.statuses, status];
    onChange({ ...filters, statuses: selected.length ? selected : ALL_STATUSES });
  }

  function clearFilters() {
    onChange({
      ...filters,
      search: variant === 'drawer' ? filters.search : '',
      statuses: ALL_STATUSES,
      createdPreset: 'any',
      updatedPreset: 'any',
      createdFrom: undefined,
      createdTo: undefined,
      updatedFrom: undefined,
      updatedTo: undefined,
    });
  }

  return (
    <div className={cn('flex flex-col', variant === 'drawer' && 'min-h-0 flex-1', className)}>
      {variant === 'sidebar' && (
        <div className="mb-6">
          <p className="text-primary text-xs font-bold tracking-widest">SIMPLE TASKS</p>
          <p className="mt-2 truncate text-sm text-muted-foreground">{email}</p>
        </div>
      )}

      {variant === 'drawer' && email && (
        <p className="mb-4 truncate text-sm text-muted-foreground">{email}</p>
      )}

      {showSearch && (
        <Input
          type="search"
          placeholder="Search tasks..."
          value={filters.search}
          onChange={(e) => onChange({ ...filters, search: e.target.value })}
          className="mb-4"
        />
      )}

      <div className={cn('mb-4 space-y-2', variant === 'drawer' && 'grid grid-cols-2 gap-x-3 gap-y-2 space-y-0')}>
        <p
          className={cn(
            'text-xs font-semibold uppercase tracking-wide text-muted-foreground',
            variant === 'drawer' && 'col-span-2',
          )}
        >
          Status
        </p>
        {COLUMNS.map((col) => (
          <label key={col.status} className="flex items-center gap-2 text-sm">
            <Checkbox
              checked={filters.statuses.includes(col.status)}
              onCheckedChange={() => toggleStatus(col.status)}
            />
            {col.label}
          </label>
        ))}
      </div>

      <DateFilterGroup
        label="Created"
        preset={filters.createdPreset}
        from={filters.createdFrom}
        to={filters.createdTo}
        compact={variant === 'drawer'}
        onChange={(createdPreset, createdFrom, createdTo) =>
          onChange({ ...filters, createdPreset, createdFrom, createdTo })
        }
      />

      <DateFilterGroup
        label="Updated"
        preset={filters.updatedPreset}
        from={filters.updatedFrom}
        to={filters.updatedTo}
        compact={variant === 'drawer'}
        onChange={(updatedPreset, updatedFrom, updatedTo) =>
          onChange({ ...filters, updatedPreset, updatedFrom, updatedTo })
        }
      />

      <Button type="button" variant="link" onClick={clearFilters} className="mt-4 h-auto p-0 text-primary">
        Clear filters
      </Button>

      <Button
        type="button"
        variant="outline"
        onClick={onLogout}
        className={cn(variant === 'drawer' ? 'mt-6' : 'mt-auto')}
      >
        Logout
      </Button>
    </div>
  );
}

interface SidebarFiltersProps {
  filters: TaskFilters;
  onChange: (filters: TaskFilters) => void;
  email: string | null;
  onLogout: () => void;
}

export function SidebarFilters(props: SidebarFiltersProps) {
  return (
    <aside className="sidebar-filters hidden shrink-0 flex-col border-r border-border bg-card/80 p-3 sm:p-4 lg:flex">
      <FilterPanel {...props} className="h-full min-h-0" />
    </aside>
  );
}

function DateFilterGroup({
  label,
  preset,
  from,
  to,
  compact = false,
  onChange,
}: {
  label: string;
  preset: DatePreset;
  from?: string;
  to?: string;
  compact?: boolean;
  onChange: (preset: DatePreset, from?: string, to?: string) => void;
}) {
  return (
    <div className="mb-4 space-y-2">
      <Label>{label}</Label>
      <Select value={preset} onValueChange={(v) => onChange(v as DatePreset, from, to)}>
        <SelectTrigger className="w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {DATE_PRESETS.map((p) => (
            <SelectItem key={p.value} value={p.value}>
              {p.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {preset === 'custom' && (
        <div className={cn('gap-2', compact ? 'flex flex-col' : 'grid grid-cols-2')}>
          <Input
            type="date"
            value={from?.slice(0, 10) ?? ''}
            onChange={(e) => onChange(preset, e.target.value, to)}
          />
          <Input
            type="date"
            value={to?.slice(0, 10) ?? ''}
            onChange={(e) => onChange(preset, from, e.target.value)}
          />
        </div>
      )}
    </div>
  );
}
