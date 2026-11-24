# Kakuro

Juego de Kakuro para Android desarrollado con Unity. Resuelve tableros
de distintas dificultades, utiliza pistas cuando las necesites y
comprueba tus mejores tiempos.

## Características

- Tres dificultades: fácil (7 × 7), medio (9 × 9) y difícil (12 × 12).
- Generación de puzles con solución única.
- Progresión de dificultad según las victorias en cada modo.
- Pistas, deshacer y reinicio del tablero.
- Opción «Me rindo» para descubrir la solución.
- Guardado de partida y pausa.
- Ranking local con los 8 mejores tiempos de cada dificultad.
- Estadísticas de partidas ganadas y no resueltas.
- Introducción y borrado de números mediante voz en Android.
- Panel de reglas y ayuda con combinaciones.

## Cómo jugar

Completa las casillas blancas con números del 1 al 9. Los números
de cada grupo deben sumar la cantidad indicada en su pista, sin
repetir cifras dentro del mismo grupo.

Puedes seleccionar una casilla y elegir un número en el panel,
o activar el control por voz y decir el número que quieras colocar.

## Abrir el proyecto

1. Descarga o clona este repositorio.
2. Añade la carpeta del proyecto a Unity Hub.
3. Ábrelo con **Unity 2022.3.29f1**.
4. Abre la escena `Assets/Scenes/MainMenu.unity`.
5. Pulsa **Play** para probarlo.

Para compilarlo para Android necesitarás tener instalado el módulo
Android Build Support de Unity. La función de voz requiere conceder
permiso de micrófono en el dispositivo.

## Tecnologías

- Unity y C#
- TextMesh Pro
- Vosk para reconocimiento de voz

## Autor

Camilo Puertas Sanchez

## Recursos de terceros
