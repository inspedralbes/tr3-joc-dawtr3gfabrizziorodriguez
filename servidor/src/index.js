require('dotenv').config();
const express = require('express');
const { connectDB } = require('./config/database');
const authRoutes = require('./routes/authRoutes');
const gameRoutes = require('./routes/gameRoutes');
const app = express();
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
        app.listen(PORT, () => {
            console.log(`🚀 Servidor HTTP escoltant al port ${PORT}`);
            console.log(`🌐 Pots provar-ho obrint: http://localhost:${PORT}`);
        });
    } catch (error) {
        console.error("Error a l'iniciar el servidor:", error);
        process.exit(1);
    }
};

startServer();