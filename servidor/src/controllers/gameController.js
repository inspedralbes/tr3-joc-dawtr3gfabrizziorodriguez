const { getDB } = require('../config/database');
const { ObjectId } = require('mongodb');

// 1. CREAR PARTIDA 
const createLobby = async (req, res) => {
    try {
        const { lobbyName, maxPlayers, createdBy } = req.body;
        if (!lobbyName || !maxPlayers || !createdBy) {
            return res.status(400).json({ error: "Falten dades per crear la partida" });
        }

        const db = getDB();
        const lobbiesCollection = db.collection('partides');

        // Generar un codi de 6 lletres o números
        const joinCode = Math.random().toString(36).substring(2, 8).toUpperCase();

        const newLobby = {
            lobbyName,
            maxPlayers: parseInt(maxPlayers),
            joinCode: joinCode,
            currentPlayers: 1,
            host: createdBy,
            players: [createdBy],
            status: 'waiting',
            createdAt: new Date()
        };

        const result = await lobbiesCollection.insertOne(newLobby);
        res.status(201).json({ missatge: "Partida creada", lobbyId: result.insertedId, joinCode: joinCode });
    } catch (error) {
        res.status(500).json({ error: "Error intern" });
    }
};

// 2. LLISTAR PARTIDES
const getAllLobbies = async (req, res) => {
    try {
        const db = getDB();
        // Busquem només les que estan en espera de jugadors
        const lobbies = await db.collection('partides').find({ status: 'waiting' }).toArray();
        res.status(200).json(lobbies);
    } catch (error) {
        res.status(500).json({ error: "Error al llistar partides" });
    }
};

// 3. UNIR-SE A PARTIDA 
const joinLobby = async (req, res) => {
    try {
        const { lobbyId, username } = req.body;
        const db = getDB();
        const lobbiesCollection = db.collection('partides');

        // Busquem la partida
        const lobby = await lobbiesCollection.findOne({ _id: new ObjectId(lobbyId) });

        if (!lobby) return res.status(404).json({ error: "Partida no trobada" });
        if (lobby.currentPlayers >= lobby.maxPlayers) return res.status(400).json({ error: "La partida està plena" });
        if (lobby.players.includes(username)) return res.status(400).json({ error: "Ja estàs dins d'aquesta partida" });

        // Actualitzem la partida afegint el jugador i incrementant el contador
        await lobbiesCollection.updateOne(
            { _id: new ObjectId(lobbyId) },
            {
                $push: { players: username },
                $inc: { currentPlayers: 1 }
            }
        );

        res.status(200).json({ missatge: "T'has unit a la partida!", lobbyId });
    } catch (error) {
        res.status(500).json({ error: "Error al unir-se" });
    }
};

module.exports = { createLobby, getAllLobbies, joinLobby };