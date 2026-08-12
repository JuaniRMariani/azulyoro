"use client";

import { useEffect, useRef, useState } from "react";
import { FormMessage } from "@/components/ui/FormMessage";

type State = "loading" | "success" | "error" | "missing";

/**
 * Fires a one-shot POST to a same-origin proxy route with { token, email } read
 * from the URL, then renders the localized result. Used by newsletter
 * confirm/unsubscribe.
 */
export function TokenActionClient({
  endpoint,
  token,
  email,
  labels,
}: {
  endpoint: string;
  token: string;
  email: string;
  labels: {
    loading: string;
    success: string;
    error: string;
    missing: string;
  };
}) {
  const [state, setState] = useState<State>(token && email ? "loading" : "missing");
  const ran = useRef(false);

  useEffect(() => {
    if (ran.current || !token || !email) return;
    ran.current = true;
    (async () => {
      try {
        const res = await fetch(endpoint, {
          method: "POST",
          headers: { "content-type": "application/json" },
          body: JSON.stringify({ token, email }),
        });
        setState(res.ok ? "success" : "error");
      } catch {
        setState("error");
      }
    })();
  }, [endpoint, token, email]);

  const variant =
    state === "success" ? "success" : state === "loading" ? "info" : "error";

  return <FormMessage variant={variant}>{labels[state]}</FormMessage>;
}
