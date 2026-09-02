using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamChase.Editor
{
    public static class FlySetup
    {
        const float SmokeDeltaTime = 1f / 60f;
        const float FlySpeed = 4f;
        const float RetreatX = 8f;
        const float ViewportEpsilon = 0.02f;

        static PlayerMotor _smokeMotor;
        static ButterflyController _smokeButterfly;
        static ViewportBounds _smokeBounds;
        static Animator _smokeAnimator;
        static Vector3 _smokeButterflyStart;
        static float _smokeTargetX;

        [MenuItem("DreamChase/Setup P4 Fly")]
        public static void Setup()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                if (cameraObject != null)
                    mainCamera = cameraObject.GetComponent<Camera>();
            }

            GameObject cat = GameObject.Find("Cat_Ganbei");
            GameObject butterfly = GameObject.Find("Butterfly");
            if (mainCamera == null || cat == null)
            {
                Debug.LogError("Main Camera or Cat_Ganbei was not found.");
                return;
            }

            ViewportBounds bounds = mainCamera.GetComponent<ViewportBounds>();
            if (bounds == null)
                bounds = Undo.AddComponent<ViewportBounds>(mainCamera.gameObject);

            PlayerMotor motor = cat.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                SerializedObject motorSo = new SerializedObject(motor);
                motorSo.FindProperty("viewportBounds").objectReferenceValue = bounds;
                motorSo.FindProperty("flySpeed").floatValue = FlySpeed;
                motorSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(motor);
            }

            Animator animator = cat.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                Debug.LogWarning("Cat_Ganbei is missing an Animator controller with IsFlying.");

            if (butterfly != null)
            {
                Transform retreatPoint = EnsureRetreatPoint(butterfly.transform.position);
                ButterflyController controller = butterfly.GetComponent<ButterflyController>();
                if (controller == null)
                    controller = Undo.AddComponent<ButterflyController>(butterfly);

                SerializedObject controllerSo = new SerializedObject(controller);
                controllerSo.FindProperty("retreatTarget").objectReferenceValue = retreatPoint;
                controllerSo.FindProperty("retreatWorldPosition").vector3Value = retreatPoint.position;
                controllerSo.FindProperty("retreatSpeed").floatValue = 2.5f;
                controllerSo.FindProperty("idleOnly").boolValue = true;
                controllerSo.FindProperty("flyOnPlay").boolValue = true;
                controllerSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
                ApplyPrefab(butterfly);
            }

            ApplyPrefab(cat);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("P4 fly is wired (four-side clamp, flySpeed, butterfly fly-on-play).");
        }

        [MenuItem("DreamChase/P4 Smoke Fly Move")]
        public static void SmokeFlyMove()
        {
            if (!TryBindSmoke("fly move"))
                return;

            _smokeMotor.EnterFlying();
            Vector3 start = _smokeMotor.transform.position;
            RefreshFlyAnimator();

            _smokeMotor.EditorApplyFlyingInput(new PlayerInputSnapshot(new Vector2(1f, 1f), false, false), SmokeDeltaTime);
            float diagonalSpeed = new Vector2(_smokeMotor.Velocity.x, _smokeMotor.Velocity.y).magnitude;
            TeleportMotor(start);
            _smokeMotor.EnterFlying();

            Vector3 afterRight = FlyTicks(Vector2.right, 30);
            Vector3 afterUp = FlyTicks(Vector2.up, 30);
            Vector3 afterDiag = FlyTicks(new Vector2(1f, 1f), 30);

            bool flying = _smokeMotor.Phase == GamePhase.Flying;
            bool isFlyingParam = _smokeAnimator != null && _smokeAnimator.GetBool("IsFlying");
            bool flyState = IsInFlyState();
            bool diagonalNormalized = Mathf.Abs(diagonalSpeed - FlySpeed) < 0.05f;

            Debug.Log(
                "P4 smoke fly start=" + start
                + " afterRight=" + afterRight
                + " afterUp=" + afterUp
                + " afterDiag=" + afterDiag
                + " flying=" + flying
                + " isFlyingParam=" + isFlyingParam
                + " flyState=" + flyState
                + " diagonalSpeed=" + diagonalSpeed
                + " diagonalNormalized=" + diagonalNormalized);
        }

        [MenuItem("DreamChase/P4 Smoke Viewport Clamp")]
        public static void SmokeViewportClamp()
        {
            if (!TryBindSmoke("viewport clamp"))
                return;

            _smokeMotor.EnterFlying();
            FlyTicks(Vector2.up, 180);
            bool topInside = IsInsideViewport();
            Vector3 topPos = _smokeMotor.transform.position;

            FlyTicks(Vector2.left, 180);
            bool leftInside = IsInsideViewport();
            Vector3 leftPos = _smokeMotor.transform.position;

            FlyTicks(Vector2.right, 240);
            bool rightInside = IsInsideViewport();
            Vector3 rightPos = _smokeMotor.transform.position;

            FlyTicks(Vector2.down, 240);
            bool bottomInside = IsInsideViewport();
            Vector3 bottomPos = _smokeMotor.transform.position;

            Debug.Log(
                "P4 smoke clamp topInside=" + topInside + " top=" + topPos
                + " leftInside=" + leftInside + " left=" + leftPos
                + " rightInside=" + rightInside + " right=" + rightPos
                + " bottomInside=" + bottomInside + " bottom=" + bottomPos
                + " allInside=" + (topInside && leftInside && rightInside && bottomInside));
        }

        [MenuItem("DreamChase/P4 Smoke Butterfly Arrive")]
        public static void SmokeButterflyArrive()
        {
            if (!TryBindSmoke("butterfly arrive"))
                return;

            if (_smokeButterfly == null)
            {
                Debug.LogError("ButterflyController was not found.");
                return;
            }

            _smokeTargetX = RetreatTargetX(_smokeButterfly);
            _smokeButterflyStart = new Vector3(_smokeTargetX - 3f, _smokeButterfly.transform.position.y, _smokeButterfly.transform.position.z);
            _smokeButterfly.EditorResetAndFly(_smokeButterflyStart);

            int ticks = 180;
            for (int i = 0; i < ticks; i++)
                _smokeButterfly.EditorTickRetreat(SmokeDeltaTime);

            float x = _smokeButterfly.transform.position.x;
            float deltaX = x - _smokeButterflyStart.x;
            bool arrived = _smokeButterfly.HasReachedRetreatPoint;
            bool atTarget = Mathf.Abs(x - _smokeTargetX) <= 0.05f;
            Debug.Log(
                "P4 butterfly after ticks startX=" + _smokeButterflyStart.x
                + " x=" + x
                + " deltaX=" + deltaX
                + " targetX=" + _smokeTargetX
                + " arrived=" + arrived
                + " atTarget=" + atTarget
                + " retreating=" + _smokeButterfly.IsRetreating);
        }

        static bool TryBindSmoke(string label)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode before running the P4 " + label + " smoke test.");
                return false;
            }

            _smokeMotor = Object.FindObjectOfType<PlayerMotor>();
            _smokeButterfly = Object.FindObjectOfType<ButterflyController>();
            _smokeBounds = Object.FindObjectOfType<ViewportBounds>();
            _smokeAnimator = _smokeMotor != null ? _smokeMotor.GetComponent<Animator>() : null;
            if (_smokeMotor == null)
            {
                Debug.LogError("PlayerMotor was not found.");
                return false;
            }

            return true;
        }

        static Vector3 FlyTicks(Vector2 move, int ticks)
        {
            PlayerInputSnapshot input = new PlayerInputSnapshot(move, false, false);
            for (int i = 0; i < ticks; i++)
                _smokeMotor.EditorApplyFlyingInput(input, SmokeDeltaTime);

            return _smokeMotor.transform.position;
        }

        static void RefreshFlyAnimator()
        {
            if (_smokeAnimator == null)
                return;

            _smokeAnimator.SetBool("IsFlying", true);
            _smokeAnimator.SetBool("IsGrounded", true);
            _smokeAnimator.Update(0f);
            _smokeAnimator.Update(0.25f);
        }

        static bool IsInFlyState()
        {
            if (_smokeAnimator == null)
                return false;

            AnimatorStateInfo info = _smokeAnimator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = _smokeAnimator.GetNextAnimatorStateInfo(0);
            return info.IsName("Fly")
                || info.IsName("Base Layer.Fly")
                || next.IsName("Fly")
                || next.IsName("Base Layer.Fly");
        }

        static void TeleportMotor(Vector3 position)
        {
            CharacterController controller = _smokeMotor.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            _smokeMotor.transform.position = position;

            if (controller != null)
                controller.enabled = true;
        }

        static bool IsInsideViewport()
        {
            if (_smokeBounds == null)
                return false;

            CharacterController controller = _smokeMotor.GetComponent<CharacterController>();
            if (controller == null)
                return false;

            Vector3 min;
            Vector3 max;
            if (!_smokeBounds.TryGetWorldRect(controller.transform.position.z, out min, out max))
                return false;

            Bounds bounds = controller.bounds;
            return bounds.min.x >= min.x - ViewportEpsilon
                && bounds.max.x <= max.x + ViewportEpsilon
                && bounds.min.y >= min.y - ViewportEpsilon
                && bounds.max.y <= max.y + ViewportEpsilon;
        }

        static float RetreatTargetX(ButterflyController butterfly)
        {
            SerializedObject so = new SerializedObject(butterfly);
            Transform target = so.FindProperty("retreatTarget").objectReferenceValue as Transform;
            if (target != null)
                return target.position.x;

            return so.FindProperty("retreatWorldPosition").vector3Value.x;
        }

        static Transform EnsureRetreatPoint(Vector3 butterflyPosition)
        {
            GameObject point = GameObject.Find("ButterflyRetreatPoint");
            if (point != null)
                return point.transform;

            GameObject world = GameObject.Find("World");
            point = new GameObject("ButterflyRetreatPoint");
            Undo.RegisterCreatedObjectUndo(point, "Create ButterflyRetreatPoint");
            if (world != null)
                point.transform.SetParent(world.transform, true);

            point.transform.position = new Vector3(RetreatX, butterflyPosition.y, butterflyPosition.z);
            return point.transform;
        }

        static void ApplyPrefab(GameObject instance)
        {
            if (instance == null || PrefabUtility.GetPrefabInstanceStatus(instance) != PrefabInstanceStatus.Connected)
                return;

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        }
    }
}
