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

interface SidebarFiltersProps {
  filters: TaskFilters;
  onChange: (filters: TaskFilters) => void;
  email: string | null;
  onLogout: () => void;
}

const DATE_PRESETS: { value: DatePreset; label: string }[] = [
  { value: 'any', label: 'Any' },
  { value: 'today', label: 'Today' },
  { value: 'last7', label: 'Last 7 days' },
  { value: 'last30', label: 'Last 30 days' },
  { value: 'custom', label: 'Custom range' },
];

export function SidebarFilters({ filters, onChange, email, onLogout }: SidebarFiltersProps) {
  function toggleStatus(status: KanbanStatus) {
    const selected = filters.statuses.includes(status)
      ? filters.statuses.filter((s) => s !== status)
      : [...filters.statuses, status];
    onChange({ ...filters, statuses: selected.length ? selected : ALL_STATUSES });
  }

  function clearFilters() {
    onChange({
      search: '',
      statuses: ALL_STATUSES,
      createdPreset: 'any',
      updatedPreset: 'any',
    });
  }

  return (
    <aside className="flex w-72 flex-shrink-0 flex-col border-r border-border bg-card/80 p-4">
      <div className="mb-6">
        <p className="text-primary text-xs font-bold tracking-widest">SIMPLE TASKS</p>
        <p className="mt-2 truncate text-sm text-muted-foreground">{email}</p>
      </div>

      <Input
        type="search"
        placeholder="Search tasks..."
        value={filters.search}
        onChange={(e) => onChange({ ...filters, search: e.target.value })}
        className="mb-4"
      />

      <div className="mb-4 space-y-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Status</p>
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
        onChange={(createdPreset, createdFrom, createdTo) =>
          onChange({ ...filters, createdPreset, createdFrom, createdTo })
        }
      />

      <DateFilterGroup
        label="Updated"
        preset={filters.updatedPreset}
        from={filters.updatedFrom}
        to={filters.updatedTo}
        onChange={(updatedPreset, updatedFrom, updatedTo) =>
          onChange({ ...filters, updatedPreset, updatedFrom, updatedTo })
        }
      />

      <Button type="button" variant="link" onClick={clearFilters} className="mt-4 h-auto p-0 text-primary">
        Clear filters
      </Button>

      <Button type="button" variant="outline" onClick={onLogout} className="mt-auto">
        Logout
      </Button>
    </aside>
  );
}

function DateFilterGroup({
  label,
  preset,
  from,
  to,
  onChange,
}: {
  label: string;
  preset: DatePreset;
  from?: string;
  to?: string;
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
        <div className="grid grid-cols-2 gap-2">
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
