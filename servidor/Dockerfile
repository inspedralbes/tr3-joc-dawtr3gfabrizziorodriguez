FROM node:20-alpine

# Crear directori de l'aplicació
WORKDIR /app

# Copiar els fitxers de paquets (package.json i package-lock.json si existeix)
COPY package*.json ./

# Instal·lar les dependències
RUN npm install

# Copiar la resta del codi
COPY . .

# Exposar el port del servidor
EXPOSE 3000

# Executar el servidor Node.js
CMD ["node", "src/index.js"]
