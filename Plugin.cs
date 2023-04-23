using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using BepInEx;
using System;

namespace CameraPlacements
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private CameraMenu _cameraMenu;
        private MouseLook _locker0;
        private MouseLook _locker1;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Scn2_CheckpointBravo") || scene.name.Equals("MainMenu"))
            {
                _cameraMenu = new GameObject("CameraMenu").AddComponent<CameraMenu>();
                _cameraMenu.GetComponent<CameraMenu>().enabled = false;
            }

            if (!scene.name.Equals("Scn2_CheckpointBravo")) return;
            _locker0 = GameObject.Find("/First Person Controller").GetComponent<MouseLook>();
            _locker1 = GameObject.Find("/First Person Controller/Main Camera").GetComponent<MouseLook>();
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

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == "Scn2_CheckpointBravo" ||
                SceneManager.GetActiveScene().name == "MainMenu")
            {
                if (Input.GetKeyDown(KeyCode.F10))
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
    }

    class CameraMenu : MonoBehaviour
    {
        private Camera _newCamera;
        private CameraManager _cameraManager;
        private PointsManager _pointsManager;
        public int currentlySelectedPoint = -1;
        public LineRenderer lineRenderer;
        private Gradient _gradient;
        private GradientColorKey[] _colorKey;
        private (Vector3, Vector3, float, int, int) _lastPoint;
        private void Awake()
        {
            _newCamera = new GameObject("FreeCamera").AddComponent<Camera>();
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                _newCamera.transform.position = GameObject.Find("/Main Camera").transform.position;
            } else if (SceneManager.GetActiveScene().name == "Scn2_CheckpointBravo")
            {
                _newCamera.transform.position = 
                    GameObject.Find("/First Person Controller/Main Camera").transform.position;
            }
            _cameraManager = _newCamera.gameObject.AddComponent<CameraManager>();
            _pointsManager = _newCamera.gameObject.AddComponent<PointsManager>();
            _newCamera.gameObject.AddComponent<Rigidbody>().useGravity = false;
            _newCamera.enabled = false;
            lineRenderer = _newCamera.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _gradient = new Gradient();
            _gradient.SetKeys(
                new[] { new(Color.red
                     , 0.0f), new(Color.green, 0.5f), new GradientColorKey(Color.red, 1.0f) },
                new[] { new(100, 0.0f), new(100, 0.0f), new GradientAlphaKey(100, 1.0f) }
            );
            lineRenderer.colorGradient = _gradient;
        }   

        private void OnGUI()
        {
            if (_cameraManager.animStarted) return;
            GUI.Box(new Rect(5, Screen.height - 305, 500, 300), "Camera Control");
            if (GUI.Button(new Rect(10, Screen.height - 70, 100, 60),"Switch view\nto freecam"))
            {
                _newCamera.enabled = !_newCamera.enabled;
            }
            if (GUI.Button(new Rect(10, Screen.height - 135, 100, 60), "Launch\nanimation") &&
                _pointsManager.Points.Count > 1)
            {
                _cameraManager.StartAnimation();
            }

            var transform1 = _newCamera.transform;
            var position = transform1.position;
            var rotation = transform1.eulerAngles;
            
            GUI.Label(new Rect(115, Screen.height - 60, 50, 25), "Position");
            _newCamera.transform.position = new Vector3(
                float.Parse(GUI.TextField(new Rect(170, Screen.height - 60, 60, 20), position.x + "")),
                float.Parse(GUI.TextField(new Rect(235, Screen.height - 60, 60, 20), position.y + "")),
                float.Parse(GUI.TextField(new Rect(300, Screen.height - 60, 60, 20), position.z + "")));
            GUI.Label(new Rect(115, Screen.height - 40, 50, 25), "Rotation");
            _newCamera.transform.eulerAngles = new Vector3(
                float.Parse(GUI.TextField(new Rect(170, Screen.height - 40, 60, 20), rotation.x + "")),
                float.Parse(GUI.TextField(new Rect(235, Screen.height - 40, 60, 20), rotation.y + "")),
                float.Parse(GUI.TextField(new Rect(300, Screen.height - 40, 60, 20), rotation.z + "")));
            _cameraManager.zLocked = GUI.Toggle(new Rect(365, Screen.height - 40, 100, 20), _cameraManager.zLocked,
                "Lock X&Z axis");
            GUI.Label(new Rect(395, Screen.height - 305, 150, 20), "Camera FOV: "+ _newCamera.fieldOfView);
            GUI.Label(new Rect(380, Screen.height - 288, 150, 20), "Freecam Speed: "+ _cameraManager.mSpeed);
            
            var lastSelectedPoint = currentlySelectedPoint;
            if (_pointsManager.Points.Count > 0)
            {
                _lastPoint = _pointsManager.Points[currentlySelectedPoint];
            }
            GUI.Label(new Rect(10, Screen.height - 300, 150, 20), "Tracks Points: "+_pointsManager.Points.Count);
            if (GUI.Button(new Rect(10, Screen.height - 275, 150, 20), "Add Point"))
            {
                if (_pointsManager.Points.Count > currentlySelectedPoint) currentlySelectedPoint++;
                var cameraTransform = _newCamera.transform;
                _pointsManager.AddPoint(currentlySelectedPoint, cameraTransform.position, cameraTransform.eulerAngles,
                    _newCamera.fieldOfView, _cameraManager.mSpeed);
                lineRenderer.positionCount = _pointsManager.Points.Count;
                for (var i = 0; i < _pointsManager.Points.Count; i++)
                {
                    lineRenderer.SetPosition(i, _pointsManager.Points[i].Item1);
                }
            }

            if (GUI.Button(new Rect(345, Screen.height - 265, 150, 20), "Remove Point") &&
                _pointsManager.Points.Count > 0)
            {
                _pointsManager.RemovePoint(currentlySelectedPoint);
                if (currentlySelectedPoint > 0) currentlySelectedPoint--;
                lineRenderer.positionCount = _pointsManager.Points.Count;
            }

            GUI.Label(new Rect(175, Screen.height - 287, 180, 20), "Currently Selected Point: "+currentlySelectedPoint);
            currentlySelectedPoint = (int)GUI.HorizontalSlider(new Rect(180, Screen.height - 265, 150, 20),
                currentlySelectedPoint, 0,
                MinClamp(_pointsManager.Points.Count - 1, 0));
            if (_pointsManager.Points.Count > 0)
            {
                GUI.Label(new Rect(10, Screen.height - 250, 50, 25), "Position");
                GUI.Label(new Rect(10, Screen.height - 230, 50, 25), "Rotation");
                GUI.Label(new Rect(10, Screen.height - 210, 50, 25), "FOV");
                GUI.Label(new Rect(10, Screen.height - 190, 50, 25), "Speed");
                var point = _pointsManager.Points[currentlySelectedPoint];
                float.TryParse(
                    GUI.TextField(new Rect(65, Screen.height - 230, 60, 20), point.Item2.x + ""), out var prx);
                float.TryParse(
                    GUI.TextField(new Rect(130, Screen.height - 230, 60, 20), point.Item2.y + ""), out var pry);
                float.TryParse(
                    GUI.TextField(new Rect(195, Screen.height - 230, 60, 20), point.Item2.z + ""), out var prz);
                float.TryParse(GUI.TextField(new Rect(65, Screen.height - 250, 60, 20), point.Item1.x + ""),
                    out var ppx);
                float.TryParse(GUI.TextField(new Rect(130, Screen.height - 250, 60, 20), point.Item1.y + ""),
                    out var ppy);
                float.TryParse(GUI.TextField(new Rect(195, Screen.height - 250, 60, 20), point.Item1.z + ""), 
                    out var ppz);
                float.TryParse(GUI.TextField(new Rect(65, Screen.height - 210, 60, 20), point.Item3 + ""),
                    out var pFov);
                int.TryParse(GUI.TextField(new Rect(65, Screen.height - 190, 60, 20), point.Item4 + ""),
                    out var pSpeed);

                var baseType = _pointsManager.Points[currentlySelectedPoint].Item5;
                GUI.Label(new Rect(260, Screen.height - 230, 150, 20), "Animation mode:");
                if (GUI.Toggle(new Rect(360, Screen.height - 230, 30, 20),
                        baseType == 0, "▲")) baseType = 0;
                if (GUI.Toggle(new Rect(390, Screen.height - 230, 30, 20),
                        baseType == 1, "●")) baseType = 1;
                if (GUI.Toggle(new Rect(420, Screen.height - 230, 30, 20),
                        baseType == 2, "■")) baseType = 2;
                
                _pointsManager.Points[currentlySelectedPoint] = (
                    new Vector3(ppx, ppy, ppz), 
                    new Vector3( Clamp(prx, 359, 0), Clamp(pry, 359, 0), Clamp(prz, 359, 0)),
                    (int)Clamp(pFov, 175, 5),
                    (int)Clamp(pSpeed, 950, 0), baseType);
                
                    lineRenderer.SetPosition(currentlySelectedPoint, _pointsManager.Points[currentlySelectedPoint].Item1);
            }

            if (_pointsManager.Points.Count == 0) return;
            if (currentlySelectedPoint == lastSelectedPoint && _pointsManager.Points[currentlySelectedPoint] != _lastPoint)
            {
                var (fov, speed) = (_pointsManager.Points[currentlySelectedPoint].Item3,
                    _pointsManager.Points[currentlySelectedPoint].Item4);
                _pointsManager.PointsObjects[currentlySelectedPoint].transform.localScale = new Vector3(fov / 60*100, fov / 60*100, Clamp(speed, 150, 0) / 30*100);
            }
            if (lastSelectedPoint == currentlySelectedPoint || lastSelectedPoint == -1) return;
            _colorKey = new[] { new GradientColorKey(Color.magenta, 0.0f), new GradientColorKey(Color.magenta, 1.0f) };
            _gradient.SetKeys(_colorKey,new[] { new(100, 0.0f), new GradientAlphaKey(100, 1.0f)});
            lineRenderer.colorGradient = _gradient;
            
            if (_pointsManager.PointsObjects.Count != lastSelectedPoint)
            {
                _pointsManager.PointsObjects[lastSelectedPoint].GetComponent<MeshRenderer>().material.color = Color.white;
                _pointsManager.PointsObjects[currentlySelectedPoint].GetComponent<MeshRenderer>().material.color = Color.blue;
            }
            else
            {
                _pointsManager.PointsObjects[currentlySelectedPoint].GetComponent<MeshRenderer>().material.color = Color.blue;
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

    class CameraManager : MonoBehaviour
    {
        public int mSpeed = 10;
        private Rigidbody _mRigidbody;
        private Camera _mCamera;
        private CameraMenu _mCameraMenu;
        private PointsManager _pointsManager;
        private float _originalDistance;
        public bool zLocked;
        public bool animStarted;
        private float _startTime;
        private AnimationCurve _curve;

        private void Start()
        {
            _mRigidbody = gameObject.GetComponent<Rigidbody>();
            _mCamera = gameObject.GetComponent<Camera>();
            _mCameraMenu = GameObject.Find("CameraMenu").GetComponent<CameraMenu>();
            _pointsManager = gameObject.GetComponent<PointsManager>();
        }

        public void StartAnimation()
        {
            animStarted = true;
            var transform0 = gameObject.transform;
            transform0.position = _pointsManager.Points[0].Item1;
            transform0.rotation = Quaternion.Euler(_pointsManager.Points[0].Item2);
            _mCameraMenu.currentlySelectedPoint = 1;
            _mCameraMenu.lineRenderer.enabled = false;
            var p0 = transform0.position;
            var p1 = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
            _originalDistance = (float)Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) + Math.Pow(p1.z - p0.z, 2));
            _startTime = Time.time;
            for (var i = 0; i < _pointsManager.Points.Count; i++)
            {
                _pointsManager.PointsObjects[i].SetActive(false);
            }
            SetCurve(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item5);
        }

        private void SetCurve(int type)
        {
            _curve = new AnimationCurve();
            if (type == 0)
            {
                _curve.AddKey(0, 0);
                _curve.AddKey(1, 1);
            }
            else if (type == 1)
            {
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
                _curve.AddKey(1.0f, 1.0f);
            } else if (type == 2)
            {
                _curve.AddKey(0, 0);
                _curve.AddKey(0.99f, 0);
                _curve.AddKey(1, 1);
            }
            else
            {
                Debug.LogError("Curve type not found");
            }
        }
        private void Update()
        {
            if (animStarted)
            {
                var p0 = gameObject.transform.position;
                var p1 = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
                var d = Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) + Math.Pow(p1.z - p0.z, 2));
                _mCamera.fieldOfView = Mathf.Lerp(
                    _pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item3,
                    _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item3,
                    (float)(1 - d / _originalDistance));
                
                var curveValue = _curve.Evaluate((float)(1 - d / _originalDistance));
                gameObject.transform.rotation = Quaternion.Lerp(
                    Quaternion.Euler(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item2),
                    Quaternion.Euler(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item2), curveValue);
                    
                
                if (Math.Round(d) == 0 || (_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item4 > 100 && d < 5))
                {
                    _mCamera.fieldOfView = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item3;
                    _mCameraMenu.currentlySelectedPoint++;
                    if (_mCameraMenu.currentlySelectedPoint == _pointsManager.Points.Count)
                    {
                        animStarted = false;
                        _mCameraMenu.currentlySelectedPoint = 0;
                        _mCameraMenu.lineRenderer.enabled = true;
                        for (var i = 0; i < _pointsManager.Points.Count; i++)
                        {
                            _pointsManager.PointsObjects[i].SetActive(true);
                        }
                    }
                    else
                    {
                        p1 = _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1;
                        _originalDistance = (float)Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) + Math.Pow(p1.z - p0.z, 2));
                        _startTime = Time.time;
                        SetCurve(_pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item5);
                    }
                }
                else
                {
                    curveValue = _curve.Evaluate((Time.time - _startTime) * _pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item4 / _originalDistance);
                        
                    gameObject.transform.position = Vector3.Lerp(
                        _pointsManager.Points[_mCameraMenu.currentlySelectedPoint - 1].Item1,
                        _pointsManager.Points[_mCameraMenu.currentlySelectedPoint].Item1, curveValue);
                }
                return;
            }
            var rot = transform.rotation;
            if (Input.GetKey(KeyCode.UpArrow))
            {
                _mRigidbody.velocity = transform.forward * mSpeed;
            } else if (Input.GetKey(KeyCode.DownArrow))
            {
                _mRigidbody.velocity = -transform.forward * mSpeed;
            } else if (Input.GetKey(KeyCode.RightArrow))
            {
                _mRigidbody.velocity = transform.right * mSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                _mRigidbody.velocity = -transform.right * mSpeed;
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
                switch (Input.mouseScrollDelta.y)
                {
                    case > 0:
                        if (_mCamera.fieldOfView < 175) _mCamera.fieldOfView += 5;
                        break;
                    case < 0:
                        if (_mCamera.fieldOfView > 5) _mCamera.fieldOfView -= 5;
                        break;
                }
            } 
            else
            {
                switch (Input.mouseScrollDelta.y)
                {
                    case > 0:
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                        {
                            if (mSpeed < 980) mSpeed += 20;
                        }
                        else
                        {
                            if (mSpeed < 995) mSpeed += 5;
                        }
                        break;
                    case < 0:
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                        {
                            if (mSpeed > 20) mSpeed -= 20;
                        }
                        else
                        {
                            if (mSpeed > 5) mSpeed -= 5;
                        }
                        break;
                }
            }
        }
    }

    class PointsManager : MonoBehaviour
    {
        public readonly Dictionary<int, (Vector3, Vector3, float, int, int)> Points = new();
        public readonly Dictionary<int, GameObject> PointsObjects = new();
        private CameraMenu _cameraMenu;
        private GameObject _conePrefab;
        private void Awake()
        {
            _cameraMenu = GameObject.Find("CameraMenu").GetComponent<CameraMenu>();
            var coneBundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, "assets"));
            if (coneBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle!");
                return;
            }
            _conePrefab = coneBundle.LoadAsset<GameObject>("pointer-cone");
            coneBundle.Unload(false);
        }
        public void AddPoint(int index, Vector3 position, Vector3 rotation, float fov, int speed)
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
            Points[index] = (position, rotation, fov, speed, 0);
            PointsObjects[index] = Instantiate(_conePrefab);
            PointsObjects[index].transform.position = position;
            PointsObjects[index].transform.eulerAngles = rotation;
            PointsObjects[index].transform.localScale = new Vector3(fov / 60*100, fov / 60*100, Clamp(speed, 150, 0) / 30*100);
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
}
