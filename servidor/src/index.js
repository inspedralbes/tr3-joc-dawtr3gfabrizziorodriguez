require('dotenv').config();
const express = require('express');
const http = require('http');
const { WebSocketServer } = require('ws');
const { connectDB } = require('./config/database');
const authRoutes = require('./routes/authRoutes');
const gameRoutes = require('./routes/gameRoutes');

const app = express();
const server = http.createServer(app);

// Mapa: lobbyId -> { players: [{ws, username, index}], maxPlayers }
const rooms = new Map();

const PORT = process.env.PORT || 3000;

app.use(express.json());
app.use('/api/auth', authRoutes);
app.use('/api/games', gameRoutes);

app.get('/', (req, res) => {
    res.json({ missatge: "🎮 Benvingut a l'API del teu Videojoc multijugador!" });
});

const startServer = async () => {
    try {
        await connectDB();

        const wss = new WebSocketServer({ server });

        wss.on('connection', (ws) => {
            console.log('🟢 [WS] Nou jugador connectat');
            let currentLobby = null;
            let currentUsername = null;
            let currentIndex = null;

            ws.on('message', (rawData) => {
                let msg;
                try { msg = JSON.parse(rawData.toString()); }
                catch (e) { return; }

                // ── JOIN ROOM ──────────────────────────────────────────
                if (msg.type === 'join_room') {
                    const { lobbyId, username, maxPlayers } = msg;
                    currentLobby    = lobbyId;
                    currentUsername = username;

                    if (!rooms.has(lobbyId)) {
                        rooms.set(lobbyId, { players: [], maxPlayers: parseInt(maxPlayers) || 4 });
                    }

                    const room = rooms.get(lobbyId);

                    // Assignar índex (1-based) segons l'ordre d'entrada
                    currentIndex = room.players.length + 1;
                    room.players.push({ ws, username, index: currentIndex });

                    console.log(`👤 [${currentIndex}] ${username} s'ha unit a la sala: ${lobbyId}`);

                    // Dir al client quin índex li ha tocat
                    ws.send(JSON.stringify({ type: 'you_are', index: currentIndex }));

                    // Avisar a la resta
                    room.players.forEach(p => {
                        if (p.ws !== ws && p.ws.readyState === 1) {
                            p.ws.send(JSON.stringify({ type: 'player_joined', username, index: currentIndex }));
                        }
                    });
                }

                // ── START GAME ─────────────────────────────────────────
                if (msg.type === 'start_game') {
                    const { lobbyId } = msg;
                    const room = rooms.get(lobbyId);
                    if (!room) return;

                    const maxPlayers = room.maxPlayers;
                    console.log(`🚀 Partida iniciada a la sala: ${lobbyId} (maxPlayers: ${maxPlayers})`);

                    room.players.forEach(p => {
                        if (p.ws.readyState === 1) {
                            p.ws.send(JSON.stringify({ type: 'game_started', maxPlayers }));
                        }
                    });
                }
            });

            ws.on('close', () => {
                console.log(`🔴 [WS] Jugador desconnectat: ${currentUsername}`);
                if (currentLobby && rooms.has(currentLobby)) {
                    const room = rooms.get(currentLobby);
                    room.players = room.players.filter(p => p.ws !== ws);
                    if (room.players.length === 0) rooms.delete(currentLobby);
                }
            });

            ws.on('error', (err) => console.error('Error WS:', err.message));
        });

        server.listen(PORT, () => {
            console.log(`🚀 Servidor HTTP corrent al port ${PORT}`);
            console.log(`⚡ WebSocket natiu activat i escoltant connexions...`);
        });
    } catch (error) {
        console.error("Error a l'iniciar el servidor:", error);
        process.exit(1);
    }
};

startServer();