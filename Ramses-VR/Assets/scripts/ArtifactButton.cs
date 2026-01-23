using UnityEngine;
using UnityEngine.EventSystems;

public class ArtifactButton : MonoBehaviour,
                               IPointerDownHandler,
                               IPointerUpHandler
{
    [Tooltip("Key used in FastAPI ARTIFACT_DB")]
    public string artifactKey = "EA19";

    public void OnPointerDown(PointerEventData e)
        => PushToTalkManager.Instance.BeginRecord(artifactKey);

    public void OnPointerUp(PointerEventData e)
        => PushToTalkManager.Instance.EndRecord();
}
