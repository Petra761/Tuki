import { useCallback, useEffect, useRef, useState, type DependencyList } from "react";
import { useFocusEffect } from "expo-router";
import { errorMessage } from "@/api/client";

// Carga datos al abrir la pantalla y cada vez que se vuelve a ella (por ejemplo, al cerrar un formulario)
export function useResource<T>(load: () => Promise<T>, deps: DependencyList = []) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const loadRef = useRef(load);
  loadRef.current = load;
  // Evita que una respuesta lenta pise a una más reciente
  const requestId = useRef(0);
  const depKey = JSON.stringify(deps);

  const run = useCallback(async () => {
    const id = ++requestId.current;
    setLoading(true);
    try {
      const result = await loadRef.current();
      if (id !== requestId.current) return;
      setData(result);
      setError(null);
    } catch (e) {
      if (id !== requestId.current) return;
      setError(errorMessage(e));
    } finally {
      if (id === requestId.current) {
        setLoading(false);
        setRefreshing(false);
      }
    }
  }, []);

  // Al cambiar los parámetros (por ejemplo, el mes) se descartan los datos anteriores
  useEffect(() => {
    setData(null);
  }, [depKey]);

  useFocusEffect(
    useCallback(() => {
      void run();
    }, [run, depKey]),
  );

  const refresh = useCallback(() => {
    setRefreshing(true);
    void run();
  }, [run]);

  return { data, error, loading, refreshing, reload: run, refresh };
}
