using UnityEngine;
using UnityEngine.UI;

using agora_gaming_rtc;
using agora_utilities;
using System.Collections;


// this is an example of using Agora Unity SDK
// It demonstrates:
// How to enable video
// How to join/leave channel
// 
public class TestHelloUnityVideo
{
    //public GameObject go;

    AudienceTouchWatcher touchWatcher;
    MonoBehaviour monoProxy;

    IRtcEngine rtcEngine;
    int dataStreamId = 0;

    // instance of agora engine
    private IRtcEngine mRtcEngine;
    private Text MessageText;

    string RemoteScreenName = "RemoteARScreen";
    string UserScreenName = "UserScreen";

    // load agora engine
    public void loadEngine(string appId)
    {
        // start sdk
        Debug.Log("initializeEngine");

        if (mRtcEngine != null)
        {
            Debug.Log("Engine exists. Please unload it first!");
            return;
        }

        // init engine
        mRtcEngine = IRtcEngine.GetEngine(appId);

        // enable log
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }

    public void join(string channel, bool enableVideoOrNot)
    {
        Debug.Log("calling join (channel = " + channel + ")");

        if (mRtcEngine == null)
            return;

        // set callbacks (optional)
        mRtcEngine.OnJoinChannelSuccess = onJoinChannelSuccess;
        mRtcEngine.OnUserJoined = onUserJoined;
        mRtcEngine.OnUserOffline = onUserOffline;
        mRtcEngine.OnLeaveChannel += OnLeaveChannelHandler;
        mRtcEngine.OnWarning = (int warn, string msg) =>
        {
            Debug.LogWarningFormat("Warning code:{0} msg:{1}", warn, IRtcEngine.GetErrorDescription(warn));
        };
        mRtcEngine.OnError = HandleError;

        // enable video
        if (enableVideoOrNot)
        {
            mRtcEngine.EnableVideo();
        }

        // allow camera output callback
        mRtcEngine.EnableVideoObserver();

        // join channel
        mRtcEngine.JoinChannel(channel, null, 0);
    }

    void OnLeaveChannelHandler(RtcStats stats)
    {
        Debug.Log("OnLeaveChannelSuccess ---- TEST");
    }

    public string getSdkVersion()
    {
        string ver = IRtcEngine.GetSdkVersion();
        return ver;
    }

    public void leave()
    {
        Debug.Log("calling leave");

        if (mRtcEngine == null)
            return;

        // leave channel
        mRtcEngine.LeaveChannel();
        // deregister video frame observers in native-c code
        mRtcEngine.DisableVideoObserver();
    }

    // unload agora engine
    public void unloadEngine()
    {
        Debug.Log("calling unloadEngine");

        // delete
        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy();  // Place this call in ApplicationQuit
            mRtcEngine = null;
        }
    }

    public void EnableVideo(bool pauseVideo)
    {
        if (mRtcEngine != null)
        {
            if (!pauseVideo)
            {
                mRtcEngine.EnableVideo();
            }
            else
            {
                mRtcEngine.DisableVideo();
            }
        }
    }

    // accessing GameObject in Scnene1
    // set video transform delegate for statically created GameObject
    public void onSceneHelloVideoLoaded()
    {
        // Attach the SDK Script VideoSurface for video rendering
        GameObject quad = GameObject.Find(UserScreenName);
        if (ReferenceEquals(quad, null))
        {
            Debug.Log("failed to find Quad");
            return;
        }
        else
        {
            quad.AddComponent<VideoSurface>();
        }

        //GameObject GO = GameObject.Find(RemoteScreenName);
        //if (ReferenceEquals(GO, null))
        //{
        //    Debug.Log("failed to find Cube");
        //    return;
        //}
        //else
        //{
        //    GO.AddComponent<VideoSurface>();
        //}

        GameObject text = GameObject.Find("MessageText");
        if (!ReferenceEquals(text, null))
        {
            MessageText = text.GetComponent<Text>();
        }

        //GameObject gameObject = GameObject.Find("ColorController");
        //if (gameObject != null)
        //{
        //    colorButtonController = gameObject.GetComponent<ColorButtonController>();
        //    monoProxy = colorButtonController.GetComponent<MonoBehaviour>();

        //}

        GameObject gameObject = GameObject.Find("TouchWatcher");
        if (gameObject != null)
        {
            touchWatcher = gameObject.GetComponent<AudienceTouchWatcher>();
            monoProxy = touchWatcher.GetComponent<MonoBehaviour>();
            //touchWatcher.DrawColor = Color.blue;// colorButtonController.SelectedColor;
            touchWatcher.ProcessDrawing += ProcessDrawing;
            touchWatcher.NotifyClearDrawings += delegate ()
            {
                monoProxy.StartCoroutine(CoClearDrawing());

            };

            //colorButtonController.OnColorChange += delegate (Color color)
            //{
            //    touchWatcher.DrawColor = color;
            //};

            rtcEngine = IRtcEngine.QueryEngine();
            //DataStreamConfig dsc = new DataStreamConfig();
            //dsc.ordered = true;
            //dsc.syncWithAudio = true;
            //dataStreamId = rtcEngine.CreateDataStream(dsc);
            dataStreamId = rtcEngine.CreateDataStream(reliable: true, ordered: true);
            MessageText.text += "dataStreamId = " + dataStreamId;
        }
    }

    void ProcessDrawing(DrawmarkModel dm)
    {
        //MessageText.text += "Inside ProcessDrawing()!";
        monoProxy.StartCoroutine(CoProcessDrawing(dm));
    }

    IEnumerator CoProcessDrawing(DrawmarkModel dm)
    {
        string json = JsonUtility.ToJson(dm);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
        if (dataStreamId >= 0)
        {
            //MessageText.text += "Sending data bytes!";
            int ret = rtcEngine.SendStreamMessage(dataStreamId, data);
            MessageText.text += (ret == 0) ? "Success!" : MessageText.text += "Failure!";
        }
        else
        {
            MessageText.text += "dataStreamId < 0";
        }

        yield return null;
    }

    // implement engine callbacks
    private void onJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
        GameObject textVersionGameObject = GameObject.Find("VersionText");
        textVersionGameObject.GetComponent<Text>().text = "SDK Version : " + getSdkVersion();
    }

    // When a remote user joined, this delegate will be called. Typically
    // create a GameObject to render video on it
    private void onUserJoined(uint uid, int elapsed)
    {
        Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);
        // this is called in main thread

        // find a game object to render video stream from 'uid'
        GameObject go = GameObject.Find(uid.ToString());
        //GameObject go = GameObject.Find(RemoteScreenName);
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }

        // create a GameObject and assign to this new user
        VideoSurface videoSurface = makeImageSurface(uid.ToString());
        //VideoSurface videoSurface = makeImageSurface(RemoteScreenName);
        //VideoSurface videoSurface = go.GetComponent<VideoSurface>();
        //VideoSurface videoSurface = makePlaneSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            //videoSurface.enabled = true;
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
            videoSurface.SetGameFps(30);
        }
    }

    public VideoSurface makePlaneSurface(string goName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

        if (go == null)
        {
            return null;
        }
        go.name = goName;
        // set up transform
        go.transform.Rotate(-90.0f, 0.0f, 0.0f);
        float yPos = Random.Range(3.0f, 5.0f);
        float xPos = Random.Range(-2.0f, 2.0f);
        go.transform.position = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }

    //private const float Offset = 100;
    //public VideoSurface makeImageSurface(string goName)
    //{
    //    GameObject go = new GameObject();

    //    if (go == null)
    //    {
    //        return null;
    //    }

    //    go.name = goName;

    //    // to be renderered onto
    //    go.AddComponent<RawImage>();

    //    // make the object draggable
    //    //go.AddComponent<UIElementDragger>();
    //    GameObject canvas = GameObject.Find("Canvas");
    //    if (canvas != null)
    //    {
    //        go.transform.parent = canvas.transform;
    //    }
    //    // set up transform
    //    go.transform.Rotate(0f, 0.0f, 180.0f);
    //    float xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
    //    float yPos = Random.Range(Offset, Screen.height / 2f - Offset);
    //    //go.transform.localPosition = new Vector3(xPos, yPos, 0f);
    //    go.transform.position = new Vector3(f, 1.84f, 0f);
    //    go.transform.localScale = new Vector3(3f, 4f, 1f);

    //    // configure videoSurface
    //    VideoSurface videoSurface = go.AddComponent<VideoSurface>();
    //    return videoSurface;
    //}

    private const float Offset = 100;
    public VideoSurface makeImageSurface(string goName)
    {
        //GameObject go = new GameObject();
        GameObject go = GameObject.Find(RemoteScreenName);
        if (go == null)
        {
            Debug.LogError("No object with name RemoteARScreen found");
            return null;
        }

        go.name = goName;

        // to be renderered onto
        //go.AddComponent<RawImage>();

        // make the object draggable
        //go.AddComponent<UIElementDragger>();
        //GameObject canvas = GameObject.Find("Canvas");
        //if (canvas != null)
        //{
        //    go.transform.parent = canvas.transform;
        //}
        //// set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        //float xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        //float yPos = Random.Range(Offset, Screen.height / 2f - Offset);
        //go.transform.localPosition = new Vector3(xPos, yPos, 0f);
        //go.transform.position = new Vector3(2.5f, 1.84f, 0f);
        //go.transform.localScale = new Vector3(3f, 4f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
    // When remote user is offline, this delegate will be called. Typically
    // delete the GameObject for this user
    private void onUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        // remove video stream
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);
        // this is called in main thread
        GameObject go = GameObject.Find(uid.ToString());
        //GameObject go = GameObject.Find(RemoteScreenName);
        if (!ReferenceEquals(go, null))
        {
            Object.Destroy(go);
        }
    }

    IEnumerator CoClearDrawing()
    {
        string json = "{\"clear\": true}";
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
        if (dataStreamId > 0)
        {
            rtcEngine.SendStreamMessage(dataStreamId, data);
        }

        yield return null;
    }

    #region Error Handling
    private int LastError { get; set; }
    private void HandleError(int error, string msg)
    {
        if (error == LastError)
        {
            return;
        }

        msg = string.Format("Error code:{0} msg:{1}", error, IRtcEngine.GetErrorDescription(error));

        switch (error)
        {
            case 101:
                msg += "\nPlease make sure your AppId is valid and it does not require a certificate for this demo.";
                break;
        }

        Debug.LogError(msg);
        if (MessageText != null)
        {
            if (MessageText.text.Length > 0)
            {
                msg = "\n" + msg;
            }
            MessageText.text += msg;
        }

        LastError = error;
    }

    #endregion
}
