using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            SceneManager.sceneLoaded += OnSceneLoaded;
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
            if (SceneManager.GetActiveScene().name != "Scn2_CheckpointBravo" &&
                SceneManager.GetActiveScene().name != "MainMenu") return;
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

    class CameraMenu : MonoBehaviour
    {
        private Camera _newCamera;
        private CameraManager _cameraManager;
        private PointsManager _pointsManager;
        public int currentlySelectedPoint = -1;
        private void Awake()
        {
            _newCamera = new GameObject("NewCamera").AddComponent<Camera>();
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
        }   

        private void OnGUI()
        {
            GUI.Box(new Rect(5, Screen.height - 305, 500, 300), "Camera Control");
            if (GUI.Button(new Rect(10, Screen.height - 70, 100, 60),"Switch view\nto freecam"))
            {
                _newCamera.enabled = !_newCamera.enabled;
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
            GUI.Label(new Rect(10, Screen.height - 300, 150, 20), "Tracks Points: "+_pointsManager.Points.Count);
            if (GUI.Button(new Rect(10, Screen.height - 275, 150, 20), "Add Point"))
            {
                if (_pointsManager.Points.Count > currentlySelectedPoint) currentlySelectedPoint++;
                var cameraTransform = _newCamera.transform;
                _pointsManager.AddPoint(currentlySelectedPoint, cameraTransform.position, cameraTransform.eulerAngles,
                    _newCamera.fieldOfView);
            }
            if (GUI.Button(new Rect(345, Screen.height - 265, 150, 20), "Remove Point"))
            {
                _pointsManager.RemovePoint(currentlySelectedPoint);
                if (currentlySelectedPoint > 0) currentlySelectedPoint--;
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
                var point = _pointsManager.Points[currentlySelectedPoint];
                float.TryParse(
                    GUI.TextField(new Rect(65, Screen.height - 230, 60, 20), point.Item2.x + ""), out var prx);
                float.TryParse(
                    GUI.TextField(new Rect(130, Screen.height - 230, 60, 20), point.Item2.y + ""), out var pry);
                float.TryParse(
                    GUI.TextField(new Rect(195, Screen.height - 230, 60, 20), point.Item2.z + ""), out var prz);
                _pointsManager.Points[currentlySelectedPoint] = (
                    new Vector3(
                        float.Parse(GUI.TextField(new Rect(65, Screen.height - 250, 60, 20), point.Item1.x + "")), 
                        float.Parse(GUI.TextField(new Rect(130, Screen.height - 250, 60, 20), point.Item1.y + "")), 
                        float.Parse(GUI.TextField(new Rect(195, Screen.height - 250, 60, 20), point.Item1.z + ""))), 
                    new Vector3( Clamp(prx, 359, 0), Clamp(pry, 359, 0), Clamp(prz, 359, 0)), 
                    float.Parse(GUI.TextField(new Rect(65, Screen.height - 210, 60, 20), point.Item3 + "")));
                
            }

            if (lastSelectedPoint == currentlySelectedPoint || lastSelectedPoint == -1) return;
            if (_pointsManager.PointsObjects.Count != lastSelectedPoint)
            {
                _pointsManager.PointsObjects[lastSelectedPoint].GetComponent<MeshRenderer>().material.color = Color.white;
                _pointsManager.PointsObjects[currentlySelectedPoint].GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else
            {
                _pointsManager.PointsObjects[currentlySelectedPoint].GetComponent<MeshRenderer>().material.color = Color.red;
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
        public bool zLocked;

        private void Start()
        {
            _mRigidbody = gameObject.GetComponent<Rigidbody>();
            _mCamera = gameObject.GetComponent<Camera>();
        }

        private void Update()
        {
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
        public readonly Dictionary<int, (Vector3, Vector3, float)> Points = new();
        public readonly Dictionary<int, GameObject> PointsObjects = new();
        private CameraMenu _cameraMenu;
        private GameObject _conePrefab;
        private Camera _camera;
        private void Awake()
        {
            _cameraMenu = GameObject.Find("CameraMenu").GetComponent<CameraMenu>();
            _camera = gameObject.GetComponent<Camera>();
            var coneBundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, "assets"));
                if (coneBundle == null)
                {
                    Debug.Log("Failed to load AssetBundle!");
                    return;
                }
                _conePrefab = coneBundle.LoadAsset<GameObject>("pointer-cone");
        }

        public void AddPoint(int index, Vector3 position, Vector3 rotation, float fov)
        {
            Points[index] = (position, rotation, fov);
            PointsObjects[index] = Instantiate(_conePrefab);
            PointsObjects[index].transform.position = position;
            PointsObjects[index].transform.eulerAngles = rotation;
        }

        public void RemovePoint(int index)
        {
            Destroy(PointsObjects[index]);
            Points.Remove(index);
            PointsObjects.Remove(index);
            for (var i = index; i < Points.Count; i++)
            {
                Debug.Log(i);
                Points[i] = Points[i + 1];
                Points.Remove(i + 1);
                PointsObjects[i] = PointsObjects[i + 1];
                PointsObjects.Remove(i + 1);
            }
        }
        private void Update()
        {
            if (PointsObjects.Count > 0)
            {
                PointsObjects[_cameraMenu.currentlySelectedPoint].transform.position =
                    Points[_cameraMenu.currentlySelectedPoint].Item1;
                PointsObjects[_cameraMenu.currentlySelectedPoint].transform.eulerAngles =
                    Points[_cameraMenu.currentlySelectedPoint].Item2;
            }
        }
    }
}
