/** Inline status message (no window.alert). Variants: error | success | info. */
export function FormMessage({
  variant = "info",
  children,
}: {
  variant?: "error" | "success" | "info";
  children: React.ReactNode;
}) {
  const styles: Record<string, string> = {
    error:
      "border-[color-mix(in_oklab,var(--live)_45%,transparent)] bg-[color-mix(in_oklab,var(--live)_10%,transparent)] text-[var(--live)]",
    success:
      "border-[color-mix(in_oklab,var(--primary)_45%,transparent)] bg-[color-mix(in_oklab,var(--primary)_10%,transparent)] text-[var(--primary)]",
    info: "border-[var(--border)] bg-[var(--muted)] text-[var(--foreground)]",
  };
  return (
    <p
      role={variant === "error" ? "alert" : "status"}
      className={`rounded-lg border px-3 py-2 text-sm ${styles[variant]}`}
    >
      {children}
    </p>
  );
}
