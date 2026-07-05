import { Search, X } from 'lucide-react';
import type { TaskFilters } from '@/api/types';
import { DEFAULT_TASK_FILTERS, hasAnyFiltersActive } from '@/components/SidebarFilters';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

interface MobileSearchBarProps {
  filters: TaskFilters;
  onChange: (filters: TaskFilters) => void;
}

export function MobileSearchBar({ filters, onChange }: MobileSearchBarProps) {
  function clearAll() {
    onChange(DEFAULT_TASK_FILTERS);
  }

  return (
    <div className="mb-4 space-y-2 lg:hidden">
      <div className="relative">
        <Search
          size={16}
          className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted-foreground"
          aria-hidden
        />
        <Input
          type="search"
          placeholder="Search tasks..."
          value={filters.search}
          onChange={(e) => onChange({ ...filters, search: e.target.value })}
          className={cn('pl-9', filters.search && 'pr-9')}
        />
        {filters.search && (
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="absolute top-1/2 right-1 -translate-y-1/2"
            aria-label="Clear search"
            onClick={() => onChange({ ...filters, search: '' })}
          >
            <X size={14} />
          </Button>
        )}
      </div>
      {hasAnyFiltersActive(filters) && (
        <div className="flex items-center justify-between px-1">
          <span className="text-xs text-muted-foreground">Filters applied</span>
          <Button type="button" variant="link" className="h-auto p-0 text-xs text-primary" onClick={clearAll}>
            Clear all
          </Button>
        </div>
      )}
    </div>
  );
}
