using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DreamChase.Editor
{
    public static class GroundControlSetup
    {
        const string ClipsFolder = "Assets/Animators/Clips";
        const string ControllerPath = "Assets/Animators/Cat.controller";
        const string CatPrefabPath = "Assets/Prefabs/Player/Cat_Ganbei.prefab";
        const float SpeedThreshold = 0.1f;

        [MenuItem("DreamChase/Setup P1 Ground Controls")]
        public static void Setup()
        {
            AnimatorController controller = CreateOrRefreshAnimator();
            GameObject sceneCat = GameObject.Find("Cat_Ganbei");
            if (sceneCat != null)
            {
                ApplyTo(sceneCat, controller);
                PrefabUtility.ApplyPrefabInstance(sceneCat, InteractionMode.AutomatedAction);
            }

            ApplyToPrefabAsset(controller);
            Debug.Log("P1 ground controls are on Cat_Ganbei (CharacterController, input, motor, animator).");
        }

        [MenuItem("DreamChase/P1 Smoke Walk Right")]
        public static void SmokeWalkRight()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode before running the P1 walk smoke test.");
                return;
            }

            PlayerMotor motor = Object.FindObjectOfType<PlayerMotor>();
            if (motor == null)
            {
                Debug.LogError("PlayerMotor was not found.");
                return;
            }

            Transform cat = motor.transform;
            Debug.Log("P1 smoke start pos=" + cat.position + " grounded=" + motor.IsGrounded);
            PlayerInputSnapshot walk = new PlayerInputSnapshot(Vector2.right, false, false);
            for (int i = 0; i < 45; i++)
                motor.EditorApplyGroundInput(walk, 1f / 60f);

            Debug.Log("P1 smoke after 45 walk ticks pos=" + cat.position + " moving=" + motor.IsMoving);
        }

        [MenuItem("DreamChase/P1 Smoke Run Jump")]
        public static void SmokeRunJump()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode before running the P1 jump smoke test.");
                return;
            }

            PlayerMotor motor = Object.FindObjectOfType<PlayerMotor>();
            if (motor == null)
            {
                Debug.LogError("PlayerMotor was not found.");
                return;
            }

            Transform cat = motor.transform;
            float startY = cat.position.y;
            motor.EditorApplyGroundInput(new PlayerInputSnapshot(Vector2.right, true, true), 1f / 60f);
            Debug.Log("P1 smoke jump startY=" + startY + " nowY=" + cat.position.y + " jumping=" + motor.IsJumping);
        }

        public static void ApplyTo(GameObject cat)
        {
            ApplyTo(cat, CreateOrRefreshAnimator());
        }

        static void ApplyTo(GameObject cat, AnimatorController controller)
        {
            if (cat == null)
                return;

            CharacterController characterController = cat.GetComponent<CharacterController>();
            if (characterController == null)
                characterController = cat.AddComponent<CharacterController>();

            FitCharacterController(cat, characterController);

            if (cat.GetComponent<PlayerInputReader>() == null)
                cat.AddComponent<PlayerInputReader>();

            if (cat.GetComponent<PlayerMotor>() == null)
                cat.AddComponent<PlayerMotor>();

            Animator animator = cat.GetComponent<Animator>();
            if (animator == null)
                animator = cat.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (cat.GetComponent<PlayerAnimationDriver>() == null)
                cat.AddComponent<PlayerAnimationDriver>();
        }

        static void ApplyToPrefabAsset(AnimatorController controller)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(CatPrefabPath);
            try
            {
                ApplyTo(contents, controller);
                PrefabUtility.SaveAsPrefabAsset(contents, CatPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void FitCharacterController(GameObject cat, CharacterController characterController)
        {
            float absScaleX = Mathf.Max(0.0001f, Mathf.Abs(cat.transform.lossyScale.x));
            float absScaleY = Mathf.Max(0.0001f, Mathf.Abs(cat.transform.lossyScale.y));
            float height = 1.6f / absScaleY;
            float radius = 0.28f / absScaleX;
            Vector3 center = Vector3.zero;

            Renderer renderer = cat.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds worldBounds = renderer.bounds;
                if (worldBounds.size.sqrMagnitude > 0.0001f)
                {
                    float worldHeight = Mathf.Max(0.8f, worldBounds.size.y);
                    float worldRadius = Mathf.Clamp(worldBounds.extents.x * 0.4f, 0.12f, worldHeight * 0.35f);
                    height = worldHeight / absScaleY;
                    radius = worldRadius / absScaleX;
                    float worldCenterY = worldBounds.center.y - cat.transform.position.y;
                    center = new Vector3(0f, worldCenterY / absScaleY, 0f);
                }
            }

            characterController.height = height;
            characterController.radius = radius;
            characterController.center = center;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0.001f;
            characterController.slopeLimit = 45f;
            float maxStep = Mathf.Max(0.01f, height - 2f * radius - 0.01f);
            characterController.stepOffset = Mathf.Min(0.3f, maxStep);
        }

        static AnimatorController CreateOrRefreshAnimator()
        {
            Directory.CreateDirectory(ToAbsolute(ClipsFolder));

            AnimationClip idle = GetOrCreateClip(ClipsFolder + "/idle.anim", true, 1f);
            AnimationClip walk = GetOrCreateClip(ClipsFolder + "/walk.anim", true, 1f);
            AnimationClip run = GetOrCreateClip(ClipsFolder + "/run.anim", true, 0.8f);
            AnimationClip jump = GetOrCreateClip(ClipsFolder + "/jump.anim", false, 0.45f);
            AnimationClip fly = GetOrCreateClip(ClipsFolder + "/fly.anim", true, 1f);

            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            EnsureParameters(controller);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.states = new ChildAnimatorState[0];
            machine.anyStateTransitions = new AnimatorStateTransition[0];

            AnimatorState idleState = machine.AddState("Idle", new Vector3(250f, 0f, 0f));
            idleState.motion = idle;
            AnimatorState walkState = machine.AddState("Walk", new Vector3(500f, 0f, 0f));
            walkState.motion = walk;
            AnimatorState runState = machine.AddState("Run", new Vector3(750f, 0f, 0f));
            runState.motion = run;
            AnimatorState jumpState = machine.AddState("Jump", new Vector3(500f, -180f, 0f));
            jumpState.motion = jump;
            AnimatorState flyState = machine.AddState("Fly", new Vector3(500f, 180f, 0f));
            flyState.motion = fly;

            machine.defaultState = idleState;

            AddConditionTransition(idleState, walkState, AnimatorConditionMode.Greater, "Speed", SpeedThreshold);
            AddConditionTransition(walkState, idleState, AnimatorConditionMode.Less, "Speed", SpeedThreshold);
            AddBoolTransition(walkState, runState, "IsRunning", true);
            AddBoolTransition(runState, walkState, "IsRunning", false);
            AddBoolTransition(idleState, runState, "IsRunning", true);
            AddConditionTransition(runState, idleState, AnimatorConditionMode.Less, "Speed", SpeedThreshold);

            AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
            ConfigureInstant(jumpToIdle);
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToIdle.AddCondition(AnimatorConditionMode.Less, SpeedThreshold, "Speed");

            AnimatorStateTransition jumpToWalk = jumpState.AddTransition(walkState);
            ConfigureInstant(jumpToWalk);
            jumpToWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToWalk.AddCondition(AnimatorConditionMode.Greater, SpeedThreshold, "Speed");
            jumpToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            AnimatorStateTransition jumpToRun = jumpState.AddTransition(runState);
            ConfigureInstant(jumpToRun);
            jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            AnimatorStateTransition anyToFly = machine.AddAnyStateTransition(flyState);
            ConfigureInstant(anyToFly);
            anyToFly.AddCondition(AnimatorConditionMode.If, 0f, "IsFlying");
            anyToFly.canTransitionToSelf = false;

            AnimatorStateTransition anyToJumpTrigger = machine.AddAnyStateTransition(jumpState);
            ConfigureInstant(anyToJumpTrigger);
            anyToJumpTrigger.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            anyToJumpTrigger.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFlying");
            anyToJumpTrigger.canTransitionToSelf = false;

            AnimatorStateTransition anyToJumpAir = machine.AddAnyStateTransition(jumpState);
            ConfigureInstant(anyToJumpAir);
            anyToJumpAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            anyToJumpAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFlying");
            anyToJumpAir.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static void EnsureParameters(AnimatorController controller)
        {
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFlying", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        }

        static void AddConditionTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            string parameter,
            float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureInstant(transition);
            transition.AddCondition(mode, threshold, parameter);
        }

        static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureInstant(transition);
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        static void ConfigureInstant(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.exitTime = 0f;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.offset = 0f;
        }

        static AnimationClip GetOrCreateClip(string path, bool loop, float duration)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { frameRate = 60f, name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(clip, path);
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.startTime = 0f;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static string ToAbsolute(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        }
    }
}
