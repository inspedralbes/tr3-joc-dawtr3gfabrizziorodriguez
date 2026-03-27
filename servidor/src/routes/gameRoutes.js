const express = require('express');
const router = express.Router();
const { createLobby, getAllLobbies, joinLobby } = require('../controllers/gameController');

router.post('/create', createLobby);
router.get('/list', getAllLobbies);
router.post('/join', joinLobby);

module.exports = router;