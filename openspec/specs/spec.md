# Especificació Tècnica: Integració UI Toolkit amb Node.js

## Components de la Interfície (UI Toolkit)

### 1. `LoginScreen.uxml`
Definició de l'estructura de la finestra d'autenticació:
- Un contenidor arrel principal (`VisualElement`) per centrar el contingut a la pantalla.
- Camp de text per a l'usuari (`TextField` amb nom `username-input`).
- Camp de text per a la constrasenya (`TextField` amb nom `password-input`, configurat amb la propietat *isPasswordField* = true).
- Botó de connexió (`Button` amb nom `login-button`, text: "Login").
- Botó de registre (`Button` amb nom `register-button`, text: "Registrar").
- Etiqueta de missatges d'estat (`Label` amb nom `status-label`), que servirà per notificar l'usuari d'èxits (ex. "Usuari registrat") o errors (ex. "Credencials invàlides").

### 2. `LoginScreen.uss`
Regles d'estil mínimes per aconseguir una aparença neta i polida:
- Visualització `Flexbox` que centri tot el formulari a la pantalla (`align-items: center; justify-content: center;`).
- Marge i farciment intern (`margin` i `padding`) per proporcionar un espai adequat i visualment separat entre els camps i els botons.
- Colors interactius (efectes `hover` i `active`) per als botons, i colors de text variants segons si és error (`color: red`) vs èxit (`color: green`) a l'etiqueta d'estat.

## Script de Control (`AuthUIManager.cs`)

Codi responsable d'unir el document UXML i la lògica de xarxa HTTP cap al servidor de Node.js:

### Propietats:
- Referència a la URL base Node.js: `private string backendUrl = "http://localhost:3000/api/auth";`
- Referència als elements visuals definits a dalt, extrets via `rootVisualElement.Q<T>("nom")`.

### Mètodes i Funcionalitat Principal:
- **`OnEnable()`**: Es recuperen les instàncies dels camps de text, botons i el label. Es subscriuen les accions de clics als botons (`loginButton.clicked += ...`).
- **`OnDisable()`**: Es desubscriuen les accions dels botons per evitar fugues de memòria.
- **`HandleLoginClick()`** / **`HandleRegisterClick()`**: Controladors on es llegeixen els valors dels `TextField` i s'inicia la respectiva corrutina. Validació preventiva de camps buits per estalviar enviaments al servidor.
- **`SendAuthRequest(string endpoint, string username, string password)`**: Corrutina central, on:
  1. Es crea una instància de classe per emmagatzemar dades ex. `AuthData(username, password)`.
  2. S'utilitza `JsonUtility.ToJson(data)` per obtenir l'string JSON esperat pel servidor.
  3. L'enviament de JSON com a POST requereix un procediment especial a Unity: crear manualment el request (`new UnityWebRequest(url, "POST")`), posar el JSON a un `UploadHandlerRaw` i utilitzar `SetRequestHeader("Content-Type", "application/json")`.
  4. Gestionar el retorn: Comprovar errors (`webRequest.result`), notificar al label d'estat (ex. 200 OK vs 401 Unauthorized), i interpretar respostes JSON si fos necessari.

### Estructures de Dades de Suport:
```csharp
[Serializable]
public class AuthData {
    public string username;
    public string password;
}
```
