const bcrypt = require('bcrypt');
const { getDB } = require('../config/database'); // Importem la connexió nativa que ja tenies

/**
 * Registre d'un nou usuari
 */
const register = async (req, res) => {
    try {
        const { username, password } = req.body;

        // 1. Validació de camps buits
        if (!username || !password) {
            return res.status(400).json({ error: "L'usuari i la contrasenya són obligatoris" });
        }

        const db = getDB();
        const usersCollection = db.collection('usuaris');

        // 2. Comprovar si l'usuari ja existeix
        const userExists = await usersCollection.findOne({ username });
        if (userExists) {
            return res.status(400).json({ error: "Aquest usuari ja existeix al sistema" });
        }

        // 3. Encriptar la contrasenya
        const salt = await bcrypt.genSalt(10);
        const hashedPassword = await bcrypt.hash(password, salt);

        // 4. Crear l'objecte i guardar-lo a MongoDB
        const newUser = {
            username,
            password: hashedPassword,
            createdAt: new Date()
        };

        const result = await usersCollection.insertOne(newUser);

        res.status(201).json({ 
            missatge: "Usuari registrat correctament", 
            userId: result.insertedId 
        });

    } catch (error) {
        console.error("Error al registre:", error);
        res.status(500).json({ error: "Error intern del servidor" });
    }
};

/**
 * Inici de sessió (Login)
 */
const login = async (req, res) => {
    try {
        const { username, password } = req.body;

        // 1. Validació de camps buits
        if (!username || !password) {
            return res.status(400).json({ error: "L'usuari i la contrasenya són obligatoris" });
        }

        const db = getDB();
        const usersCollection = db.collection('usuaris');

        // 2. Buscar l'usuari per nom
        const user = await usersCollection.findOne({ username });
        if (!user) {
            return res.status(401).json({ error: "Credencials incorrectes" });
        }

        // 3. Comparar la contrasenya enviada amb la de la BD (encriptada)
        const isMatch = await bcrypt.compare(password, user.password);
        if (!isMatch) {
            return res.status(401).json({ error: "Credencials incorrectes" });
        }

        // 4. Resposta d'èxit
        res.status(200).json({ 
            missatge: "Login correcte! Benvingut de nou.", 
            userId: user._id,
            username: user.username
        });

    } catch (error) {
        console.error("❌ Error al login:", error);
        res.status(500).json({ error: "Error intern del servidor" });
    }
};

// Exportem les dues funcions per usar-les a les rutes
module.exports = { 
    register, 
    login 
};