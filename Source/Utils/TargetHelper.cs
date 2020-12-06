using System;
using UnityEngine;
using UnityEngine.UI;

namespace OLDD_camera.Utils
{
    /// <summary>
    /// Extended information about the selected target
    /// </summary>
    public class TargetHelper
    {
        private readonly GameObject _self;
        private readonly Part _selfPart;
        public Matrix4x4 CoordinateTransform;
        public Vector3 DPos;
        public Vector3 Velocity;
        public float DX;
        public float DY;
        public float DZ;
        public float SpeedX;
        public float SpeedY;
        public float SpeedZ;
        public float AngleX;
        public float AngleY;
        public float AngleZ;
        public float Destination;
        public float lastDestination;
        public float closureRate;
        public bool IsMoveToTarget;
        public float SecondsToDock;
        public bool LookForward;
        public float TargetMoveHelpX;
        public float TargetMoveHelpY;
        public struct VelocityMarker
        {
            public bool Forward;
            public float X;
            public float Y;
        }
        public VelocityMarker DesiredVelocityMarker;
        public VelocityMarker CurrentVelocityMarker;

        /// <param name="from">Object of comparison</param>
        public TargetHelper(Part from)
        {
            _selfPart = from;
            _self = _selfPart.gameObject;
        }
        public static ITargetable Target => FlightGlobals.fetch.VesselTarget;

        private Transform targetTransform => Target.GetTransform();

        public static bool IsTargetSelect => Target != null && (Target as ModuleDockingNode != null || Target as Vessel != null);

        public bool IsDockPort => Target is ModuleDockingNode;


        public void Update()
        {
            UpdateTransformationMatrix();
            UpdatePosition();


            if (!HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_2>().altOverlay)
            {
                var velocity = UpdateSpeed();
                lastDestination = Destination;
                Destination = (float)Math.Sqrt(Math.Pow(DX, 2) + Math.Pow(DY, 2) + Math.Pow(DZ, 2));
                closureRate = velocity.magnitude;
                if (lastDestination < Destination)
                    closureRate *= -1;
            }
            else
            {
                UpdateVelocity();
                Destination = DPos.magnitude;
            }
            UpdateAngles();
            UpdateIsMoveToTarget();
            DesiredVelocityMarker = UpdateVelocityMarker(DPos);
            CurrentVelocityMarker = UpdateVelocityMarker(Velocity);
        }

        private void UpdateTransformationMatrix()
        {
            CoordinateTransform.SetColumn(0, _self.transform.right);
            CoordinateTransform.SetColumn(1, -_self.transform.forward);
            CoordinateTransform.SetColumn(2, _self.transform.up);
            CoordinateTransform.SetColumn(3, new Vector4(0, 0, 0, 1));
            CoordinateTransform = CoordinateTransform.inverse;
        }

        private void UpdatePosition()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_2>().altOverlay)
            {
#if false
            DX = targetTransform.position.x - _self.transform.position.x;
            DY = targetTransform.position.y - _self.transform.position.y;
            DZ = targetTransform.position.z - _self.transform.position.z;
#endif
                Vector3 targetVector = targetTransform.position - _self.transform.position;
                DX = Vector3.Dot(targetVector, _self.transform.right);
                DZ = Vector3.Dot(targetVector, _self.transform.up);
                DY = Vector3.Dot(targetVector, _self.transform.forward);
            }
            DPos = targetTransform.position - _self.transform.position;
            DPos = CoordinateTransform.MultiplyPoint3x4(DPos);
        }

        private void UpdateVelocity()
        {
            Velocity = _selfPart.vessel.GetObtVelocity() - Target.GetObtVelocity();
            Velocity = CoordinateTransform.MultiplyPoint3x4(Velocity);
        }

        private void UpdateAngles()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_2>().altOverlay)
                UpdateAnglesOld();
            else
                UpdateAnglesNew();
        }

        private void UpdateAnglesNew()
        {
            AngleX = -SignedAngleAroundVector(-targetTransform.forward, _self.transform.up, _self.transform.right);
            AngleY = SignedAngleAroundVector(-targetTransform.forward, _self.transform.up, -_self.transform.forward);
            AngleZ = SignedAngleAroundVector(targetTransform.up, _self.transform.forward, -_self.transform.up);
        }

        private VelocityMarker UpdateVelocityMarker(Vector3 velocity)
        {
            const float epsilon = 0.000001f;
            VelocityMarker velocityMarker = new VelocityMarker();
            float zOverX = Mathf.Sign(velocity.x) * velocity.z / (Mathf.Abs(velocity.x) + epsilon);
            float zOverY = Mathf.Sign(velocity.y) * velocity.z / (Mathf.Abs(velocity.y) + epsilon);
            velocityMarker.X = Mathf.Sign(velocity.z)*Mathf.Sign(zOverX) / (1 + Mathf.Abs(zOverX));
            velocityMarker.Y = Mathf.Sign(velocity.z)*Mathf.Sign(zOverY) / (1 + Mathf.Abs(zOverY));
            velocityMarker.X = (velocityMarker.X + 1) / 2;
            velocityMarker.Y = (velocityMarker.Y + 1) / 2;
            velocityMarker.Forward = velocity.z > 0;
            return velocityMarker;
        }

        private void UpdateIsMoveToTarget()             // dockingLamp- 
        {
            var checkedDevByZero = false;

            try
            {
                SecondsToDock = Destination / Velocity.magnitude;
                checkedDevByZero = true;
            }
            catch (DivideByZeroException)
            {
                IsMoveToTarget = false;
            }

            if (!checkedDevByZero) return;

            float timeX;
            float timeY;
            if (Velocity.x == 0 && Mathf.Abs(DPos.x) < .5f)
                timeX = SecondsToDock;
            else
                timeX = (Mathf.Abs(DPos.x) < .5f) ? SecondsToDock : -DPos.x / Velocity.x;

            if (Velocity.y == 0 && Mathf.Abs(DPos.y) < .5f)
                timeY = SecondsToDock;
            else
                timeY = (Mathf.Abs(DPos.y) < .5f) ? SecondsToDock : -DPos.y / Velocity.y;

            IsMoveToTarget = Mathf.Abs(SecondsToDock - timeX) < 1 &&
                             Mathf.Abs(SecondsToDock - timeY) < 1 &&
                             DPos.z * Velocity.z < 0;
        }


        private void UpdateAnglesOld()
        {
            AngleX = SignedAngleAroundVector(-targetTransform.forward, _self.transform.up, -_self.transform.forward);
            AngleY = SignedAngleAroundVector(-targetTransform.forward, _self.transform.up, _self.transform.right);
            AngleZ = SignedAngleAroundVector(targetTransform.up, -_self.transform.forward, -_self.transform.up);
        }

        private Vector3 UpdateSpeed()
        {
            var velocity = Target.GetObtVelocity() - _selfPart.vessel.GetObtVelocity();
            SpeedX = velocity.x;
            SpeedY = velocity.y;
            SpeedZ = velocity.z;

            return velocity;
        }

        private void UpdateTargetMoveHelp()
        {
            Vector3 targetToOwnship = _self.transform.position - targetTransform.position;
            var translationDeviation = new Vector2(
                SignedAngleAroundVector(targetToOwnship, targetTransform.forward.normalized, _self.transform.forward),
                SignedAngleAroundVector(targetToOwnship, targetTransform.forward.normalized, -_self.transform.right));
            LookForward = Math.Abs(translationDeviation.x) < 90;
            float gaugeX = (LookForward ? 1 : -1) * ((translationDeviation.x / 90f)%2);
            float gaugeY = (LookForward ? 1 : -1) * ((translationDeviation.y / 90f)%2);
            float exponent = .75f;

            if (Destination <= 5f)
                exponent = 1f;
            else
                if (Destination < 15f)
                {
                    float toGo = Destination - 5f;
                    float range = 15f - 5f;
                    float lerp = toGo / range;
                    float exponentReduction = 1f - .75f;
                    exponent = 1 - (exponentReduction) * lerp;
                }

            TargetMoveHelpX = (ScaleExponentially(gaugeX, exponent) + 1) / 2f;
            TargetMoveHelpY = (ScaleExponentially(gaugeY, exponent) + 1) / 2f;
        }

        //private void UpdateTargetMoveHelp()
        //{
        //    Vector3 targetToOwnship = _self.transform.position - targetTransform.position;
        //    var translationDeviation = new Vector2(
        //        SignedAngleAroundVector(targetToOwnship, targetTransform.forward.normalized, _self.transform.forward),
        //        SignedAngleAroundVector(targetToOwnship, targetTransform.forward.normalized, -_self.transform.right));
        //    LookForward = Math.Abs(translationDeviation.x) < 90;
        //    float gaugeX = (LookForward ? 1 : -1) * ((translationDeviation.x / 90f)%2);
        //    float gaugeY = (LookForward ? 1 : -1) * ((translationDeviation.y / 90f)%2);
        //    float exponent = .75f;

        //    if (Destination <= 5f)
        //        exponent = 1f;
        //    else
        //        if (Destination < 15f)
        //        {
        //            float toGo = Destination - 5f;
        //            float range = 15f - 5f;
        //            float lerp = toGo / range;
        //            float exponentReduction = 1f - .75f;
        //            exponent = 1 - (exponentReduction) * lerp;
        //        }

        //    TargetMoveHelpX = (ScaleExponentially(gaugeX, exponent) + 1) / 2f;
        //    TargetMoveHelpY = (ScaleExponentially(gaugeY, exponent) + 1) / 2f;
        //}


        private static float SignedAngleAroundVector(Vector3 a, Vector3 b, Vector3 c)
        {
            var v1 = Vector3.Cross(c, a);
            var v2 = Vector3.Cross(c, b);
            if (Vector3.Dot(Vector3.Cross(v1, v2), c) < 0)
                return -Vector3.Angle(v1, v2);
            return Vector3.Angle(v1, v2);
        }
        
        private static float ScaleExponentially(float value, float exponent)
        {
            return (float)Math.Pow(Math.Abs(value), exponent) * Math.Sign(value);
        }

    }
}
