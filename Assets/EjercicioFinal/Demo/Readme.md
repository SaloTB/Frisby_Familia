# Demo técnica — Trabajo Final (Actividad #4)

Escena: `Assets/EjercicioFinal/Demo/Escenas/Demo Final.unity` (primera escena del Build).

Un pato de goma recorre el agua alrededor de una isla con la estatua de **Calabaza**. El pato es el
personaje animado que ejecuta los tres efectos de la demo.

## Video

> **Pendiente:** insertar aquí el video que muestra los tres efectos funcionando.
>
> `[![Video de la demo](miniatura.png)](https://ENLACE-AL-VIDEO)`

## Jugar (WebGL / descarga)

> **Pendiente:** enlace a la página WebGL o a la build en `.zip`.
>
> `https://ENLACE-A-LA-BUILD`

## Controles

| Tecla | Acción |
|---|---|
| W A S D | Moverse por el agua |
| Espacio | Saltar |
| E | Giro (lanza agua) |
| 1 (mantener y soltar) | Efecto Burbuja |
| 2 | Efecto Teletransporte |
| 3 | Efecto Nova de Calabaza |

Cada número reproduce su efecto al instante; si otro estaba en curso, se corta y empieza el nuevo.
La burbuja crece mientras se mantiene el 1 y se lanza al soltarlo (o sola al llegar al tamaño máximo).
Arriba a la izquierda están los controles y el contador de pescados de las cajas.

## Efectos

Cada efecto está coordinado con una animación del pato, usa al menos un shader propio (HLSL para URP,
en `Shaders/`) y tiene su propio post-procesado (perfiles en `PostProcesado/`, cuyo peso se anima
durante el efecto).

| # | Efecto | Animación del pato | Shaders propios | Post-procesado |
|---|---|---|---|---|
| 1 | **Burbuja** | Infla los cachetes y se echa hacia atrás mientras la burbuja crece en su pico (tiembla cerca del máximo); al soltar la lanza con un culatazo y la burbuja vuela y revienta en gotas | `FX Burbuja` (película iridiscente con fresnel, reflejos y superficie que tiembla), `FX Brillo` (gotas y aire) | Aberración cromática |
| 2 | **Teletransporte arcano** | Gira cada vez más rápido sobre un círculo mágico, se disuelve, reaparece en otro punto y frena | `FX Disolver` (disolución del pato con borde brillante), `FX Circulo Magico` (runas y hexagrama procedurales), `FX Brillo` (motas) | Distorsión de lente |
| 3 | **Nova de Calabaza** | Se infla absorbiendo chispas, se aplasta y suelta una onda de fuego azul por el agua y una columna de llamas mientras salta girando, y vuelve rebotando | `FX Fuego` (llamas azules procedurales en la onda y la columna), `FX Brillo` (chispas) | Viñeta azul + Ajustes de color |

## Jugabilidad extra (no evaluada como efecto)

- **Cajas de pescado** alrededor de la isla: al chocarlas el pato las recoge, suma al contador y
  reaparecen a los 5 s (shader `FX Caja Pescado`).
- **Salto** y **giro** del pato (`PatoHabilidades`).
- **Límite de juego:** el pato no puede salir de un círculo de 42 m alrededor de la isla ni subir a la arena.

## Estructura

```
Assets/EjercicioFinal/Demo/
  Escenas/        Demo Final.unity
  Scripts/        EfectoPato (base), EfectoBurbuja, EfectoTeletransporte,
                  EfectoNovaCalabaza, GestorDemo (teclas 1/2/3 y contador), CajaPescado, FXUtil,
                  PatoJugador, PatoFlotante, PatoHabilidades, CamaraOndas
  Shaders/        FX_*.shader + FXComun.hlsl (efectos), Water Toon, Halo Pato e Isla Estilizada
                  (shader graphs del agua y la isla) con su subgraph y Lighting_Core_G2.hlsl
  Materiales/     efectos, estatua, pescados, agua, pato; Isla/ con los materiales de la isla
  Texturas/       textura del pato, normales del agua, cubemap y la Render Texture de las ondas
  PostProcesado/  PP Global Demo (Bloom) y un perfil por efecto
  Modelos/        isla, pato, disco de agua original y mallas generadas (océano, estatua, pescado)
  Editor/         ConstructorDemo (Herramientas > Demo Final > Construir escena)
```

La escena se reconstruye desde el menú **Herramientas > Demo Final > Construir escena**.
