// Root passthrough layout. The real <html>/<body> is rendered by
// app/[locale]/layout.tsx so the `lang` attribute can vary per locale.
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return children;
}
