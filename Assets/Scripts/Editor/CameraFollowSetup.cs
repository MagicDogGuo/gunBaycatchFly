using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamChase.Editor
{
    public static class CameraFollowSetup
    {
        const float SmokeDeltaTime = 1f / 60f;

        [MenuItem("DreamChase/Setup P2 Camera")]
        public static void Setup()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                if (cameraObject != null)
                    mainCamera = cameraObject.GetComponent<Camera>();
            }

            if (mainCamera == null)
            {
                Debug.LogError("Main Camera was not found.");
                return;
            }

            GameObject cat = GameObject.Find("Cat_Ganbei");
            if (cat == null)
            {
                Debug.LogError("Cat_Ganbei was not found.");
                return;
            }

            ViewportBounds bounds = mainCamera.GetComponent<ViewportBounds>();
            if (bounds == null)
                bounds = Undo.AddComponent<ViewportBounds>(mainCamera.gameObject);

            SideScrollCamera follow = mainCamera.GetComponent<SideScrollCamera>();
            if (follow == null)
                follow = Undo.AddComponent<SideScrollCamera>(mainCamera.gameObject);

            SerializedObject followSo = new SerializedObject(follow);
            followSo.FindProperty("target").objectReferenceValue = cat.transform;
            followSo.FindProperty("leftFollowNormalizedX").floatValue = 0.28f;
            followSo.FindProperty("rightFollowNormalizedX").floatValue = 0.72f;
            followSo.FindProperty("lookAheadDamping").floatValue = 0.15f;
            followSo.FindProperty("neverScrollLeft").boolValue = false;
            followSo.ApplyModifiedProperties();

            PlayerMotor motor = cat.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                SerializedObject motorSo = new SerializedObject(motor);
                motorSo.FindProperty("viewportBounds").objectReferenceValue = bounds;
                motorSo.FindProperty("clampGroundLeft").boolValue = true;
                motorSo.ApplyModifiedProperties();
            }

            AspectRatioEnforcer enforcer = Object.FindObjectOfType<AspectRatioEnforcer>();
            if (enforcer != null)
            {
                SerializedObject enforcerSo = new SerializedObject(enforcer);
                enforcerSo.FindProperty("targetCamera").objectReferenceValue = mainCamera;
                enforcerSo.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(mainCamera.gameObject);
            if (motor != null)
                EditorUtility.SetDirty(motor);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("P2 camera follow is on Main Camera (dead-zone left/right).");
        }

        [MenuItem("DreamChase/P2 Smoke Camera Follow")]
        public static void SmokeCameraFollow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode before running the P2 camera smoke test.");
                return;
            }

            PlayerMotor motor = Object.FindObjectOfType<PlayerMotor>();
            SideScrollCamera follow = Object.FindObjectOfType<SideScrollCamera>();
            Camera cam = follow != null ? follow.GetComponent<Camera>() : Camera.main;
            if (motor == null || follow == null || cam == null)
            {
                Debug.LogError("PlayerMotor or SideScrollCamera was not found.");
                return;
            }

            Transform cat = motor.transform;
            float startCamX = follow.transform.position.x;
            Vector3 startViewport = cam.WorldToViewportPoint(cat.position);
            Debug.Log("P2 smoke start camX=" + startCamX + " player=" + cat.position + " viewportX=" + startViewport.x);

            PlayerInputSnapshot walkLeft = new PlayerInputSnapshot(Vector2.left, false, false);
            for (int i = 0; i < 40; i++)
            {
                motor.EditorApplyGroundInput(walkLeft, SmokeDeltaTime);
                follow.EditorFollowNow(SmokeDeltaTime);
            }

            float afterLeftCamX = follow.transform.position.x;
            Debug.Log("P2 smoke after left camX=" + afterLeftCamX + " player=" + cat.position + " camDelta=" + (afterLeftCamX - startCamX));

            PlayerInputSnapshot runRight = new PlayerInputSnapshot(Vector2.right, true, false);
            for (int i = 0; i < 90; i++)
            {
                motor.EditorApplyGroundInput(runRight, SmokeDeltaTime);
                follow.EditorFollowNow(SmokeDeltaTime);
            }

            float afterRightCamX = follow.transform.position.x;
            Vector3 afterRightViewport = cam.WorldToViewportPoint(cat.position);
            Debug.Log("P2 smoke after right camX=" + afterRightCamX + " player=" + cat.position + " viewportX=" + afterRightViewport.x + " camDelta=" + (afterRightCamX - startCamX));

            PlayerInputSnapshot runLeft = new PlayerInputSnapshot(Vector2.left, true, false);
            for (int i = 0; i < 90; i++)
            {
                motor.EditorApplyGroundInput(runLeft, SmokeDeltaTime);
                follow.EditorFollowNow(SmokeDeltaTime);
            }

            float afterBackCamX = follow.transform.position.x;
            Vector3 afterBackViewport = cam.WorldToViewportPoint(cat.position);
            Debug.Log("P2 smoke after back camX=" + afterBackCamX + " player=" + cat.position + " viewportX=" + afterBackViewport.x + " camDeltaFromRight=" + (afterBackCamX - afterRightCamX));
        }
    }
}
