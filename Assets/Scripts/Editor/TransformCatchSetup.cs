using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamChase.Editor
{
    public static class TransformCatchSetup
    {
        const string BurstPrefabPath = "Assets/Prefabs/Fx/TransformBurst.prefab";
        const string PlayerLayerName = "Player";
        const string ButterflyLayerName = "Butterfly";
        const float CatchScale = 1.25f;
        const float RetreatX = 8f;

        static GameDirector _smokeDirector;
        static PlayerMotor _smokeMotor;
        static ButterflyController _smokeButterfly;
        static Vector3 _smokeStart;
        static Vector3 _smokeButterflyStart;
        static int _walkFramesLeft;
        static double _nextMoveAt;
        static double _watchUntil;
        static bool _watching;

        [MenuItem("DreamChase/Setup P3 Transform")]
        public static void Setup()
        {
            ConfigureLayerCollision();

            GameObject directorObject = GameObject.Find("GameDirector");
            if (directorObject == null)
            {
                Debug.LogError("GameDirector was not found.");
                return;
            }

            GameObject cat = GameObject.Find("Cat_Ganbei");
            GameObject butterfly = GameObject.Find("Butterfly");
            GameObject burstObject = FindChild(FindTransform("Fx"), "TransformBurst");
            if (cat == null || butterfly == null || burstObject == null)
            {
                Debug.LogError("Cat_Ganbei, Butterfly, or TransformBurst was not found.");
                return;
            }

            Transform retreatPoint = EnsureRetreatPoint(butterfly.transform.position);
            ParticleSystem burst = ConfigureBurst(burstObject);
            TransformSequence sequence = ConfigureDirector(directorObject, burst);
            ConfigureButterfly(butterfly);
            ConfigureCat(cat);

            SavePrefab(burstObject, BurstPrefabPath);
            ApplyPrefab(cat);
            ApplyPrefab(butterfly);

            BindSceneReferences(
                cat,
                butterfly,
                directorObject.GetComponent<GameDirector>(),
                retreatPoint);

            EditorUtility.SetDirty(directorObject);
            EditorUtility.SetDirty(sequence);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("P3 transform catch is wired (director, burst, butterfly fly-on-play, player trigger).");
        }

        [MenuItem("DreamChase/P3 Smoke Catch Walk")]
        public static void SmokeCatchWalk()
        {
            if (!TryBindSmoke("catch walk"))
                return;

            EditorApplication.isPaused = false;
            Time.timeScale = 1f;
            _walkFramesLeft = 80;
            _nextMoveAt = 0d;
            _watching = false;
            Debug.Log("P3 catch walk start pos=" + _smokeMotor.transform.position + " director=" + _smokeDirector.CurrentPhase);
            EditorApplication.update -= TickSmoke;
            EditorApplication.update += TickSmoke;
        }

        [MenuItem("DreamChase/P3 Smoke Transform")]
        public static void SmokeTransform()
        {
            if (!TryBindSmoke("transform"))
                return;

            EditorApplication.isPaused = false;
            Time.timeScale = 1f;
            _walkFramesLeft = 0;
            _smokeStart = _smokeMotor.transform.position;
            _smokeButterflyStart = _smokeButterfly != null ? _smokeButterfly.transform.position : Vector3.zero;
            Debug.Log("P3 smoke start director=" + _smokeDirector.CurrentPhase + " motor=" + _smokeMotor.Phase + " pos=" + _smokeStart);

            _smokeDirector.BeginTransform(_smokeMotor, _smokeButterfly);
            Debug.Log("P3 smoke after begin director=" + _smokeDirector.CurrentPhase + " motor=" + _smokeMotor.Phase);
            BeginWatch(1.2d);
        }

        static bool TryBindSmoke(string label)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode before running the P3 " + label + " smoke test.");
                return false;
            }

            _smokeDirector = Object.FindObjectOfType<GameDirector>();
            _smokeMotor = Object.FindObjectOfType<PlayerMotor>();
            _smokeButterfly = Object.FindObjectOfType<ButterflyController>();
            if (_smokeDirector == null || _smokeMotor == null)
            {
                Debug.LogError("GameDirector or PlayerMotor was not found.");
                return false;
            }

            return true;
        }

        static void TickSmoke()
        {
            if (!Application.isPlaying)
            {
                StopSmokeTick();
                return;
            }

            EditorApplication.isPaused = false;

            if (_walkFramesLeft > 0)
            {
                if (EditorApplication.timeSinceStartup < _nextMoveAt)
                    return;

                _nextMoveAt = EditorApplication.timeSinceStartup + (1.0 / 60.0);
                Time.timeScale = 1f;
                if (_smokeDirector.CurrentPhase == GamePhase.Ground)
                    _smokeMotor.EditorApplyGroundInput(new PlayerInputSnapshot(Vector2.right, true, false), 1f / 60f);

                _walkFramesLeft--;
                if (_walkFramesLeft == 0)
                {
                    Debug.Log(
                        "P3 catch walk after run pos=" + _smokeMotor.transform.position
                        + " director=" + _smokeDirector.CurrentPhase
                        + " motor=" + _smokeMotor.Phase);
                    _smokeStart = _smokeMotor.transform.position;
                    _smokeButterflyStart = _smokeButterfly != null ? _smokeButterfly.transform.position : Vector3.zero;
                    BeginWatch(1.2d);
                }

                return;
            }

            if (_watching && EditorApplication.timeSinceStartup >= _watchUntil)
                FinishWatch();
        }

        static void BeginWatch(double seconds)
        {
            _watching = true;
            _watchUntil = EditorApplication.timeSinceStartup + seconds;
            EditorApplication.update -= TickSmoke;
            EditorApplication.update += TickSmoke;
        }

        static void FinishWatch()
        {
            StopSmokeTick();
            if (_smokeDirector == null || _smokeMotor == null)
                return;

            bool flying = _smokeDirector.CurrentPhase == GamePhase.Flying && _smokeMotor.Phase == GamePhase.Flying;
            bool stillAtStart = (_smokeMotor.transform.position - _smokeStart).sqrMagnitude < 0.0001f;
            bool butterflyWaiting = _smokeButterfly != null && !_smokeButterfly.IsRetreating;
            float butterflyDeltaX = _smokeButterfly != null ? _smokeButterfly.transform.position.x - _smokeButterflyStart.x : 0f;

            Debug.Log(
                "P3 smoke after wait director=" + _smokeDirector.CurrentPhase
                + " motor=" + _smokeMotor.Phase
                + " flying=" + flying
                + " stillAtStart=" + stillAtStart
                + " butterflyWaiting=" + butterflyWaiting
                + " butterflyDeltaX=" + butterflyDeltaX);
        }

        static void StopSmokeTick()
        {
            _watching = false;
            _walkFramesLeft = 0;
            EditorApplication.update -= TickSmoke;
        }

        static void ConfigureLayerCollision()
        {
            int defaultLayer = LayerMask.NameToLayer("Default");
            int butterflyLayer = LayerMask.NameToLayer(ButterflyLayerName);
            if (defaultLayer < 0 || butterflyLayer < 0)
                return;

            Physics.IgnoreLayerCollision(butterflyLayer, defaultLayer, true);
        }

        static Transform EnsureRetreatPoint(Vector3 butterflyPosition)
        {
            GameObject point = GameObject.Find("ButterflyRetreatPoint");
            if (point != null)
                return point.transform;

            Transform world = FindTransform("World");
            point = new GameObject("ButterflyRetreatPoint");
            Undo.RegisterCreatedObjectUndo(point, "Create ButterflyRetreatPoint");
            if (world != null)
                point.transform.SetParent(world, true);

            point.transform.position = new Vector3(RetreatX, butterflyPosition.y, butterflyPosition.z);
            return point.transform;
        }

        static ParticleSystem ConfigureBurst(GameObject burstObject)
        {
            ParticleSystem particle = burstObject.GetComponent<ParticleSystem>();
            if (particle == null)
                particle = Undo.AddComponent<ParticleSystem>(burstObject);

            ParticleSystem.MainModule main = particle.main;
            main.duration = 0.8f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.55f;
            main.startSpeed = 2.4f;
            main.startSize = 0.16f;
            main.startColor = new Color(1f, 0.88f, 0.4f, 1f);
            main.gravityModifier = 0.12f;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.92f, 0.45f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.4f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = burstObject.GetComponent<ParticleSystemRenderer>();
            GameObject ambient = GameObject.Find("AmbientParticles");
            if (renderer != null && ambient != null)
            {
                ParticleSystemRenderer ambientRenderer = ambient.GetComponent<ParticleSystemRenderer>();
                if (ambientRenderer != null && ambientRenderer.sharedMaterial != null)
                    renderer.sharedMaterial = ambientRenderer.sharedMaterial;
            }

            burstObject.SetActive(false);
            return particle;
        }

        static TransformSequence ConfigureDirector(GameObject directorObject, ParticleSystem burst)
        {
            TransformSequence sequence = directorObject.GetComponent<TransformSequence>();
            if (sequence == null)
                sequence = Undo.AddComponent<TransformSequence>(directorObject);

            SerializedObject sequenceSo = new SerializedObject(sequence);
            sequenceSo.FindProperty("burst").objectReferenceValue = burst;
            sequenceSo.FindProperty("duration").floatValue = 0.8f;
            sequenceSo.ApplyModifiedProperties();

            GameDirector director = directorObject.GetComponent<GameDirector>();
            if (director == null)
                director = Undo.AddComponent<GameDirector>(directorObject);

            SerializedObject directorSo = new SerializedObject(director);
            directorSo.FindProperty("transformSequence").objectReferenceValue = sequence;
            directorSo.ApplyModifiedProperties();
            return sequence;
        }

        static void ConfigureButterfly(GameObject butterfly)
        {
            int layer = LayerMask.NameToLayer(ButterflyLayerName);
            if (layer >= 0)
                butterfly.layer = layer;
            butterfly.tag = "Butterfly";

            CapsuleCollider capsule = butterfly.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = Undo.AddComponent<CapsuleCollider>(butterfly);

            FitTriggerCapsule(butterfly, capsule, 1.05f);
            capsule.isTrigger = true;
            capsule.direction = 1;

            Rigidbody body = butterfly.GetComponent<Rigidbody>();
            if (body == null)
                body = Undo.AddComponent<Rigidbody>(butterfly);

            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotation
                | RigidbodyConstraints.FreezePositionZ;

            ButterflyController controller = butterfly.GetComponent<ButterflyController>();
            if (controller == null)
                controller = Undo.AddComponent<ButterflyController>(butterfly);

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("retreatSpeed").floatValue = 2.5f;
            controllerSo.FindProperty("idleOnly").boolValue = true;
            controllerSo.FindProperty("flyOnPlay").boolValue = true;
            controllerSo.ApplyModifiedProperties();
        }

        static void ConfigureCat(GameObject cat)
        {
            int layer = LayerMask.NameToLayer(PlayerLayerName);
            if (layer >= 0)
                cat.layer = layer;
            cat.tag = "Player";

            CapsuleCollider rootCapsule = cat.GetComponent<CapsuleCollider>();
            if (rootCapsule != null)
                Undo.DestroyObjectImmediate(rootCapsule);

            PlayerCatchTrigger rootCatch = cat.GetComponent<PlayerCatchTrigger>();
            if (rootCatch != null)
                Undo.DestroyObjectImmediate(rootCatch);

            Transform catchTransform = cat.transform.Find("CatchTrigger");
            GameObject catchObject = catchTransform != null ? catchTransform.gameObject : null;
            if (catchObject == null)
            {
                catchObject = new GameObject("CatchTrigger");
                Undo.RegisterCreatedObjectUndo(catchObject, "Create CatchTrigger");
                catchObject.transform.SetParent(cat.transform, false);
                catchObject.transform.localPosition = Vector3.zero;
                catchObject.transform.localRotation = Quaternion.identity;
                catchObject.transform.localScale = Vector3.one;
            }

            catchObject.layer = cat.layer;
            catchObject.tag = "Player";

            CapsuleCollider capsule = catchObject.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = Undo.AddComponent<CapsuleCollider>(catchObject);

            CharacterController characterController = cat.GetComponent<CharacterController>();
            if (characterController != null)
            {
                capsule.center = characterController.center;
                capsule.height = characterController.height * 1.1f;
                capsule.radius = characterController.radius * CatchScale;
            }
            else
            {
                FitTriggerCapsule(cat, capsule, CatchScale);
            }

            capsule.isTrigger = true;
            capsule.direction = 1;

            Rigidbody body = catchObject.GetComponent<Rigidbody>();
            if (body != null)
                Undo.DestroyObjectImmediate(body);

            if (catchObject.GetComponent<PlayerCatchTrigger>() == null)
                Undo.AddComponent<PlayerCatchTrigger>(catchObject);
        }

        static void BindSceneReferences(GameObject cat, GameObject butterfly, GameDirector director, Transform retreatPoint)
        {
            Transform catchTransform = cat.transform.Find("CatchTrigger");
            PlayerCatchTrigger catchTrigger = catchTransform != null
                ? catchTransform.GetComponent<PlayerCatchTrigger>()
                : cat.GetComponent<PlayerCatchTrigger>();
            if (catchTrigger != null)
            {
                SerializedObject catchSo = new SerializedObject(catchTrigger);
                catchSo.FindProperty("gameDirector").objectReferenceValue = director;
                catchSo.ApplyModifiedProperties();
            }

            ButterflyController controller = butterfly.GetComponent<ButterflyController>();
            if (controller != null && retreatPoint != null)
            {
                SerializedObject controllerSo = new SerializedObject(controller);
                controllerSo.FindProperty("retreatTarget").objectReferenceValue = retreatPoint;
                controllerSo.FindProperty("retreatWorldPosition").vector3Value = retreatPoint.position;
                controllerSo.FindProperty("flyOnPlay").boolValue = true;
                controllerSo.ApplyModifiedProperties();
            }
        }

        static void FitTriggerCapsule(GameObject go, CapsuleCollider capsule, float scale)
        {
            float absScaleX = Mathf.Max(0.0001f, Mathf.Abs(go.transform.lossyScale.x));
            float absScaleY = Mathf.Max(0.0001f, Mathf.Abs(go.transform.lossyScale.y));
            float height = 1.2f / absScaleY;
            float radius = 0.4f / absScaleX;
            Vector3 center = Vector3.zero;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds worldBounds = renderer.bounds;
                if (worldBounds.size.sqrMagnitude > 0.0001f)
                {
                    float worldHeight = Mathf.Max(0.4f, worldBounds.size.y) * scale;
                    float worldRadius = Mathf.Clamp(worldBounds.extents.x * 0.5f * scale, 0.12f, worldHeight * 0.5f);
                    height = worldHeight / absScaleY;
                    radius = worldRadius / absScaleX;
                    float worldCenterY = worldBounds.center.y - go.transform.position.y;
                    center = new Vector3(0f, worldCenterY / absScaleY, 0f);
                }
            }

            capsule.height = height;
            capsule.radius = radius;
            capsule.center = center;
        }

        static void ApplyPrefab(GameObject instance)
        {
            if (instance == null || PrefabUtility.GetPrefabInstanceStatus(instance) != PrefabInstanceStatus.Connected)
                return;

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        }

        static void SavePrefab(GameObject instance, string path)
        {
            if (instance == null)
                return;

            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
        }

        static Transform FindTransform(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.transform : null;
        }

        static GameObject FindChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            Transform child = parent.Find(childName);
            return child != null ? child.gameObject : null;
        }
    }
}
