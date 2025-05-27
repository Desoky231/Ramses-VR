using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends audio to your FastAPI endpoints (transcribe / ask / chat-with-audio)
/// and handles the responses. Attach once in any scene; survives scene loads.
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("FastAPI base URL")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    // ───────────────────────────────────────────────────────────────────────
    // DTOs used by JsonUtility
    // ───────────────────────────────────────────────────────────────────────
    [Serializable] private class TranscribeResponse { public string transcript; }
    [Serializable] private class AskResponse { public string response; }

    // Parse the tiny JSON stuffed in X-Metadata
    [Serializable]
    private struct MetadataHeader
    {
        public string session;
        public string transcript;
        public string response;
    }

    [Serializable] private struct PromptRequest { public string prompt; }

    // ───────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ───────────── PUBLIC API ──────────────────────────────────────────────

    /// <summary>Convert an AudioClip to WAV → /transcribe → transcript.</summary>
    public void Transcribe(AudioClip clip,
                           Action<string> onDone = null,
                           Action<string> onError = null)
    {
        if (clip == null) { onError?.Invoke("Clip was null"); return; }
        StartCoroutine(PostTranscribeCoroutine(clip, onDone, onError));
    }

    /// <summary>POST a prompt string to /ask → Gemini reply.</summary>
    public void AskGemini(string prompt,
                          Action<string> onDone = null,
                          Action<string> onError = null)
    {
        StartCoroutine(PostAskCoroutine(prompt, onDone, onError));
    }

    /// <summary>
    /// One-shot pipeline: upload clip → Whisper → RAG → TTS mp3.
    /// Auto-plays the reply and returns (transcript, response).
    /// </summary>
    public void ChatWithAudio(AudioClip clip,
                              Action<string, string> onDone = null,
                              Action<string> onError = null)
    {
        if (clip == null) { onError?.Invoke("Clip was null"); return; }
        StartCoroutine(PostChatWithAudioCoroutine(clip, onDone, onError));
    }

    // ───────────── COROUTINES ──────────────────────────────────────────────

    private IEnumerator PostTranscribeCoroutine(AudioClip clip,
                                                Action<string> onDone,
                                                Action<string> onError)
    {
        byte[] wav = WavUtility.FromAudioClip(clip);

        var form = new WWWForm();
        form.AddBinaryData("file", wav, "recording.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post($"{baseUrl}/transcribe", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<TranscribeResponse>(req.downloadHandler.text);
                onDone?.Invoke(data?.transcript ?? "");
            }
            else onError?.Invoke(req.error);
        }
    }

    private IEnumerator PostAskCoroutine(string prompt,
                                         Action<string> onDone,
                                         Action<string> onError)
    {
        var payload = new PromptRequest { prompt = prompt };
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using (UnityWebRequest req = new UnityWebRequest($"{baseUrl}/ask", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<AskResponse>(req.downloadHandler.text);
                onDone?.Invoke(data?.response ?? "");
            }
            else onError?.Invoke(req.error);
        }
    }

    /// <summary>
    /// Sends the clip to /chat-with-audio, receives MP3, plays it, returns transcript+reply.
    /// </summary>
    private IEnumerator PostChatWithAudioCoroutine(AudioClip clip,
                                                   Action<string, string> onDone,
                                                   Action<string> onError)
    {
        byte[] wav = WavUtility.FromAudioClip(clip);

        var form = new WWWForm();
        form.AddBinaryData("file", wav, "recording.wav", "audio/wav");

        string url = $"{baseUrl}/chat-with-audio";

        // Build POST request but override the download handler for audio
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

            // 1. Get the AudioClip and play it
            AudioClip replyClip = DownloadHandlerAudioClip.GetContent(req);
            if (replyClip == null)
            {
                onError?.Invoke("Server returned no audio.");
                yield break;
            }

            // Ensure we have an AudioSource
            AudioSource src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            src.clip = replyClip;
            src.Play();

            // 2. Parse X-Metadata header
            string transcript = "", response = "";
            if (req.GetResponseHeaders()
                  .TryGetValue("X-Metadata", out string metaJson))
            {
                try
                {
                    MetadataHeader meta =
                        JsonUtility.FromJson<MetadataHeader>(metaJson);
                    transcript = meta.transcript ?? "";
                    response = meta.response ?? "";
                }
                catch { Debug.LogWarning("Failed to parse X-Metadata"); }
            }
            else Debug.LogWarning("X-Metadata header missing");

            Debug.Log($"[ChatWithAudio] Transcript: {transcript}");
            Debug.Log($"[ChatWithAudio] Response  : {response}");

            onDone?.Invoke(transcript, response);
        }
    }
}
