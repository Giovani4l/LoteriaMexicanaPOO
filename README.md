# Lotería Mexicana (Multijugador) 🃏

## Descripción
Lotería Mexicana es una recreación digital del juego tradicional, diseñada como una aplicación de escritorio para entornos de red local (LAN) [cite: 3]. El proyecto utiliza un modelo cliente-servidor donde un "Gritón" actúa como host y los jugadores se conectan para competir en tiempo real [cite: 3].

## 👥 Autores
- **Axel Giovani Gonzalez Lopez** 
- **Raúl de Jesús Berlanga Martínez**

## 🏗️ Arquitectura del Sistema
El sistema se basa en una arquitectura de comunicación en tiempo real:
- **Host (El Gritón):** Servidor integrado basado en ASP.NET Core y SignalR [cite: 3]. Gestiona el flujo del juego, la síntesis de voz (TTS) y el estado de la partida [cite: 3].
- **Clientes (Jugadores):** Aplicaciones WinForms que se conectan al host para recibir las cartas cantadas, actualizar el tablero y enviar eventos (marcar fichas) [cite: 3].
- **Protocolo:** Comunicación fluida mediante WebSockets a través de SignalR [cite: 3].


## ✨ Características Clave
- **Multijugador LAN:** Experiencia en tiempo real para múltiples jugadores en la misma red [cite: 3].
- **TTS (Text-to-Speech):** Integración con `System.Speech` para que el "Gritón" cante las cartas de forma audible [cite: 3].
- **Mecánica de Tabla Doble:** Cada tabla de juego contiene exactamente una carta repetida al azar, añadiendo un elemento estratégico [cite: 3].
- **Puntaje Global:** Marcador en tiempo real visible para todos los participantes [cite: 3].
- **Persistencia:** Exportación e importación de tablas personalizadas en formato JSON [cite: 3].
- **Comunicación:** Chat integrado para interacción entre jugadores [cite: 3].
- **Auto-canto con Velocidades:** El "Gritón" ahora cuenta con un sistema de auto-canto con velocidades ajustables (rápido, normal, lento) para automatizar el ritmo del juego.
- **Formatos de Victoria Dinámicos:** Ahora es posible modificar o agregar formatos de victoria (línea, columna, diagonal, cruz, tabla llena) durante una partida en curso.

---

## 🛠️ Stack Tecnológico
- **Lenguaje:** C# / .NET 8 [cite: 3]
- **Interfaz:** Windows Forms [cite: 3]
- **Red / Tiempo Real:** SignalR, ASP.NET Core [cite: 3]
- **Audio:** System.Speech (TTS) [cite: 3]
- **Datos:** JSON (Persistencia de tablas) [cite: 3]

---

## 🎮 Flujo del Juego
1. **Creación:** El Host crea la sala de juego [cite: 3].
2. **Conexión:** Los jugadores se conectan a la dirección del host [cite: 3].
3. **Inicio:** El Host inicia la partida [cite: 3].
4. **Canto:** El Gritón anuncia las cartas mediante voz sintetizada (con velocidad configurable) [cite: 3].
5. **Marcado:** Los jugadores marcan sus fichas [cite: 3].
6. **Victoria:** El sistema valida condiciones de victoria configuradas dinámicamente y anuncia al ganador [cite: 3].
