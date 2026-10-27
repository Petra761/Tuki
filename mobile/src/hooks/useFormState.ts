import { useState } from "react";

// Estado de un formulario que se reinicia cada vez que cambia lo que se está editando (key)
export function useFormState<T>(key: unknown, initial: () => T) {
  const [state, setState] = useState<T>(initial);
  const [loadedFor, setLoadedFor] = useState<unknown>(key);
  if (key !== loadedFor) {
    setLoadedFor(key);
    setState(initial());
  }
  return [state, setState] as const;
}
