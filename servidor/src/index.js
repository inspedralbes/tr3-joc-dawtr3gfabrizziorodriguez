require('dotenv').config();
const { connectDB } = require('./config/database');

const startServer = async () => {
    try {
        await connectDB();
        console.log("Servidor iniciat correctament. Esperant connexions...");
    } catch (error) {
        console.error("Error a l'iniciar el servidor:", error);
    }
};

startServer();