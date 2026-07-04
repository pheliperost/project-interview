import { COLUMNS } from '@/api/types';

export function BoardSkeleton() {
  return (
    <div className="flex flex-1 gap-4 overflow-x-auto pb-4">
      {COLUMNS.map((col) => (
        <section
          key={col.status}
          className="flex min-w-[280px] max-w-[320px] flex-shrink-0 flex-col rounded-xl border border-zinc-800 bg-zinc-900/60"
        >
          <header className="flex items-center justify-between border-b border-zinc-800 px-4 py-3">
            <div className="h-5 w-24 animate-pulse rounded bg-zinc-800" />
            <div className="h-5 w-8 animate-pulse rounded-full bg-zinc-800" />
          </header>
          <div className="flex flex-col gap-3 p-3">
            {Array.from({ length: col.terminal ? 1 : 2 }).map((_, i) => (
              <div
                key={i}
                className="animate-pulse rounded-xl border border-zinc-800 bg-zinc-950 p-3"
              >
                <div className="mb-2 flex gap-2">
                  <div className="h-4 w-14 rounded bg-zinc-800" />
                  <div className="h-4 w-20 rounded bg-zinc-800" />
                </div>
                <div className="h-4 w-3/4 rounded bg-zinc-800" />
                <div className="mt-2 h-3 w-full rounded bg-zinc-800/80" />
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
