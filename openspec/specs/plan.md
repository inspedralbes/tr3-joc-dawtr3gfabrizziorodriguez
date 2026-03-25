# Pla d'Acció: Desenvolupament de l'Autenticació a Unity

## Fase 1: Creació d'Assets UI Toolkit
1. **[Asset]** Crear a la carpeta adient (`Joc_Unity/Assets/UI/` o similar) un nou fitxer anomenat `LoginScreen.uxml`.
2. **[Interfície]** Definir els components visuals (`TextField` d'usuari i contrasenya, dos `Button` de login i registre, i el `Label` de retorn) dins l'estructura UXML via el UI Builder de Unity.
3. **[Estil]** Crear `LoginScreen.uss` a la mateixa carpeta. Configurar regles CSS senzilles per a centrar el contenidor de la UI i estilitzar els botons i text.
4. **[Vincle]** Connectar l'arxiu `.uss` a l'arxiu `.uxml` aplicant-lo com a *Style Sheet* a l'element arrel.

## Fase 2: Preparació de l'Escena
1. **[GameObject]** Afegir un nou GameObject buit a l'escena inicial de Unity (ex. donar-li el nom "AuthUI").
2. **[Component]** Afegir-hi el component natiu `UIDocument`.
3. **[Configuració]** Assignar de l'explorador de projectes el `LoginScreen.uxml` a la propietat *Source Asset* d'aquest component `UIDocument`.

## Fase 3: Desenvolupament de la Lògica de Xarxa i Control
1. **[Script]** Crear l'arxiu `AuthUIManager.cs` a la carpeta `Joc_Unity/Assets/Scripts/Auth/`.
2. **[DTO o Model]** Definir la classe o estructura (amb `[Serializable]`) que emula el contracte JSON del servidor: `{ "username": "...", "password": "..." }`.
3. **[Vincles de UI]** En el cicle `OnEnable()` i `OnDisable()`, inicialitzar les referències als elements de comportament (botons i labels) i a les subscripcions a events de clic respectius (es. `myButton.clicked += MyMethod;`).
4. **[UnityWebRequest]** Escriure els mètodes de registre i logatge implementant les corrutines mitjançant `UnityWebRequest.Post()`, formatant-lo adequadament amb `new UploadHandlerRaw(...)` i incloent les capçaleres de contingut de tipus aplicació JSON per cridar al sistema local Node.js a `http://localhost:3000/api/auth/register` i `login`.
5. **[Feedback Gràfic]** Actualitzar l'etiqueta `status-label` immediatament rebre respostes per avisar del final del procés d'autenticació i indicar si l'operació s'ha resolt i qualsevol missatge rebedor.

## Fase 4: Integració i Prova Automàtica o Manual
1. **[Vinculació Unity]** Afegir el script `AuthUIManager` sobre el mateix element d'Escena ("AuthUI") assegurant que hi ha lligam al `UIDocument`.
2. **[Start Servidor]** Arrencar el projecte de Node.js via terminal usant `npm start` des de la ruta escaient per a posar en marxa els serveis backend localment.
3. **[Execució Client]** Accionar el *Play Mode* a l'editor temporal de jocs Unity; enviar registres nous i entrar per comprovar de manera visual la integració fins que mostrin indicis satisfactoris.
