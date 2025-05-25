using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";   // FastAPI origin

    // ---------- DTOs used by JsonUtility -----------------------------------
    [Serializable] private class TranscribeResponse { public string transcript; }
    [Serializable] private class AskResponse { public string response; }
    [Serializable] private class ChatResponse { public string transcript; public string response; }
    [Serializable] private struct PromptRequest { public string prompt; }

    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);      // survive scene loads
    }

    // ---------- PUBLIC API --------------------------------------------------

    /// <summary>
    /// Convert an AudioClip to WAV, POST to /transcribe, return the transcript.
    /// </summary>
    public void Transcribe(AudioClip clip,
                           Action<string> onDone = null,
                           Action<string> onError = null)
    {
        if (clip == null) { onError?.Invoke("Clip was null"); return; }
        StartCoroutine(PostTranscribeCoroutine(clip, onDone, onError));
    }

    /// <summary>
    /// Send a prompt to the Gemini /ask endpoint, return the model’s reply.
    /// </summary>
    public void AskGemini(string prompt,
                          Action<string> onDone = null,
                          Action<string> onError = null)
    {
        StartCoroutine(PostAskCoroutine(prompt, onDone, onError));
    }

    /// <summary>
    /// One-shot pipeline: upload audio → Whisper transcription → Gemini answer.
    /// Logs both transcript and response, and invokes onDone(transcript, response).
    /// </summary>
    public void ChatWithAudio(AudioClip clip,
                              Action<string, string> onDone = null,
                              Action<string> onError = null)
    {
        if (clip == null) { onError?.Invoke("Clip was null"); return; }
        StartCoroutine(PostChatWithAudioCoroutine(clip, onDone, onError));
    }

    // ---------- COROUTINES --------------------------------------------------

    private IEnumerator PostTranscribeCoroutine(AudioClip clip,
                                                Action<string> onDone,
                                                Action<string> onError)
    {
        byte[] wavData = WavUtility.FromAudioClip(clip);
        var form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post($"{baseUrl}/transcribe", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                var data = JsonUtility.FromJson<TranscribeResponse>(json);
                string transcriptOrEmpty = data?.transcript ?? "";

                Debug.Log($"[ASR] {transcriptOrEmpty}");
                onDone?.Invoke(transcriptOrEmpty);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    private IEnumerator PostAskCoroutine(string prompt,
                                         Action<string> onDone,
                                         Action<string> onError)
    {
        var payload = new PromptRequest { prompt = prompt };
        string body = JsonUtility.ToJson(payload);

        using (UnityWebRequest req = new UnityWebRequest($"{baseUrl}/ask", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                var data = JsonUtility.FromJson<AskResponse>(json);
                string responseOrEmpty = data?.response ?? "";

                onDone?.Invoke(responseOrEmpty);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    private IEnumerator PostChatWithAudioCoroutine(AudioClip clip,
                                                   Action<string, string> onDone,
                                                   Action<string> onError)
    {
        byte[] wavData = WavUtility.FromAudioClip(clip);
        var form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post($"{baseUrl}/chat-with-audio", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                var data = JsonUtility.FromJson<ChatResponse>(json);
                string transcriptOrEmpty = data?.transcript ?? "";
                string responseOrEmpty = data?.response ?? "";

                Debug.Log($"[ChatWithAudio] Transcript: {transcriptOrEmpty}");
                Debug.Log($"[ChatWithAudio] Response:   {responseOrEmpty}");

                onDone?.Invoke(transcriptOrEmpty, responseOrEmpty);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }
}
