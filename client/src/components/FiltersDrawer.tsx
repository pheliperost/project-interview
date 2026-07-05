import { useEffect } from 'react';
import type { TaskFilters } from '@/api/types';
import { AppDrawer } from '@/components/AppDrawer';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { FilterPanel } from '@/components/SidebarFilters';

interface FiltersDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  filters: TaskFilters;
  onChange: (filters: TaskFilters) => void;
  email: string | null;
  onLogout: () => void;
}

export function FiltersDrawer({
  open,
  onOpenChange,
  filters,
  onChange,
  email,
  onLogout,
}: FiltersDrawerProps) {
  const isLargeScreen = useMediaQuery('(min-width: 1024px)');

  useEffect(() => {
    if (isLargeScreen && open) onOpenChange(false);
  }, [isLargeScreen, open, onOpenChange]);

  if (isLargeScreen) return null;

  return (
    <AppDrawer open={open} onClose={() => onOpenChange(false)} title="Filters">
      <FilterPanel
        filters={filters}
        onChange={onChange}
        email={email}
        showSearch={false}
        variant="drawer"
        onLogout={() => {
          onLogout();
          onOpenChange(false);
        }}
      />
    </AppDrawer>
  );
}
