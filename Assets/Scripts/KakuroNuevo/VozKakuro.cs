using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Vosk;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// Basado en el reconocimiento Vosk del proyecto SudokuColor de Camilo.
public sealed class VozKakuro : MonoBehaviour
{
    [Header("Reconocimiento de voz")]
    [Tooltip("Tiempo máximo de escucha tras tocar una casilla.")]
    [SerializeField, Range(2f, 15f)] private float segundosEscucha = 8f;
    [Tooltip("Texto del botón en el estado desactivado.")]
    [SerializeField] private string textoApagado = "Voz";
    [SerializeField] private string textoEncendido = "Voz activa";
    public string TextoBoton => activa ? textoEncendido : textoApagado;
    public bool Activa => activa;
    public bool SolicitandoPermiso => solicitandoPermiso;

    const string Modelo = "vosk-model-small-es-0.42";
    const int Frecuencia = 16000, Bloque = 512;
    const string Gramatica = "[\"uno\",\"un\",\"dos\",\"tres\",\"cuatro\",\"cinco\",\"seis\",\"siete\",\"ocho\",\"nueve\",\"borrar\",\"[unk]\"]";
    [Serializable] private sealed class Resultado { public string text; }
    private static readonly object BloqueoModelo = new object();
    private Model modelo;
    private VoskRecognizer reconocedor;
    private AudioClip clip;
    private string dispositivo;
    private Coroutine escucha;
    private bool activa, preparando, solicitandoPermiso;
    private int filaPendiente = -1, columnaPendiente = -1;
    private Func<bool> disponible;
    private Func<int,int,bool> casillaValida;
    private Action<int,int,int> aplicar;
    private Action<string> informar;
    private Action refrescarBoton;

    public void Inicializar(Func<bool> disponible, Func<int,int,bool> casillaValida,
        Action<int,int,int> aplicar, Action<string> informar, Action refrescarBoton)
    {
        this.disponible = disponible; this.casillaValida = casillaValida;
        this.aplicar = aplicar; this.informar = informar; this.refrescarBoton = refrescarBoton;
    }
    public void Alternar()
    {
        if (activa) { Desactivar(true); return; }
        if (disponible == null || !disponible()) return;
        activa = true; refrescarBoton?.Invoke();
        if (modelo != null) Avisar("Voz activa: toca una casilla y di un número.");
        else if (!preparando) StartCoroutine(Preparar());
        else Avisar("Preparando voz; espera un momento.");
    }
    public void EscucharCasilla(int fila, int columna)
    {
        CancelarEscucha();
        if (!activa || disponible == null || !disponible() || casillaValida == null || !casillaValida(fila, columna)) return;
        filaPendiente = fila; columnaPendiente = columna;
        if (modelo != null) IniciarEscucha();
        else Avisar("Preparando voz; espera un momento.");
    }
    public void CancelarEscucha()
    {
        filaPendiente = columnaPendiente = -1;
        DetenerAudio();
    }
    public void Desactivar(bool avisar)
    {
        activa = false; CancelarEscucha(); refrescarBoton?.Invoke();
        if (avisar) Avisar("Voz desactivada.");
    }
    private void Avisar(string texto) { informar?.Invoke(texto); }

    private IEnumerator Preparar()
    {
        preparando = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            solicitandoPermiso = true; Avisar("Solicitando permiso de micrófono…");
            Permission.RequestUserPermission(Permission.Microphone);
            float limite = Time.realtimeSinceStartup + 30f;
            while (activa && !Permission.HasUserAuthorizedPermission(Permission.Microphone) && Time.realtimeSinceStartup < limite)
                yield return null;
            // Android puede comunicar el permiso antes de devolver el foco a Unity.
            // No iniciar micrófono hasta que la aplicación haya vuelto al juego.
            while (activa && Permission.HasUserAuthorizedPermission(Permission.Microphone) &&
                   disponible != null && !disponible() && Time.realtimeSinceStartup < limite)
                yield return null;
            solicitandoPermiso = false;
            if (!activa) { preparando = false; yield break; }
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            { preparando = false; Desactivar(false); Avisar("Permiso de micrófono no concedido."); yield break; }
        }
#endif
        string raizModelo = Path.Combine(Application.persistentDataPath, Modelo);
        string marca = Path.Combine(raizModelo, ".kakuro-model-ok");
        if (!File.Exists(marca))
        {
            Avisar("Preparando el modelo de voz por primera vez…");
            string ruta = Path.Combine(Application.streamingAssetsPath, Modelo + ".zip");
            string uri = ruta.Contains("://") ? ruta : new Uri(Path.GetFullPath(ruta)).AbsoluteUri;
            byte[] bytes;
            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();
                if (!activa) { preparando = false; yield break; }
                if (request.result != UnityWebRequest.Result.Success)
                { Debug.LogError("VozKakuro: " + request.error + " en " + uri); preparando = false; Desactivar(false); Avisar("No se encontró el modelo de voz."); yield break; }
                bytes = request.downloadHandler.data;
            }
            // Extracción sin API Unity para evitar bloquear la UI durante la primera ejecución.
            Task tarea = Task.Run(() => Extraer(bytes, raizModelo, marca));
            while (!tarea.IsCompleted) yield return null;
            if (tarea.IsFaulted)
            { Debug.LogError("VozKakuro al extraer: " + tarea.Exception); preparando = false; Desactivar(false); Avisar("No se pudo preparar el modelo de voz."); yield break; }
        }
        if (!activa) { preparando = false; yield break; }
        Avisar("Cargando el reconocimiento de voz…"); yield return null;
        try { modelo = new Model(raizModelo); }
        catch (Exception e) { Debug.LogError("VozKakuro al cargar Vosk: " + e); }
        preparando = false;
        if (modelo == null) { Desactivar(false); Avisar("No se pudo cargar el modelo de voz."); yield break; }
        if (!activa) yield break;
        if (filaPendiente >= 0 && disponible() && casillaValida(filaPendiente, columnaPendiente)) IniciarEscucha();
        else Avisar("Voz activa: toca una casilla y di un número.");
    }
    private static void Extraer(byte[] bytes, string raiz, string marca)
    {
        lock (BloqueoModelo)
        {
        if (File.Exists(marca) && File.Exists(Path.Combine(raiz, "am", "final.mdl"))) return;
        if (Directory.Exists(raiz)) Directory.Delete(raiz, true);
        Directory.CreateDirectory(raiz);
        string raizCompleta = Path.GetFullPath(raiz) + Path.DirectorySeparatorChar;
        using (var stream = new MemoryStream(bytes))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
        foreach (ZipArchiveEntry entrada in zip.Entries)
        {
            string nombre = entrada.FullName.Replace('\\', '/');
            string prefijo = Modelo + "/";
            if (!nombre.StartsWith(prefijo, StringComparison.Ordinal)) continue;
            string relativo = nombre.Substring(prefijo.Length);
            if (string.IsNullOrEmpty(relativo)) continue;
            string destino = Path.GetFullPath(Path.Combine(raiz, relativo));
            if (!destino.StartsWith(raizCompleta, StringComparison.Ordinal)) throw new IOException("Ruta no válida en el modelo.");
            if (nombre.EndsWith("/")) { Directory.CreateDirectory(destino); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            using (Stream entradaZip = entrada.Open()) using (FileStream archivo = File.Create(destino)) entradaZip.CopyTo(archivo);
        }
        if (!File.Exists(Path.Combine(raiz, "am", "final.mdl"))) throw new IOException("Modelo incompleto.");
        File.WriteAllText(marca, "ok");
        }
    }
    private void IniciarEscucha()
    {
        if (!activa || modelo == null || filaPendiente < 0 || !disponible() || !casillaValida(filaPendiente, columnaPendiente)) return;
        if (Microphone.devices.Length == 0) { Avisar("No hay micrófono disponible."); return; }
        dispositivo = Microphone.devices[0];
        try
        {
            reconocedor = new VoskRecognizer(modelo, Frecuencia, Gramatica);
            reconocedor.SetMaxAlternatives(0);
            clip = Microphone.Start(dispositivo, false, 15, Frecuencia);
            if (clip == null || clip.channels != 1 || clip.frequency != Frecuencia)
            { DetenerAudio(); Avisar("El micrófono no admite 16 kHz mono."); return; }
        }
        catch (Exception e) { Debug.LogError("VozKakuro al iniciar: " + e); DetenerAudio(); Avisar("No se pudo activar el micrófono."); return; }
        Avisar("Escuchando: di un número o borrar.");
        escucha = StartCoroutine(Escuchar(filaPendiente, columnaPendiente));
    }
    private IEnumerator Escuchar(int fila, int columna)
    {
        float fin = Time.realtimeSinceStartup + segundosEscucha;
        int posicion = 0; float[] muestras = new float[Bloque]; short[] pcm = new short[Bloque];
        string resultado = null;
        while (activa && disponible() && casillaValida(fila, columna) && Time.realtimeSinceStartup < fin)
        {
            int finGrabado = Microphone.GetPosition(dispositivo), procesados = 0;
            while (finGrabado - posicion >= Bloque && procesados < 16)
            {
                if (!clip.GetData(muestras, posicion)) break;
                for (int i = 0; i < Bloque; i++) pcm[i] = (short)(Mathf.Clamp(muestras[i], -1f, 1f) * 32767f);
                posicion += Bloque; procesados++;
                bool haTerminado = false, errorAudio = false;
                try { haTerminado = reconocedor.AcceptWaveform(pcm, pcm.Length); }
                catch (Exception e) { Debug.LogError("VozKakuro al reconocer: " + e); errorAudio = true; }
                if (errorAudio) { escucha = null; Desactivar(false); Avisar("Error al reconocer la voz."); yield break; }
                if (haTerminado) { resultado = reconocedor.Result(); break; }
            }
            if (resultado != null) break;
            yield return null;
        }
        if (resultado == null && reconocedor != null) resultado = reconocedor.FinalResult();
        int numero = NumeroReconocido(resultado);
        escucha = null; DetenerAudio(); filaPendiente = columnaPendiente = -1;
        if (!activa || !disponible() || !casillaValida(fila, columna)) yield break;
        if (numero < 0) { Avisar("No se reconoció un número ni borrar. Toca la casilla y repite."); yield break; }
        aplicar(fila, columna, numero);
        Avisar(numero == 0 ? "Casilla borrada. Toca otra casilla." :
            "Número " + numero + " reconocido. Toca otra casilla.");
    }
    private static int NumeroReconocido(string json)
    {
        if (string.IsNullOrEmpty(json)) return -1;
        Resultado r;
        try { r = JsonUtility.FromJson<Resultado>(json); } catch (Exception) { return -1; }
        if (r == null || string.IsNullOrWhiteSpace(r.text)) return -1;
        switch (r.text.Trim().ToLowerInvariant())
        {
            case "uno": case "un": return 1; case "dos": return 2; case "tres": return 3;
            case "cuatro": return 4; case "cinco": return 5; case "seis": return 6;
            case "siete": return 7; case "ocho": return 8; case "nueve": return 9;
            case "borrar": return 0;
            default: return -1;
        }
    }
    private void DetenerAudio()
    {
        if (escucha != null) { StopCoroutine(escucha); escucha = null; }
        if (!string.IsNullOrEmpty(dispositivo) && Microphone.IsRecording(dispositivo)) Microphone.End(dispositivo);
        if (clip != null) { Destroy(clip); clip = null; }
        if (reconocedor != null) { reconocedor.Dispose(); reconocedor = null; }
    }
    private void OnDisable() { Desactivar(false); }
    private void OnDestroy() { Desactivar(false); if (modelo != null) { modelo.Dispose(); modelo = null; } }
}
