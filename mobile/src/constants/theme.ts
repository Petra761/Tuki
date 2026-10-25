// Paleta tomada de la landing de Tuki, para que la app y la página se vean como el mismo producto.
export const colors = {
  background: "#0E1F1A",
  // Más oscuro que el fondo, para zonas que deben hundirse (barra de pestañas, fondos de modales)
  backgroundDeep: "#0A1311",
  surface: "#15302A",
  surfaceAlt: "#1D3D2F",
  border: "#2A3A35",
  text: "#F1F4EE",
  textMuted: "#8CA096",
  textDim: "#4A5E56",
  primary: "#C8F25A",
  // El lima es claro, por eso el texto sobre él usa el verde oscuro del fondo
  onPrimary: "#0E1F1A",
  // Gastos y errores; los ingresos usan el color primario
  danger: "#FF6A4D",
} as const;
