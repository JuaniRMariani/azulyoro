import type { InputHTMLAttributes, ReactNode } from "react";

/** Label + input pair. Required fields render a `(*)` marker per the UI rules. */
export function Field({
  id,
  label,
  required,
  hint,
  children,
  ...input
}: {
  id: string;
  label: string;
  required?: boolean;
  hint?: ReactNode;
  children?: ReactNode;
} & InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
        {required && (
          <span className="ml-0.5 text-[var(--live)]" aria-hidden>
            {" "}
            (*)
          </span>
        )}
      </label>
      {children ?? (
        <input
          id={id}
          required={required}
          aria-required={required}
          className="rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 py-2 text-sm outline-none transition-colors focus:border-[var(--primary)]"
          {...input}
        />
      )}
      {hint && <p className="text-xs text-[var(--muted-foreground)]">{hint}</p>}
    </div>
  );
}
