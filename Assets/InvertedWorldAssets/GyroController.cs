using UnityEngine;

namespace DancingLineFanmade.Level.InvertedWorld
{
    public class GyroController : MonoBehaviour
    {
        private static readonly int CurveOriginZ = Shader.PropertyToID("_CurveOriginZ");
        public static GyroController Instance { get; private set; }

        [SerializeField] private float sensitivity = 1.5f;
        [SerializeField] private float maxRollAngle = 45f;
        [SerializeField] private float smoothTime = 0.2f;
        
        [SerializeField] private float simTurnTiltAmount = 15f;
        [SerializeField] private float simTiltRecoverySpeed = 2.0f;

        private float currentRoll;
        private float currentVelocity;
        private Quaternion baseIdentity = Quaternion.identity;
        private float simulationTargetRoll;

        private const float LowPassFilterFactor = 0.2f;
        private Vector3 lowPassValue = Vector3.zero;

        private void Awake()
        {
            Instance = this;
            
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                baseIdentity = Quaternion.Inverse(GetUnityGyroRotation());
            }
        }

        private void Start()
        {
            if (Player.Instance != null)
            {
                Player.Instance.OnTurn.AddListener(OnPlayerTurn);
            }
        }

        private void OnPlayerTurn()
        {
            if (!SystemInfo.supportsGyroscope && Input.acceleration == Vector3.zero)
            {
                simulationTargetRoll = (Random.value > 0.5f ? 1 : -1) * simTurnTiltAmount;
            }
        }

        private void Update()
        {
            float targetRoll;

            if (SystemInfo.supportsGyroscope)
            {
                Quaternion q = GetUnityGyroRotation();
                q = baseIdentity * q; 
                
                float rawRoll = -q.eulerAngles.z;
                if (rawRoll < -180) rawRoll += 360;
                
                targetRoll = rawRoll * sensitivity;
            }
            else if (Input.acceleration != Vector3.zero)
            {
                lowPassValue = Vector3.Lerp(lowPassValue, Input.acceleration, LowPassFilterFactor);
                
                targetRoll = Mathf.Clamp(lowPassValue.x * 90f, -maxRollAngle, maxRollAngle);
            }
            else
            {
                simulationTargetRoll = Mathf.Lerp(simulationTargetRoll, 0f, Time.deltaTime * simTiltRecoverySpeed);
                targetRoll = simulationTargetRoll;
            }

            targetRoll = Mathf.Clamp(targetRoll, -maxRollAngle, maxRollAngle);

            currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref currentVelocity, smoothTime);

            UpdateShaderGlobals();
        }

        private Quaternion GetUnityGyroRotation()
        {
            return new Quaternion(0.5f, 0.5f, -0.5f, 0.5f) * Input.gyro.attitude * new Quaternion(0, 0, 1, 0);
        }

        public float GetRoll()
        {
            return currentRoll;
        }

        public void Calibrate()
        {
            if (SystemInfo.supportsGyroscope)
            {
                baseIdentity = Quaternion.Inverse(GetUnityGyroRotation());
            }
        }

        private void UpdateShaderGlobals()
        {
            if (Camera.main != null)
            {
                Shader.SetGlobalFloat(CurveOriginZ, Camera.main.transform.position.z);
            }
        }
    }
}