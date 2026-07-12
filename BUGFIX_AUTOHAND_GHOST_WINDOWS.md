# Bug Fix: AutoHand "Ghost Windows" en Unity 6

## Problema

Al instalar el asset **AutoHand v4.0.0** en proyectos de Unity 6, aparecen ventanas de editor "fantasmas" que:
- Se abren automáticamente al importar el asset o al recargar el dominio (recompilación, entrar en Play mode)
- No se pueden cerrar normalmente
- Persisten incluso después de reiniciar el layout de Unity
- Reaparecen en cada recarga del dominio del editor

## Causa Raíz

AutoHand v4.0.0 está declarado para **Unity 2020.3** (`package.json:4-5`) pero se ejecuta en Unity 6. El problema proviene de dos scripts de editor que abren ventanas automáticamente durante la inicialización del editor:

### 1. `AutoHandUpdateDataWizard.cs` (línea 41)
```csharp
[UnityEditor.InitializeOnLoadMethod]
public static void CheckSceneForOldPoses() {
    // ...
    window = GetWindow<AutoHandUpdateDataWizard>("Update Pose Data");
    // ...
}
```

- El atributo `[InitializeOnLoadMethod]` hace que `CheckSceneForOldPoses()` se ejecute automáticamente en cada recarga del dominio del editor
- Llama a `GetWindow<>()` directamente durante la inicialización, lo cual es poco fiable en Unity 2022/6000
- Además, `FindPrefabPoses()` (líneas 253-265) llama a `GetComponentsInChildren<>()` sin verificar si el prefab es null, lo que puede lanzar excepciones durante la inicialización

### 2. `AutoHandSetupWizard.cs` (líneas 30-45)
```csharp
static AutoHandSetupWizard() {
    EditorApplication.update += Start;
}

static void Start() {
    SetRequiredSettings();

    if(ShowSetupWindow()) {
        OpenWindow();
        Application.OpenURL("https://earnest-robot.gitbook.io/auto-hand-docs/");
        assetPath = Application.dataPath;
    }

    EditorApplication.update -= Start;
}
```

- El constructor estático registra `Start()` en `EditorApplication.update`
- `Start()` abre la ventana del asistente automáticamente y abre una pestaña del navegador
- Esto ocurre en cada recarga del dominio

**Por qué esto crea ventanas fantasmas en Unity 6:**
- Mostrar `EditorWindow`s mediante `GetWindow` durante `[InitializeOnLoad]`/`InitializeOnLoadMethod` (antes de que el sistema de docking/layout se restaure) es poco fiable en Unity 2022/6000
- Crea ventanas huérfanas/desacopladas que no se cierran correctamente
- Como se ejecutan en cada recarga del dominio, cerrarlas o resetear el layout no es persistente — la siguiente recarga las recrea

## Solución Aplicada

Desactivar la auto-apertura de ambos asistentes, conservando sus menús manuales y la configuración silenciosa de capas requeridas.

### 1. `AutoHandSetupWizard.cs` — Eliminar auto-apertura

**Antes (líneas 35-45):**
```csharp
static void Start() {
    SetRequiredSettings();

    if(ShowSetupWindow()) {
        OpenWindow();
        Application.OpenURL("https://earnest-robot.gitbook.io/auto-hand-docs/");
        assetPath = Application.dataPath;
    }

    EditorApplication.update -= Start;
}
```

**Después:**
```csharp
static void Start() {
    SetRequiredSettings();
    EditorApplication.update -= Start;
}
```

- Se eliminó el bloque que abría la ventana y el navegador automáticamente
- Se conservó `SetRequiredSettings()` para que siga creando silenciosamente las 4 capas requeridas (`Grabbing`, `Grabbable`, `Hand`, `HandPlayer`) y sus colisiones ignoradas
- El menú manual `Window > Autohand > Setup Window` sigue funcionando

### 2. `AutoHandUpdateDataWizard.cs` — Desactivar auto-spawn y agregar null-guard

**Cambio 1 — Quitar atributo (línea 41):**
```csharp
// Antes:
[UnityEditor.InitializeOnLoadMethod]
public static void CheckSceneForOldPoses() {

// Después:
public static void CheckSceneForOldPoses() {
```

- Al quitar `[InitializeOnLoadMethod]`, el método ya no se ejecuta automáticamente en la carga del editor
- El menú manual `Window > Autohand > Pose Data Updater` sigue funcionando

**Cambio 2 — Null-guard en FindPrefabPoses (líneas 256-258):**
```csharp
// Antes:
foreach(var guid in guids) {
    var assetObject = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
    var poses = assetObject.GetComponentsInChildren<HandPoseDataContainer>(true);

// Después:
foreach(var guid in guids) {
    var assetObject = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
    if(assetObject == null)
        continue;
    var poses = assetObject.GetComponentsInChildren<HandPoseDataContainer>(true);
```

- Se agregó una verificación de null para evitar `NullReferenceException` si un prefab falla al cargar

### 3. `EditorHandEditor.cs` — Sin cambios

Este script solo abre la herramienta de poses al seleccionar un `EditorHand`, no es una ventana fantasma de instalación, por lo que no requiere cambios.

## Cómo Aplicar el Fix en Otros Proyectos

### Opción A: Copiar los archivos editados

Copia estos dos archivos desde este proyecto hacia `Assets/AutoHand/Scripts/Editor/` del proyecto destino:
1. `AutoHandSetupWizard.cs`
2. `AutoHandUpdateDataWizard.cs`

Luego reinicia Unity una vez para eliminar cualquier ventana fantasma ya atascada.

### Opción B: Aplicar los cambios manualmente

Si el proyecto tiene una versión diferente de AutoHand, aplica los cambios manualmente:

**En `AutoHandSetupWizard.cs`:**
- En el método `Start()`, elimina el bloque `if(ShowSetupWindow()) { OpenWindow(); Application.OpenURL(...); }`
- Conserva `SetRequiredSettings();` y `EditorApplication.update -= Start;`

**En `AutoHandUpdateDataWizard.cs`:**
- Quita el atributo `[UnityEditor.InitializeOnLoadMethod]` encima de `CheckSceneForOldPoses()`
- En `FindPrefabPoses()`, agrega `if(assetObject == null) continue;` después de cargar el prefab

## Limpieza de Ventanas Atascadas

Después de aplicar los cambios:
1. Deja que Unity recompile (recarga del dominio)
2. Cierra cualquier ventana de AutoHand que siga flotando
3. Si una ventana se niega a cerrarse, reinicia Unity una vez — como la auto-apertura está desactivada, no volverá
4. Si una instancia rota fue serializada en el layout: `Window > Layouts > Default Layout` (o Revert Factory Settings)

## Verificación

- ✅ Recompilar/reiniciar Unity → ninguna ventana de AutoHand se abre automáticamente
- ✅ No se abre ninguna pestaña del navegador
- ✅ La consola no muestra `NullReferenceException` de los asistentes
- ✅ `Window > Autohand > Setup Window` y `Window > Autohand > Pose Data Updater` siguen funcionando a demanda
- ✅ Las capas requeridas (Grabbing, Grabbable, Hand, HandPlayer) siguen presentes (configuración silenciosa preservada)

## Notas Importantes

- Solo se modificaron scripts de editor; el comportamiento en runtime y las builds no se ven afectados
- Re-importar o actualizar AutoHand desde el Asset Store sobrescribirá estos archivos — reaplica los cambios después
- Este fix es específico para AutoHand v4.0.0; versiones futuras pueden tener implementaciones diferentes
