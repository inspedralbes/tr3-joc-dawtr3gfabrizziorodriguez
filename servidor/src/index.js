require('dotenv').config();
const express = require('express');
const http = require('http');
const { Server } = require('socket.io'); 
const { connectDB } = require('./config/database');
const authRoutes = require('./routes/authRoutes');
const gameRoutes = require('./routes/gameRoutes');

const app = express();
const server = http.createServer(app);
const io = new Server(server, {
    cors: {
        origin: "*", // Permet que Unity es connecti des de qualsevol lloc
        methods: ["GET", "POST"]
    }
});

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
        io.on('connection', (socket) => {
            console.log(`🟢 [Socket] Nou jugador connectat: ${socket.id}`);

            socket.on('join_room', (data) => {
                socket.join(data.lobbyId);
                console.log(`👤 Jugador ${data.username} s'ha unit a la sala per WebSockets: ${data.lobbyId}`);
                
                // Avisem a tota la resta de jugadors a la sala
                socket.to(data.lobbyId).emit('player_joined', {
                    username: data.username,
                    message: `El jugador ${data.username} s'ha unit a la partida en temps real.`
                });
            });

            // EL HOST FA CLIC A COMENÇAR PARTIDA
            socket.on('start_game', (data) => {
                console.log(`🚀 El host ha iniciat la partida a la sala: ${data.lobbyId}`);
                // Avisem a TOTS els de la sala (inclòs el que ha avisat per si de cas)
                io.in(data.lobbyId).emit('game_started', {
                    message: "A JUGAR!"
                });
            });

            socket.on('disconnect', () => {
                console.log(`🔴 [Socket] Jugador desconnectat: ${socket.id}`);
            });
        });

        server.listen(PORT, () => {
            console.log(`🚀 Servidor HTTP corrent al port ${PORT}`);
            console.log(`⚡ WebSockets activats i escoltant connexions en temps real...`);
        });
    } catch (error) {
        console.error("Error a l'iniciar el servidor:", error);
        process.exit(1);
    }
};

startServer();