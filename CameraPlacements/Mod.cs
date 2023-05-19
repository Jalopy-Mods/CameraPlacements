using UnityEngine.SceneManagement;
using System.Collections.Generic;
using JaLoader;
using System.Linq;
using UnityEngine;
using System.IO;
using System;
using System.Text.RegularExpressions;
using Console = JaLoader.Console;


namespace CameraPlacements
{
    public class Plugin : Mod
    {
        private CameraMenu _cameraMenu;
        private GameObject _conePrefab;
        private MouseLook _locker0;
        private MouseLook _locker1;
        // public static Dictionary<string, ConfigEntry<KeyboardShortcut>> KeyBinds;

        public override string ModID => "CameraPlacements";
        public override string ModName => "Camera Placements";
        public override string ModAuthor => "MeblIkea";
        public override string ModDescription => "This mod let you place cameras in the map, and do animations.";
        public override string ModVersion => "1.0.1";

        public override WhenToInit WhenToInit => WhenToInit.InMenu;

        public override void Start()
        {
            var coneBundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, "CameraPlacementsAssets"));
            if (coneBundle == null)
            {
                Console.Instance.LogError("Failed to load AssetBundle!");
                return;
            }
            // KeyBinds = new Dictionary<string, ConfigEntry<KeyboardShortcut>>
            // {
            //     { "OpenMenu", Config.Bind("Hotkeys", "OpenMenu", new KeyboardShortcut(KeyCode.F10)) },
            //     { "MoveForward", Config.Bind("Hotkeys", "MoveForward", new KeyboardShortcut(KeyCode.UpArrow)) },
            //     { "MoveBackward", Config.Bind("Hotkeys", "MoveBackward", new KeyboardShortcut(KeyCode.DownArrow)) },
            //     { "MoveLeft", Config.Bind("Hotkeys", "MoveLeft", new KeyboardShortcut(KeyCode.LeftArrow)) },
            //     { "MoveRight", Config.Bind("Hotkeys", "MoveRight", new KeyboardShortcut(KeyCode.RightArrow)) },
            //     { "MoveUp", Config.Bind("Hotkeys", "MoveUp", new KeyboardShortcut(KeyCode.Space)) },
            //     { "ControlPoint", Config.Bind("Hotkeys", "PreviewPoint", new KeyboardShortcut(KeyCode.Space)) }
            // };

            _conePrefab = Instantiate(coneBundle.LoadAsset<GameObject>("pointer-cone"));
            DontDestroyOnLoad(_conePrefab);
            coneBundle.Unload(false);
        }

        public override void OnEnable()
        {
            Initialize();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public override void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Scn2_CheckpointBravo") || scene.name.Equals("MainMenu"))
            {
                Initialize();
            }

            if (!scene.name.Equals("Scn2_CheckpointBravo")) return;
            _locker0 = GameObject.Find("/First Person Controller").GetComponent<MouseLook>();
            _locker1 = GameObject.Find("/First Person Controller/Main Camera").GetComponent<MouseLook>();
        }

        private void Initialize()
        {
            _cameraMenu = new GameObject("CameraMenu").AddComponent<CameraMenu>();
            _cameraMenu.pointsManager.conePrefab = _conePrefab;
            _cameraMenu.GetComponent<CameraMenu>().enabled = false;
            // I want to give this  value to this object: _toggleMenu
        }

        private static void UpdateCursorControl(bool locked)
        {
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public override void Update()
        {
            if (SceneManager.GetActiveScene().name != "Scn2_CheckpointBravo" &&
                SceneManager.GetActiveScene().name != "MainMenu") return;
            if (Input.GetKeyDown(KeyCode.F10) && !_cameraMenu.saveManagerOpen)
            {
                _cameraMenu.enabled = !_cameraMenu.enabled;
                if (_cameraMenu.enabled && SceneManager.GetActiveScene().name == "Scn2_CheckpointBravo")
                {
                    _locker0.enabled = false;
                    _locker1.enabled = false;
                }
                else if (!_cameraMenu.enabled && SceneManager.GetActiveScene().name == "Scn2_CheckpointBravo")
                {
                    _locker0.enabled = true;
                    _locker1.enabled = true;
                }
            }
            if (_cameraMenu.enabled) UpdateCursorControl(true);
        }
    }

    internal class CameraMenu : MonoBehaviour
    {
        private (Vector3, Vector3, float, int, int, float) _lastPoint;
        public int currentlySelectedPoint = -1;
        private CameraManager _cameraManager;
        private GradientColorKey[] _colorKey;
        public PointsManager pointsManager;
        public LineRenderer lineRenderer;
        private Vector2 _scrollPosition;
        private Regex _groupNameRegex;
        public bool saveManagerOpen;
        private Gradient _gradient;
        private string _saveError;
        private Camera _newCamera;

        private void Awake()
        {
            _newCamera = new GameObject("FreeCamera").AddComponent<Camera>();
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                _newCamera.transform.position = GameObject.Find("/Main Camera").transform.position;
            }
            else if (SceneManager.GetActiveScene().name == "Scn2_CheckpointBravo")
            {
                _newCamera.transform.position =
                    GameObject.Find("/First Person Controller/Main Camera").transform.position;
            }
            _cameraManager = _newCamera.gameObject.AddComponent<CameraManager>();
            pointsManager = _newCamera.gameObject.AddComponent<PointsManager>();
            _newCamera.gameObject.AddComponent<Rigidbody>().useGravity = false;
            _newCamera.enabled = false;
            lineRenderer = _newCamera.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _gradient = new Gradient();
            _gradient.SetKeys(
                new[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.green, 0.5f), new GradientColorKey(Color.red, 1.0f) },
                new[] { new GradientAlphaKey(100, 0.0f), new GradientAlphaKey(100, 0.0f), new GradientAlphaKey(100, 1.0f) }
            );
            lineRenderer.colorGradient = _gradient;
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.5f;
            _groupNameRegex = new Regex("[^a-zA-Z0-9 _-]");
            saveManagerOpen = false;

        }

        private void OnGUI()
        {
            if (saveManagerOpen)
            {
                var sw = Screen.width / 2;
                var sh = Screen.height / 2;
                GUI.Box(new Rect(sw - 200, sh - 200, 400, 400), "Save menu");
                if (GUI.Button(new Rect(sw - 195, sh - 195, 50, 25), "Close")) saveManagerOpen = false;

                _scrollPosition = GUI.BeginScrollView(new Rect(sw - 200, sh - 180, 400, 380), _scrollPosition, new Rect(0, 0, 400, 400));
                var saves = SaveSystem.GetSaves();
                for (var i = 0; i < saves.Count; i++)
                {
                    GUI.Label(new Rect(5, 10 + i * 30, 275, 25), saves[i]);
                    if (GUI.Button(new Rect(280, 10 + i * 30, 40, 25), "Load"))
                    {
                        pointsManager.UnloadPoints();
                        pointsManager.Points.Clear();
                        pointsManager.PointsObjects.Clear();
                        foreach (var p in SaveSystem.LoadPoints(saves[i])) pointsManager.AddPoint(p.Key, p.Value.Item1, p.Value.Item2, p.Value.Item3, p.Value.Item4, p.Value.Item5, (int)p.Value.Item6);
                        currentlySelectedPoint = pointsManager.Points.Count - 1;
                        lineRenderer.positionCount = pointsManager.Points.Count;
                        for (var u = 0; u < pointsManager.Points.Count; u++)
                        {
                            lineRenderer.SetPosition(u, pointsManager.Points[u].Item1);
                        }
                        saveManagerOpen = false;
                    }
                    if (GUI.Button(new Rect(325, 10 + i * 30, 60, 25), "Delete"))
                    {
                        SaveSystem.DeleteSave(saves[i]);
                    }
                }
                GUI.EndScrollView();
            }
            if (_cameraManager.animStarted || saveManagerOpen) return;

            GUI.Box(new Rect(5, Screen.height - 255, 470, 300), "Camera Control");
            if (GUI.Button(new Rect(10, Screen.height - 70, 100, 60), "Switch view\nto freecam"))
            {
                _newCamera.enabled = !_newCamera.enabled;
            }
            if (GUI.Button(new Rect(10, Screen.height - 135, 100, 60), "Launch\nanimation") &&
                pointsManager.Points.Count > 1) _cameraManager.StartAnimation();

            var transform1 = _newCamera.transform;
            var position = transform1.position;
            var rotation = transform1.eulerAngles;

            GUI.Label(new Rect(115, Screen.height - 60, 50, 25), "Position");
            float.TryParse(GUI.TextField(new Rect(170, Screen.height - 60, 60, 20), position.x + ""), out var cPx);
            float.TryParse(GUI.TextField(new Rect(235, Screen.height - 60, 60, 20), position.y + ""), out var cPy);
            float.TryParse(GUI.TextField(new Rect(300, Screen.height - 60, 60, 20), position.z + ""), out var cPz);

            GUI.Label(new Rect(115, Screen.height - 40, 50, 25), "Rotation");
            float.TryParse(GUI.TextField(new Rect(170, Screen.height - 40, 60, 20), rotation.x + ""), out var cRx);
            float.TryParse(GUI.TextField(new Rect(235, Screen.height - 40, 60, 20), rotation.y + ""), out var cRy);
            float.TryParse(GUI.TextField(new Rect(300, Screen.height - 40, 60, 20), rotation.z + ""), out var cRz);

            transform1.position = new Vector3(cPx, cPy, cPz);
            transform1.eulerAngles = new Vector3(cRx, cRy, cRz);

            _cameraManager.zLocked = GUI.Toggle(new Rect(365, Screen.height - 40, 100, 20), _cameraManager.zLocked,
                "Lock X&Z axis");
            GUI.Label(new Rect(365, Screen.height - 255, 150, 20), "Camera FOV: " + _newCamera.fieldOfView);
            GUI.Label(new Rect(350, Screen.height - 238, 150, 20), "Freecam Speed: " + _cameraManager.mSpeed);

            GUI.Label(new Rect(115, Screen.height - 110, 200, 20), _saveError,
                _saveError == "Saved!"
                    ? new GUIStyle { normal = { textColor = Color.green } }
                    : new GUIStyle { normal = { textColor = Color.red } });
            GUI.Label(new Rect(115, Screen.height - 85, 120, 20), "Group Point Name: ");
            pointsManager.currentGroupPointName = _groupNameRegex.Replace(GUI.TextField(
                new Rect(235, Screen.height - 85, 125, 20),
                pointsManager.currentGroupPointName), "").Truncate(20);
            if (GUI.Button(new Rect(365, Screen.height - 85, 50, 20), "Save"))
            {
                if (pointsManager.currentGroupPointName == "")
                {
                    _saveError = "Your save must have more than 0 characters!";
                }
                else if (SaveSystem.GetSaveThatContain(pointsManager.currentGroupPointName).Count != 0 &&
                         _saveError != "This name is already taken! Click save again to erase.")
                {
                    _saveError = "This name is already taken! Click save again to erase.";
                }
                else
                {
                    _saveError = "Saved!";
                    SaveSystem.SavePoints(pointsManager.currentGroupPointName, pointsManager.Points);
                }
            }

            if (GUI.Button(new Rect(365, Screen.height - 60, 100, 20), "Manage Saves")) saveManagerOpen = true;

            // var nEffect0 = GUI.Toggle(new Rect(115, Screen.height - 130, 100, 25), _cameraManager.Effects["Vignetting"], "Vignetting");
            // var nEffect1 = GUI.Toggle(new Rect(115, Screen.height - 110, 100, 25), _cameraManager.Effects["BloomAndLensFlares"], "Bloom");
            // var nEffect2 = GUI.Toggle(new Rect(115, Screen.height - 90, 100, 25), _cameraManager.Effects["SunShafts"], "Sun Shafts");
            // var nEffect3 = GUI.Toggle(new Rect(215, Screen.height - 110, 100, 25), _cameraManager.Effects["SSAOPro"], "SSAO");
            // var nEffect4 = GUI.Toggle(new Rect(215, Screen.height - 90, 100, 25), _cameraManager.Effects["AntialiasingAsPostEffect"], "Antialiasing");
            //
            // if (nEffect0 != _cameraManager.Effects["Vignetting"])
            // {
            //     _cameraManager.Effects["Vignetting"] = nEffect0;
            //     if (_cameraManager.Effects["Vignetting"]) CameraManager.EnableEffect("Vignetting");
            //     else CameraManager.DisableEffect("Vignetting");
            // }
            // if (nEffect1 != _cameraManager.Effects["BloomAndLensFlares"])
            // {
            //     _cameraManager.Effects["BloomAndLensFlares"] = nEffect1;
            //     if (_cameraManager.Effects["BloomAndLensFlares"]) CameraManager.EnableEffect("BloomAndLensFlares");
            //     else CameraManager.DisableEffect("BloomAndLensFlares");
            // }
            // if (nEffect2 != _cameraManager.Effects["SunShafts"])
            // {
            //     _cameraManager.Effects["SunShafts"] = nEffect2;
            //     if (_cameraManager.Effects["SunShafts"]) CameraManager.EnableEffect("SunShafts");
            //     else CameraManager.DisableEffect("SunShafts");
            // }
            // if (nEffect3 != _cameraManager.Effects["SSAOPro"])
            // {
            //     _cameraManager.Effects["SSAOPro"] = nEffect3;
            //     if (_cameraManager.Effects["SSAOPro"]) CameraManager.EnableEffect("SSAOPro");
            //     else CameraManager.DisableEffect("SSAOPro");
            // }
            // if (nEffect4 != _cameraManager.Effects["AntialiasingAsPostEffect"])
            // {
            //     _cameraManager.Effects["AntialiasingAsPostEffect"] = nEffect4;
            //     if (_cameraManager.Effects["AntialiasingAsPostEffect"]) CameraManager.EnableEffect("AntialiasingAsPostEffect");
            //     else CameraManager.DisableEffect("AntialiasingAsPostEffect");
            // }


            var lastSelectedPoint = currentlySelectedPoint;
            if (pointsManager.Points.Count > 0)
            {
                _lastPoint = pointsManager.Points[currentlySelectedPoint];
            }
            GUI.Label(new Rect(10, Screen.height - 250, 150, 20), "Tracks Points: " + pointsManager.Points.Count);
            if (GUI.Button(new Rect(10, Screen.height - 225, 150, 20), "Add Point"))
            {
                if (pointsManager.Points.Count > currentlySelectedPoint) currentlySelectedPoint++;
                pointsManager.AddPoint(currentlySelectedPoint, transform1.position, transform1.eulerAngles,
                    _newCamera.fieldOfView, _cameraManager.mSpeed);
                lineRenderer.positionCount = pointsManager.Points.Count;
                for (var i = 0; i < pointsManager.Points.Count; i++)
                {
                    lineRenderer.SetPosition(i, pointsManager.Points[i].Item1);
                }
            }

            if (GUI.Button(new Rect(345, Screen.height - 215, 120, 20), "Remove Point") &&
                pointsManager.Points.Count > 0)
            {
                pointsManager.RemovePoint(currentlySelectedPoint);
                if (currentlySelectedPoint > 0) currentlySelectedPoint--;
                lineRenderer.positionCount = pointsManager.Points.Count;
            }

            GUI.Label(new Rect(175, Screen.height - 237, 180, 20), "Currently Selected Point: " + currentlySelectedPoint);
            currentlySelectedPoint = (int)GUI.HorizontalSlider(new Rect(180, Screen.height - 215, 150, 20),
                currentlySelectedPoint, 0,
                MinClamp(pointsManager.Points.Count - 1, 0));
            if (pointsManager.Points.Count > 0)
            {
                GUI.Label(new Rect(10, Screen.height - 200, 50, 25), "Position");
                GUI.Label(new Rect(10, Screen.height - 180, 50, 25), "Rotation");
                GUI.Label(new Rect(30, Screen.height - 160, 50, 25), "FOV");
                GUI.Label(new Rect(152, Screen.height - 160, 50, 25), "Speed");
                GUI.Label(new Rect(260, Screen.height - 160, 150, 20), "Wait time (ms)");
                var point = pointsManager.Points[currentlySelectedPoint];
                float.TryParse(
                    GUI.TextField(new Rect(65, Screen.height - 180, 60, 20), point.Item2.x + ""), out var prx);
                float.TryParse(
                    GUI.TextField(new Rect(130, Screen.height - 180, 60, 20), point.Item2.y + ""), out var pry);
                float.TryParse(
                    GUI.TextField(new Rect(195, Screen.height - 180, 60, 20), point.Item2.z + ""), out var prz);
                float.TryParse(GUI.TextField(new Rect(65, Screen.height - 200, 60, 20), point.Item1.x + ""),
                    out var ppx);
                float.TryParse(GUI.TextField(new Rect(130, Screen.height - 200, 60, 20), point.Item1.y + ""),
                    out var ppy);
                float.TryParse(GUI.TextField(new Rect(195, Screen.height - 200, 60, 20), point.Item1.z + ""),
                    out var ppz);
                float.TryParse(GUI.TextField(new Rect(65, Screen.height - 160, 60, 20), point.Item3 + ""),
                    out var pFov);
                int.TryParse(GUI.TextField(new Rect(195, Screen.height - 160, 60, 20), point.Item4 + ""),
                    out var pSpeed);
                float.TryParse(GUI.TextField(new Rect(360, Screen.height - 160, 60, 20), point.Item6 + ""),
                    out var pWait);

                var baseType = pointsManager.Points[currentlySelectedPoint].Item5;
                GUI.Label(new Rect(260, Screen.height - 180, 150, 20), "Animation mode:");
                if (GUI.Toggle(new Rect(360, Screen.height - 180, 30, 20),
                        baseType == 0, "▲")) baseType = 0;
                if (GUI.Toggle(new Rect(390, Screen.height - 180, 30, 20),
                        baseType == 1, "●")) baseType = 1;
                if (GUI.Toggle(new Rect(420, Screen.height - 180, 30, 20),
                        baseType == 2, "■")) baseType = 2;

                pointsManager.Points[currentlySelectedPoint] = (
                    new Vector3(ppx, ppy, ppz),
                    new Vector3(Clamp(prx, 359, 0), Clamp(pry, 359, -359), Clamp(prz, 359, -359)),
                    (int)Clamp(pFov, 175, 5), (int)Clamp(pSpeed, 950, 1), baseType, pWait);

                lineRenderer.SetPosition(currentlySelectedPoint, pointsManager.Points[currentlySelectedPoint].Item1);
            }

            if (pointsManager.Points.Count == 0) return;
            if (currentlySelectedPoint == lastSelectedPoint && pointsManager.Points[currentlySelectedPoint] != _lastPoint)
            {
                var (fov, speed) = (pointsManager.Points[currentlySelectedPoint].Item3,
                    pointsManager.Points[currentlySelectedPoint].Item4);
                pointsManager.PointsObjects[currentlySelectedPoint].transform.localScale =
                    new Vector3(fov / 60 * 100, fov / 60 * 100, Clamp(speed, 150, 0) / 30 * 100);
            }
            if (lastSelectedPoint == currentlySelectedPoint || lastSelectedPoint == -1) return;
            _colorKey = new[] { new GradientColorKey(Color.magenta, 0.0f), new GradientColorKey(Color.magenta, 1.0f) };
            _gradient.SetKeys(_colorKey, new[] { new GradientAlphaKey(100, 0.0f), new GradientAlphaKey(100, 1.0f) });
            lineRenderer.colorGradient = _gradient;

            if (pointsManager.PointsObjects.Count != lastSelectedPoint)
            {
                pointsManager.PointsObjects[lastSelectedPoint].GetComponent<MeshRenderer>().material.color = Color.white;
                pointsManager.PointsObjects[currentlySelectedPoint].GetComponent<MeshRenderer>().material.color =
                    Color.blue;
            }
            else
            {
                pointsManager.PointsObjects[currentlySelectedPoint].GetComponent<MeshRenderer>().material.color =
                    Color.blue;
            }
        }
        private static float Clamp(float value, float max, float min)
        {
            return value > max ? max : value < min ? min : value;
        }
        private static float MinClamp(float value, float min)
        {
            return value < min ? min : value;
        }
    }
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength, string truncationSuffix = "…")
        {
            return value?.Length > maxLength ? value.Substring(0, maxLength) + truncationSuffix : value;
        }
    }
    internal class CameraManager : MonoBehaviour
    {
        private PointsManager _pointsManager;
        private float _originalDistance;
        private CameraMenu _mCameraMenu;
        private Rigidbody _mRigidbody;
        private AnimationCurve _curve = new AnimationCurve();
        private float _startTime;
        private Camera _mCamera;
        private GameObject _oCamera;
        public bool animStarted;
        public int mSpeed = 10;
        public bool zLocked;

        // public readonly Dictionary<string, bool> Effects = new();


        private void Start()
        {
            _mRigidbody = gameObject.GetComponent<Rigidbody>();
            _mCamera = gameObject.GetComponent<Camera>();
            _mCameraMenu = GameObject.Find("CameraMenu").GetComponent<CameraMenu>();
            _pointsManager = gameObject.GetComponent<PointsManager>();
            // Effects.Add("Vignetting", false);
            // Effects.Add("BloomAndLensFlares", false);
            // Effects.Add("SunShafts", false);
            // Effects.Add("SSAOPro", false);
            // Effects.Add("AntialiasingAsPostEffect", false);
            _oCamera = GameObject.Find("Main Camera");
        }

        public void StartAnimation()
        {
            SetCurve(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item5);
            animStarted = true;
            var transform0 = gameObject.transform;
            transform0.position = _pointsManager.Points[0].Item1;
            transform0.rotation = Quaternion.Euler(_pointsManager.Points[0].Item2);
            _mCameraMenu.currentlySelectedPoint = 1;
            _mCameraMenu.lineRenderer.enabled = false;
            var p0 = transform0.position;
            var p1 = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
            _originalDistance =
                (float)Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) + Math.Pow(p1.z - p0.z, 2));
            _startTime = Time.time + _pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item6 / 1000;
            for (var i = 0; i < _pointsManager.Points.Count; i++)
            {
                _pointsManager.PointsObjects[i].SetActive(false);
            }
            _oCamera.SetActive(false);
        }

        public void StopAnimation()
        {
            animStarted = false;
            _mCameraMenu.currentlySelectedPoint = 0;
            _mCameraMenu.lineRenderer.enabled = true;
            for (var i = 0; i < _pointsManager.Points.Count; i++)
            {
                _pointsManager.PointsObjects[i].SetActive(true);
            }
            _oCamera.SetActive(true);
        }
        private void SetCurve(int type)
        {
            _curve = new AnimationCurve();
            switch (type)
            {
                case 0:
                    _curve.AddKey(0, 0);
                    _curve.AddKey(1, 1);
                    break;
                case 1:
                    _curve.AddKey(0, 0);
                    _curve.AddKey(0.1f, 0.02f);
                    _curve.AddKey(0.2f, 0.08f);
                    _curve.AddKey(0.3f, 0.18f);
                    _curve.AddKey(0.4f, 0.32f);
                    _curve.AddKey(0.5f, 0.5f);
                    _curve.AddKey(0.6f, 0.68f);
                    _curve.AddKey(0.7f, 0.82f);
                    _curve.AddKey(0.8f, 0.92f);
                    _curve.AddKey(0.9f, 0.98f);
                    _curve.AddKey(0.95f, 1.0f);
                    _curve.AddKey(1.0f, 1.0f);
                    break;
                case 2:
                    _curve.AddKey(0, 0);
                    _curve.AddKey(0.99f, 0);
                    _curve.AddKey(1, 1);
                    break;
                default:
                    Console.Instance.LogError("Curve type not found");
                    break;
            }
        }
        private void Update()
        {
            if (animStarted)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    StopAnimation();
                    return;
                }

                var p0 = gameObject.transform.position;
                var p1 = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
                var d = Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) + Math.Pow(p1.z - p0.z, 2));

                if (d < 0.2 || (_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item4 > 100 && d < 5))
                {
                    _mCamera.fieldOfView = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item3;
                    _mCameraMenu.currentlySelectedPoint++;
                    if (_mCameraMenu.currentlySelectedPoint == _pointsManager.Points.Count)
                    {
                        StopAnimation();
                    }
                    else
                    {
                        p1 = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
                        _originalDistance = (float)Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) +
                                                             Math.Pow(p1.z - p0.z, 2));
                        _startTime = Time.time + _pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item6 / 1000;
                        SetCurve(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item5);
                    }
                }
                else
                {
                    var curve = _curve.Evaluate((Time.time - _startTime) * _pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item4 / _originalDistance);
                    _mCamera.fieldOfView = Mathf.Lerp(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item3, _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item3, curve);
                    gameObject.transform.rotation = Quaternion.Lerp(Quaternion.Euler(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item2), Quaternion.Euler(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item2), curve);
                    gameObject.transform.position = Vector3.Lerp(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item1, _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1, curve);
                }
                return;
            }

            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKey(KeyCode.Space))
            {
                if (_mCameraMenu.lineRenderer.enabled)
                {
                    _mCameraMenu.lineRenderer.enabled = false;
                    _pointsManager.PointsObjects[_mCameraMenu.currentlySelectedPoint].SetActive(false);
                    gameObject.transform.position = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
                    gameObject.transform.rotation =
                        Quaternion.Euler(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item2);
                    _mCamera.fieldOfView = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item3;
                }
                else
                {
                    var o = gameObject;
                    var p = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint];
                    _pointsManager.Points[_mCameraMenu.currentlySelectedPoint] = (o.transform.position,
                        o.transform.rotation.eulerAngles, _mCamera.fieldOfView, p.Item4, p.Item5, p.Item6);
                }
            }
            else if (_mCameraMenu.lineRenderer.enabled == false)
            {
                _mCameraMenu.lineRenderer.enabled = true;
                _pointsManager.PointsObjects[_mCameraMenu.currentlySelectedPoint].SetActive(true);
            }

            var rot = transform.rotation;
            if (Input.GetKey(KeyCode.UpArrow))
            {
                _mRigidbody.velocity = transform.forward * mSpeed;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                _mRigidbody.velocity = -transform.forward * mSpeed;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                _mRigidbody.velocity = transform.right * mSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                _mRigidbody.velocity = -transform.right * mSpeed;
            }
            else if (Input.GetKey(KeyCode.Space) && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                _mRigidbody.velocity = transform.up * mSpeed;
            }
            else
            {
                _mRigidbody.velocity = Vector3.zero;
            }
            if (Input.GetMouseButton(1))
            {
                transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0));
            }
            if (zLocked && !Input.GetMouseButton(1))
            {
                transform.rotation = new Quaternion(0, rot.y, 0, rot.w);
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                if (Input.mouseScrollDelta.y > 0)
                {
                    if (_mCamera.fieldOfView < 175) _mCamera.fieldOfView += 5;
                }
                else if (Input.mouseScrollDelta.y < 0)
                {
                    if (_mCamera.fieldOfView > 5) _mCamera.fieldOfView -= 5;
                }     
            }
            else
            {
                if (Input.mouseScrollDelta.y > 0)
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                    {
                        if (mSpeed < 980) mSpeed += 20;
                    }
                    else
                    {
                        if (mSpeed < 995) mSpeed += 5;
                    }
                }
                else if (Input.mouseScrollDelta.y < 0)
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                    {
                        if (mSpeed > 20) mSpeed -= 20;
                    }
                    else
                    {
                        if (mSpeed > 5) mSpeed -= 5;
                    }
                }
            }
        }

        public static void EnableEffect(string effect)
        {
            throw new NotImplementedException();
        }

        public static void DisableEffect(string effect)
        {
            throw new NotImplementedException();
        }
    }

    internal class PointsManager : MonoBehaviour
    {                                  //position, rotation, fov, speed, type, time
        public readonly Dictionary<int, (Vector3, Vector3, float, int, int, float)> Points = new Dictionary<int, (Vector3, Vector3, float, int, int, float)>();
        public Dictionary<int, GameObject> PointsObjects = new Dictionary<int, GameObject>();
        private CameraMenu _cameraMenu;
        public GameObject conePrefab;
        public string currentGroupPointName;

        private void Awake()
        {
            currentGroupPointName = "New Group " + SaveSystem.GetSaveThatContain("New Group").Count;
            _cameraMenu = GameObject.Find("CameraMenu").GetComponent<CameraMenu>();
        }
        public void ReloadPoints()
        {
            foreach (var point in PointsObjects)
            {
                Destroy(point.Value);
            }

            PointsObjects = new Dictionary<int, GameObject>();
            foreach (var point in Points)
            {
                PointsObjects[point.Key] = Instantiate(conePrefab);
                PointsObjects[point.Key].transform.position = point.Value.Item1;
                PointsObjects[point.Key].transform.eulerAngles = point.Value.Item2;
            }
        }
        public void UnloadPoints()
        {
            foreach (var point in PointsObjects)
            {
                Destroy(point.Value);
            }
        }
        public void AddPoint(int index, Vector3 position, Vector3 rotation, float fov, int speed, int type = 0, int time = 0)
        {
            if (Points.ContainsKey(index))
            {
                for (var i = Points.Count; i > index; i--)
                {
                    Points[i] = Points[i - 1];
                    Points.Remove(i - 1);
                    PointsObjects[i] = PointsObjects[i - 1];
                    PointsObjects.Remove(i - 1);
                }
            }
            Points[index] = (position, rotation, fov, speed, type, time);
            PointsObjects[index] = Instantiate(conePrefab);
            PointsObjects[index].transform.position = position;
            PointsObjects[index].transform.eulerAngles = rotation;
            PointsObjects[index].transform.localScale =
                new Vector3(fov / 60 * 100, fov / 60 * 100, Clamp(speed, 150, 0) / 30 * 100);
        }

        private static float Clamp(float value, float max, float min)
        {
            return value > max ? max : value < min ? min : value;
        }

        public void RemovePoint(int index)
        {
            Destroy(PointsObjects[index]);
            Points.Remove(index);
            PointsObjects.Remove(index);
            for (var i = index; i < Points.Count; i++)
            {
                Points[i] = Points[i + 1];
                Points.Remove(i + 1);
                PointsObjects[i] = PointsObjects[i + 1];
                PointsObjects.Remove(i + 1);
            }
        }
        private void Update()
        {
            if (PointsObjects.Count <= 0) return;
            PointsObjects[_cameraMenu.currentlySelectedPoint].transform.position =
                Points[_cameraMenu.currentlySelectedPoint].Item1;
            PointsObjects[_cameraMenu.currentlySelectedPoint].transform.eulerAngles =
                Points[_cameraMenu.currentlySelectedPoint].Item2;
        }
    }

    internal abstract class SaveSystem
    {
        public static void DeleteSave(string saveName)
        {
            File.Delete(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints\" + saveName + ".json"));
        }
        public static List<string> GetSaves()
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "ModSaves"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "ModSaves"));
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints"));
            return Directory.GetFiles(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints")).Select(Path.GetFileNameWithoutExtension).ToList();
        }
        private static string PointToString((Vector3, Vector3, float, int, int, float) point)
        {
            var fString = "";
            fString += "(" + point.Item1.x + "!" + point.Item1.y + "!" + point.Item1.z + ");";
            fString += "(" + point.Item2.x + "!" + point.Item2.y + "!" + point.Item2.z + ");";
            fString += point.Item3 + ";";
            fString += point.Item4 + ";";
            fString += point.Item5 + ";";
            fString += point.Item6 + ";";

            return fString;
        }
        public static List<string> GetSaveThatContain(string contain)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "ModSaves")))
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "ModSaves"));
            }
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints")))
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints"));
            }

            return Directory.GetFiles(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints")).Where(file => file.Contains(contain)).ToList();
        }
        public static void SavePoints(string groupName, Dictionary<int, (Vector3, Vector3, float, int, int, float)> points)
        {
            SettingsValues settingsValues = new SettingsValues();
            foreach (var p in points) settingsValues.Add(p.Key, PointToString(p.Value));
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "ModSaves"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "ModSaves"));
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, @"ModSaves\CameraPoints"));
            File.WriteAllText(Path.Combine(Application.persistentDataPath, $@"ModSaves\CameraPoints\{groupName}.json"), JsonUtility.ToJson(settingsValues, true));
        }

        public static Dictionary<int, (Vector3, Vector3, float, int, int, float)> LoadPoints(string groupName)
        {
            // Load the file
            var settingsValues = JsonUtility.FromJson<SettingsValues>(File.ReadAllText(Path.Combine(Application.persistentDataPath, $@"ModSaves\CameraPoints\{groupName}.json")));
            var points = new Dictionary<int, (Vector3, Vector3, float, int, int, float)>();
            foreach (var p in settingsValues)
            {
                var split = p.Value.Split(';');
                var position = split[0].Replace("(", "").Replace(")", "").Split('!');
                var rotation = split[1].Replace("(", "").Replace(")", "").Split('!');
                points[p.Key] = (new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2])), new Vector3(float.Parse(rotation[0]), float.Parse(rotation[1]), float.Parse(rotation[2])), float.Parse(split[2]), int.Parse(split[3]), int.Parse(split[4]), float.Parse(split[5]));
            }
            return points;
        }
    }
    [Serializable] internal class SettingsValues : SerializableDictionary<int, string> { }
}