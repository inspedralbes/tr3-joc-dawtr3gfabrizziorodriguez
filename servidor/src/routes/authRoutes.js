const express = require('express');
const router = express.Router();
const { register, login } = require('../controllers/authController');

// Definimos que al hacer POST a /register y /login, se ejecute la función del controlador
router.post('/register', register);
router.post('/login', login);

module.exports = router;