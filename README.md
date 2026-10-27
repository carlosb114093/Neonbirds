# Plumaje Neón

Prototipo de juego de pelea 2D en Unity con aves del Valle del Cauca. Cada golpe dado o recibido llena el **tanque neón**; con el tanque lleno, el ave se transforma en un androide neón con ataques especiales.

- **Autor:** Emanuel Reyes
- **Programa:** Desarrollo de Videojuegos y Entornos Interactivos — SENA, ficha 3466174
- **Motor:** Unity 6000.0.40f1 · Input System

## Cómo abrirlo

1. En Unity Hub: **Add → Add project from disk** y seleccionar esta carpeta.
2. Abrir con Unity **6000.0.40f1**. Si Unity pregunta por activar el nuevo Input System, responder **Yes**.
3. Abrir cualquier escena y presionar **Play**.

El juego se instancia automáticamente al cargar la escena (`Game.cs`): escenario, aves, sonido e interfaz se generan por código.

## Controles

| Acción | Jugador 1 | Jugador 2 |
|---|---|---|
| Moverse | A / D | ← / → |
| Saltar | W | ↑ |
| Bloquear (mantener) | S | ↓ |
| Pico (golpe rápido) | F | K o Num 1 |
| Garra (patada; en el aire, picada) | G | L o Num 2 |
| Transformarse (tanque lleno) | R | P o Num 0 |
| Especial 1 (solo en neón) | H | Ñ o Num 3 |
| Especial 2 (solo en neón) | S + H | ↓ + Ñ |
| Pausa | Esc | Esc |

En los menús: Enter, espacio o clic para elegir; Esc para volver.

## Reglas

- Con el tanque lleno el ave se transforma durante 10 segundos: pega un 30 % más fuerte, se mueve más rápido y desbloquea dos especiales.
- **Barranquero:** *Disco Péndulo* (proyectil) y *Túnel del Barranco* (se entierra y sale bajo el rival).
- **Tángara multicolor:** *Rayo Prisma* (rayo que cruza la pantalla) y *Bandada Mixta* (tres aves neón que caen en picada).
- Gana quien se lleve 2 rondas de 60 segundos.
- Guacamaya cariseca, Bichofué gritón, Azulejo común, Pava caucana, Lora común y Amazilia colirrufa aparecen como **Coming soon**.

## Estructura

| Archivo | Qué hace |
|---|---|
| `Data.cs` | Aves (colores, velocidad, salto, poder) y ataques (daño, frames, cajas de golpe). |
| `Fighter.cs` | Lógica de un luchador: movimiento, ataques, bloqueo, transformación, túnel. |
| `Game.cs` | Flujo (portada, selección, rondas), golpes, proyectiles, CPU, cámara e interfaz. |
| `BirdRig.cs` | Dibuja y anima un ave por piezas en tres modos: natural, neón y silueta. |
| `Gfx.cs` | Texturas por código y el escenario (atardecer en Cali, Farallones, Tres Cruces). |
| `Fx.cs` | Partículas: plumas, chispas, anillos y tierra. |
| `Sfx.cs` | Sonidos sintetizados, sin archivos de audio. |
| `Keys.cs` | Teclado, compatible con ambos sistemas de input. |

Las medidas están en "px" de un lienzo lógico de 960×540 con el eje y hacia abajo; `Gfx.W(x, y)` las convierte a coordenadas de Unity (100 px = 1 unidad).
