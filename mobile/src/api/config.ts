// La URL sale de una variable pública de Expo para pasar a remoto sin tocar el código
const url = process.env.EXPO_PUBLIC_API_URL;

if (!url) {
  throw new Error("Falta EXPO_PUBLIC_API_URL en .env (ejemplo: http://192.168.1.10:5005)");
}

// Sin barra final, para poder concatenar rutas que empiezan con "/"
export const API_URL = url.replace(/\/+$/, "");
