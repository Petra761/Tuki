import { Redirect } from "expo-router";

// "/" sin sesión cae aquí: el grupo (app) está fuera del árbol por Stack.Protected,
// así que esta es la única ruta que resuelve la raíz para quien no ha entrado
export default function AuthIndex() {
  return <Redirect href="/login" />;
}
