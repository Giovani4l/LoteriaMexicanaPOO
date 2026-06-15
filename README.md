# Lotería Mexicana (Multijugador) 🃏

## Descripción
Lotería Mexicana es una recreación digital del juego tradicional, diseñada como una aplicación de escritorio para entornos de red local (LAN).
El proyecto utiliza un modelo cliente-servidor donde un "Gritón" actúa como host y los jugadores se conectan para competir en tiempo real.

## 👥 Autores
- **Axel Giovani Gonzalez Lopez** 
- **Raúl de Jesús Berlanga Martínez**

## 🏗️ Arquitectura del Sistema
El sistema se basa en una arquitectura de comunicación en tiempo real:
- **Host (El Gritón):** Servidor integrado basado en ASP.NET Core y SignalR . Gestiona el flujo del juego, la síntesis de voz (TTS) y el estado de la partida.
- **Clientes (Jugadores):** Aplicaciones WinForms que se conectan al host para recibir las cartas cantadas, actualizar el tablero y enviar eventos (marcar fichas) 
- **Protocolo:** Comunicación fluida mediante WebSockets a través de SignalR.


## ✨ Características Clave
- **Multijugador LAN:** Experiencia en tiempo real para múltiples jugadores en la misma red.
- **TTS (Text-to-Speech):** Integración con `System.Speech` para que el "Gritón" cante las cartas de forma audible.
- **Mecánica de Tabla Doble:** Cada tabla de juego contiene exactamente una carta repetida al azar, añadiendo un elemento estratégico.
- **Puntaje Global:** Marcador en tiempo real visible para todos los participantes.
- **Persistencia:** Exportación e importación de tablas personalizadas en formato JSON.
- **Comunicación:** Chat integrado para interacción entre jugadores.
- **Auto-canto con Velocidades:** El "Gritón" ahora cuenta con un sistema de auto-canto con velocidades ajustables (rápido, normal, lento) para automatizar el ritmo del juego.
- **Formatos de Victoria Dinámicos:** Ahora es posible modificar o agregar formatos de victoria (línea, columna, diagonal, cruz, tabla llena) durante una partida en curso.

---

## 🛠️ Stack Tecnológico
- **Lenguaje:** C# / .NET 8
- **Interfaz:** Windows Forms
- **Red / Tiempo Real:** SignalR, ASP.NET Core 
- **Audio:** System.Speech (TTS) 
- **Datos:** JSON (Persistencia de tablas)

---

## 🎮 Flujo del Juego
1. **Creación:** El Host crea la sala de juego.
2. **Conexión:** Los jugadores se conectan a la dirección del host.
3. **Inicio:** El Host inicia la partida.
4. **Canto:** El Gritón anuncia las cartas mediante voz sintetizada (con velocidad configurable).
5. **Marcado:** Los jugadores marcan sus fichas [cite: 3].
6. **Victoria:** El sistema valida condiciones de victoria configuradas dinámicamente y anuncia al ganador [cite: 3].
