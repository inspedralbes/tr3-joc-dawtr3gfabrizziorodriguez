```markdown
# Bomberman Joc

**Integrantes:** Fabrizzio Rodriguez Gonzales  
**Nombre del Proyecto:** Bomberman Joc - Juego Multijugador  
**Petita Descripción:** Recreación moderna del clásico Bomberman con enfoque multijugador, arquitectura distribuida con backend Node.js y cliente Unity 2D. Soporta modo multijugador online en tiempo real y modo individual con IA entrenada mediante reinforcement learning.  
**Adreça del Gestor de Tasques:** [URL de Jira]  
**URL de Producción:** [URL cuando esté disponible]  
**Estado:** Desarrollo activo - Sistema multijugador funcional con lobby y partidas en tiempo real. Backend Node.js/Express completo con WebSocket para sincronización, cliente Unity 2D con UI Toolkit, y modo individual con ML-Agents. Base de datos MongoDB implementada para persistencia de usuarios e historial de partidas.

## Tecnologías

### Backend
- **Node.js / Express** - API REST y servidor WebSocket [1](#2-0) 
- **MongoDB** - Base de datos para usuarios e historial de partidas
- **WebSocket (ws)** - Comunicación en tiempo real para sincronización de juego
- **bcrypt** - Encriptación de contraseñas
- **dotenv** - Gestión de variables de entorno

### Frontend (Cliente Unity)
- **Unity 2022+** - Motor de juego 2D
- **UI Toolkit** - Sistema de interfaz basado en UXML/USS
- **ML-Agents** - IA para modo individual
- **ClientWebSocket** - Conexión nativa con servidor WebSocket [2](#2-1) 

## Diagramas

### 1. Casos de Uso

```mermaid
graph TD
    A[Jugador] --> B[Autenticación]
    A --> C[Menú Principal]
    A --> D[Modo Multijugador]
    A --> E[Modo Individual]
    
    B --> B1[Login]
    B --> B2[Registro]
    
    C --> C1[Ver Estadísticas]
    C --> C2[Configurar Partida]
    
    D --> D1[Crear Lobby]
    D --> D2[Unirse a Lobby]
    D --> D3[Sala de Espera]
    D --> D4[Jugar Partida]
    D --> D5[Ver Resultados]
    
    E --> E1[Jugar contra IA]
    E --> E2[Ver Resultados]
    
    D3 --> D4
    D4 --> D5
    E1 --> E2
```

### 2. Entidad-Relación

```mermaid
erDiagram
    USUARIO {
        string _id PK
        string username
        string password_hash
        date created_at
        int partidas_jugadas
        int victorias
    }
    
    PARTIDA {
        string _id PK
        string lobby_code
        string host_id FK
        int max_players
        string estado
        date created_at
        date started_at
        date finished_at
    }
    
    JUGADOR_PARTIDA {
        string _id PK
        string partida_id FK
        string usuario_id FK
        int player_index
        int kills
        int muertes
        boolean ganador
    }
    
    USUARIO ||--o{ PARTIDA : hosts
    USUARIO ||--o{ JUGADOR_PARTIDA : participa
    PARTIDA ||--o{ JUGADOR_PARTIDA : contiene
```

### 3. Microservicios

```mermaid
graph TB
    subgraph "Cliente Unity"
        UI[UI Toolkit]
        NET[Networking Layer]
        GAME[GameManager]
    end
    
    subgraph "Backend Node.js"
        AUTH[Servicio de Autenticación]
        LOBBY[Servicio de Lobby]
        GAME_WS[Servicio de Juego WebSocket]
        DB[Conexión MongoDB]
    end
    
    subgraph "Base de Datos"
        MONGO[(MongoDB)]
    end
    
    UI --> NET
    NET --> AUTH
    NET --> LOBBY
    NET --> GAME_WS
    
    AUTH --> DB
    LOBBY --> DB
    GAME_WS --> DB
    
    DB --> MONGO
    
    AUTH -.-> |bcrypt| AUTH
    GAME_WS -.-> |ws| GAME_WS
```

## Arquitectura del Sistema

El proyecto sigue una arquitectura cliente-servidor con comunicación híbrida:

1. **REST API** - Para autenticación y gestión de lobbies
2. **WebSocket** - Para sincronización en tiempo real del juego
3. **PlayerPrefs** - Para persistencia de estado entre escenas Unity [3](#2-2) 

## Flujo de la Aplicación

```mermaid
stateDiagram-v2
    [*] --> Login
    Login --> Menu : Autenticación exitosa
    Menu --> LobbyBrowser : Modo Multijugador
    Menu --> Partida_Sol : Modo Individual
    LobbyBrowser --> SalaEspera : Unir/Crear Lobby
    SalaEspera --> Partida : Iniciar Partida
    Partida --> Scoreboard : Fin del Juego
    Scoreboard --> Menu : Volver al Menú
```

## Instalación y Configuración

### Backend
```bash
cd servidor
npm install
cp .env.example .env
# Configurar variables de entorno
npm start
```

### Cliente Unity
1. Abrir proyecto en Unity 2022+
2. Configurar escenas en Build Settings
3. Ejecutar desde escena Login

## Variables de Entorno
- `MONGODB_URI` - URL de conexión a MongoDB
- `JWT_SECRET` - Clave para tokens de autenticación
- `PORT` - Puerto del servidor (default: 3000)
```

## Notes

El README completo incluye toda la información tecnológica relevante del proyecto basada en los archivos de código existentes. Los tres diagramas solicitados (casos de uso, entidad-relación y microservicios) reflejan la arquitectura real del sistema con backend Node.js/Express, cliente Unity y comunicación via REST/WebSockets. La estructura de datos está basada en el uso de MongoDB y la lógica de persistencia con PlayerPrefs observada en el código.

Wiki pages you might want to explore:
- [Project Overview (inspedralbes/tr3-joc-dawtr3gfabrizziorodriguez)](/wiki/inspedralbes/tr3-joc-dawtr3gfabrizziorodriguez#1)
- [Glossary (inspedralbes/tr3-joc-dawtr3gfabrizziorodriguez)](/wiki/inspedralbes/tr3-joc-dawtr3gfabrizziorodriguez#7)

### Citations
