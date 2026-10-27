import { useState } from "react";
import { StyleSheet, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { PressableScale } from "@/components/Button";
import { Screen } from "@/components/Layout";
import { Text } from "@/components/Text";
import { colors, radius, spacing } from "@/theme";

const QUESTIONS = [
  {
    q: "¿Cómo registro un gasto?",
    a: "Toca el botón + de la barra, elige Gasto, escribe el monto y selecciona la cuenta de donde salió el dinero y la categoría. Si compras lo mismo seguido, crea un atajo y regístralo desde Inicio con un toque.",
  },
  {
    q: "¿Qué es un traslado?",
    a: "Es mover dinero entre tus propias cuentas, por ejemplo de Efectivo a Ahorros. No cuenta como gasto ni como ingreso, porque no consumiste nada.",
  },
  {
    q: "¿Cómo anoto el dinero que presto o debo?",
    a: "Crea un tipo de cuenta de persona (activa «Es una persona») y una cuenta con el nombre de esa persona. Si te debe, su saldo es positivo; si le debes, es negativo. No se suma a tu saldo total.",
  },
  {
    q: "¿Para qué sirven los marcadores?",
    a: "Agrupan movimientos de distintas categorías bajo una misma etiqueta, como #universidad o #viaje, para ver cuánto gastaste en total en eso.",
  },
  {
    q: "¿Cómo funcionan los presupuestos?",
    a: "Defines un límite mensual para una categoría o para el gasto total. Tuki compara lo que gastas en el mes con ese límite y te avisa cuando lo superas.",
  },
  {
    q: "¿Cómo corrijo o elimino un movimiento?",
    a: "En Movimientos toca el movimiento: puedes cambiar sus datos o eliminarlo.",
  },
];

export default function HelpScreen() {
  const [open, setOpen] = useState<number | null>(0);

  return (
    <Screen>
      {QUESTIONS.map((item, index) => {
        const expanded = open === index;
        return (
          <PressableScale key={item.q} onPress={() => setOpen(expanded ? null : index)} style={styles.item}>
            <View style={styles.row}>
              <Text variant="medium" style={styles.question}>
                {item.q}
              </Text>
              <Ionicons name={expanded ? "chevron-up" : "chevron-down"} size={18} color={colors.textMuted} />
            </View>
            {expanded ? <Text variant="label" style={styles.answer}>{item.a}</Text> : null}
          </PressableScale>
        );
      })}
    </Screen>
  );
}

const styles = StyleSheet.create({
  item: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: spacing.sm,
  },
  row: { flexDirection: "row", alignItems: "center", gap: spacing.sm },
  question: { flex: 1 },
  answer: { lineHeight: 20 },
});
