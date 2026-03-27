const { MongoClient } = require('mongodb');
require('dotenv').config();

const uri = process.env.MONGODB_URI;
const client = new MongoClient(uri);
let dbInstance;

const connectDB = async () => {
    try {
        await client.connect();
        dbInstance = client.db('JocBomba');
        return dbInstance;
    } catch (error) {
        console.error(`Error en connectar a MongoDB: ${error.message}`);
        process.exit(1);
    }
};

const getDB = () => {
    if (!dbInstance) {
        throw new Error('La base de dades encara no està connectada');
    }
    return dbInstance;
};

module.exports = { connectDB, getDB };