namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class WorldInspector : MouseInspectorBase
    {
        internal static Camera MainCamera;
        private static GameObject lastHitObject;

        public override void OnBeginMouseInspect()
        {
            MainCamera = Camera.main;
            List<Camera> cameras = Camera.allCameras.ToList();

            if (!MainCamera)
            {
                ExplorerCore.LogWarning("No Main Camera was found, trying to find a camera named 'Main Camera' or 'MainCamera'.");
                MainCamera = cameras.FirstOrDefault(c => c.name == "Main Camera" || c.name == "MainCamera");

                if (!MainCamera)
                {
                    ExplorerCore.LogWarning("No camera named 'Main Camera' or 'MainCamera' found, using the first camera created.");
                    MainCamera = cameras.FirstOrDefault();
                    if (!MainCamera)
                    {
                        ExplorerCore.LogWarning("No valid cameras found! Cannot inspect world!");
                        return;
                    }
                }
            }

            MouseInspector.Instance.inspectorLabelTitle.text = $"<b>World Inspector ({MainCamera.name})</b> (press <b>ESC</b> to cancel)";
            ExplorerCore.Log($"Using camera: '{MainCamera.transform.GetTransformPath(true)}'");
        }

        public override void ClearHitData()
        {
            lastHitObject = null;
        }

        public override void OnSelectMouseInspect()
        {
            InspectorManager.Inspect(lastHitObject);
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            if (!MainCamera)
                MainCamera = Camera.main;
            if (!MainCamera)
            {
                ExplorerCore.LogWarning("No Main Camera was found, unable to inspect world!");
                MouseInspector.Instance.StopInspect();
                return;
            }

            Ray ray = MainCamera.ScreenPointToRay(mousePos);
            Physics.Raycast(ray, out RaycastHit hit, 1000f);

            if (hit.transform)
                OnHitGameObject(hit.transform.gameObject);
            else if (lastHitObject)
                MouseInspector.Instance.ClearHitData();
        }

        internal void OnHitGameObject(GameObject obj)
        {
            if (obj != lastHitObject)
            {
                lastHitObject = obj;
                MouseInspector.Instance.objNameLabel.text = $"<b>Click to Inspect:</b> <color=cyan>{obj.name}</color>";
                MouseInspector.Instance.objPathLabel.text = $"Path: {obj.transform.GetTransformPath(true)}";
            }
        }

        public override void OnEndInspect()
        {
            // not needed
        }
    }
}
