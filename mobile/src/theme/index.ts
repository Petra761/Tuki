// Paleta tomada de la landing de Tuki, para que la app y la página se vean como el mismo producto.
export const colors = {
  background: "#0E1F1A",
  // Más oscuro que el fondo, para zonas que deben hundirse (barra de pestañas, fondos de modales)
  backgroundDeep: "#0A1311",
  surface: "#15302A",
  surfaceAlt: "#1D3D2F",
  // Equivale a --linea de la landing: el color del texto al 12 % de opacidad
  border: "rgba(241, 244, 238, 0.12)",
  text: "#F1F4EE",
  textMuted: "#8CA096",
  textDim: "#4A5E56",
  primary: "#C8F25A",
  // El lima es claro, por eso el texto sobre él usa el verde oscuro del fondo
  onPrimary: "#0E1F1A",
  // Gastos y errores; los ingresos usan el color primario
  danger: "#FF6A4D",
} as const;

// Cada peso es una fuente distinta: los nombres deben coincidir con los que se cargan en el layout raíz.
// Títulos: Bricolage Grotesque. Texto: Onest (las mismas de la landing).
export const fontFamily = {
  title: "BricolageGrotesque_700Bold",
  body: "Onest_400Regular",
  bodyMedium: "Onest_500Medium",
  bodyBold: "Onest_700Bold",
} as const;

export const spacing = { xs: 4, sm: 8, md: 16, lg: 24, xl: 32 } as const;

export const radius = { sm: 8, md: 12, lg: 20, full: 999 } as const;

// xxl es para los importes grandes de las pantallas de resumen
export const fontSize = { sm: 13, md: 16, lg: 20, xl: 28, xxl: 38 } as const;

// Colores de las porciones del gráfico circular; el primero es el lima de la marca
export const chartColors = ["#C8F25A", "#5EC9A4", "#E8C468", "#FF6A4D", "#7DA8E8", "#B58FE6"] as const;
