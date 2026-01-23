/*
 *  NetworkManager.cs
 *  Sends microphone audio to a FastAPI server, plays the returned MP3,
 *  and exposes transcript / response strings via callbacks.
 *  Reads “dropdownSelection.json” each time and forwards the chosen
 *  language ("en" | "ar") to the API.
 *
 *  JSON example:
 *  {
 *      "languages" : "Arabic",
 *      "Character" : "Ramses"
 *  }
 */

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════
    //  Data-transfer structs
    // ════════════════════════════════════════════════════════════════════
    [Serializable]
    private struct MetadataHeader
    {
        public string session;
        public string transcript;
        public string response;
        public string artifact;
    }

    [Serializable]
    private struct UserPrefs        // mirrors dropdownSelection.json
    {
        public string languages;                   // "English" | "Arabic"
        public string Character;
    }

    // ════════════════════════════════════════════════════════════════════
    //  MonoBehaviour boilerplate
    // ════════════════════════════════════════════════════════════════════
    public static NetworkManager Instance { get; private set; }

    [Header("FastAPI base URL")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Utility – load selected language
    // ════════════════════════════════════════════════════════════════════
    private static string LoadLanguage()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "dropdownSelection.json");

        try
        {
            if (!File.Exists(jsonPath)) return "en";

            string json = File.ReadAllText(jsonPath);
            var userPrefs = JsonUtility.FromJson<UserPrefs>(json);
            string langRaw = userPrefs.languages?.Trim().ToLowerInvariant() ?? "";

            return langRaw switch
            {
                "arabic" => "ar",
                "english" => "en",
                _ => "en"
            };
        }
        catch
        {
            Debug.LogWarning("LoadLanguage: failed to read JSON, defaulting to English");
            return "en";
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API – chat without artifact
    // ════════════════════════════════════════════════════════════════════
    public void ChatWithAudio(AudioClip clip,
                              Action<string, string> onDone = null,
                              Action<string> onError = null)
    {
        if (clip == null) { onError?.Invoke("Clip was null"); return; }
        StartCoroutine(PostChatWithAudioCoroutine(clip, onDone, onError));
    }

    private IEnumerator PostChatWithAudioCoroutine(AudioClip clip,
                                                   Action<string, string> onDone,
                                                   Action<string> onError)
    {
        byte[] wav = WavUtility.FromAudioClip(clip);
        string lang = LoadLanguage();

        var form = new WWWForm();
        form.AddBinaryData("file", wav, "recording.wav", "audio/wav");

        string url = $"{baseUrl}/chat-with-audio?lang={lang}";
        Debug.Log($"[NetworkManager] POST {url}");

        yield return SendAndHandleRequest(url, form, onDone, onError);
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API – chat about a specific artifact
    // ════════════════════════════════════════════════════════════════════
    public void ChatWithArtifact(AudioClip clip,
                                 string artifactName,
                                 Action<string, string> onDone = null,
                                 Action<string> onError = null)
    {
        if (clip == null) { onError?.Invoke("Clip was null"); return; }
        if (string.IsNullOrEmpty(artifactName))
        { onError?.Invoke("Artifact name required."); return; }

        StartCoroutine(PostChatWithArtifactCoroutine(clip, artifactName, onDone, onError));
    }

    private IEnumerator PostChatWithArtifactCoroutine(AudioClip clip,
                                                      string artifactName,
                                                      Action<string, string> onDone,
                                                      Action<string> onError)
    {
        byte[] wav = WavUtility.FromAudioClip(clip);
        string lang = LoadLanguage();

        var form = new WWWForm();
        form.AddBinaryData("file", wav, "recording.wav", "audio/wav");
        form.AddField("artifact", artifactName);
        form.AddField("lang", lang);              // ← name matches FastAPI

        string url = $"{baseUrl}/chat-with-artifact";
        Debug.Log($"[NetworkManager] POST {url} (lang={lang})");

        yield return SendAndHandleRequest(url, form, onDone, onError);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Shared HTTP-request / audio-playback routine
    // ════════════════════════════════════════════════════════════════════
    private IEnumerator SendAndHandleRequest(string url,
                                             WWWForm form,
                                             Action<string, string> onDone,
                                             Action<string> onError)
    {
        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            req.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            req.SetRequestHeader("Accept", "audio/mpeg");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            // Receive & play MP3
            AudioClip replyClip = DownloadHandlerAudioClip.GetContent(req);
            if (replyClip == null)
            {
                onError?.Invoke("Server returned no audio.");
                yield break;
            }

            AudioSource src = GetComponent<AudioSource>() ??
                              gameObject.AddComponent<AudioSource>();
            src.clip = replyClip;
            src.Play();

            // Parse metadata header
            string transcript = "", response = "";
            if (req.GetResponseHeaders().TryGetValue("X-Metadata", out string metaJson))
            {
                try
                {
                    var meta = JsonUtility.FromJson<MetadataHeader>(metaJson);
                    transcript = meta.transcript ?? "";
                    response = meta.response ?? "";
                }
                catch { Debug.LogWarning("Failed to parse X-Metadata"); }
            }
            else Debug.LogWarning("X-Metadata header missing");

            Debug.Log($"[Chat] Transcript: {transcript}");
            Debug.Log($"[Chat] Response  : {response}");

            onDone?.Invoke(transcript, response);
        }
    }
}
