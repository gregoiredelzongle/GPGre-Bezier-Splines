using UnityEngine;
using System;
using System.Collections.Generic;
using GPGre.QuadraticBezier;

namespace GPGre.QuadraticBezier
{
    /// <summary>
    /// Create a Cubic Bezier Spline and modify it directly inside the scene view. 
    /// Inspired by Jasper Flick tutorial ( http://catlikecoding.com/unity/tutorials/curves-and-splines/ )
    /// </summary>
    public class BezierSpline : MonoBehaviour
    {
        #region Point Parameters

        // Hold every control point 
        [SerializeField]
        public Vector3[] points;

        // Hold every control point mode (free, aligned, mirrored)
        [SerializeField]
        private BezierControlPointMode[] modes;

        #endregion

        #region Spline Parameters

        // Loop the curve
        [SerializeField]
        private bool loop;

        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
                if (value == true)
                {
                    modes[modes.Length - 1] = modes[0];
                    SetControlPoint(0, points[0]);
                }
            }
        }

        // Amount of curves inside the spline
        public int CurveCount
        {
            get
            {
                return (points.Length - 1) / 3;
            }
        }

        // Amount of points
        public int ControlPointCount
        {
            get
            {
                return points.Length;
            }
        }

        #endregion

        #region Distance Computation Holding Variables

        // Hold approximate length of each curve inside the spline (used to ease complicated calculations of the total distance of the spline)
        [SerializeField]
        private float[] curvesDistance;
        [SerializeField]
        private float splineDistance;

        public float SplineDistance
        {
            get
            {
                return splineDistance;
            }
        }

        #endregion

        #region Internal Constants

        // Spacing between each point when creating a new point at the end of the spline
        private const float defaultSpacing = 3f;

        #endregion

        #region Main Methods

        /// <summary>
        /// Called when the component is created/added/reset
        /// </summary>
        public void Reset()
        {
            points = new Vector3[]{
                new Vector3(0f,0f,0f),
                new Vector3(defaultSpacing/3,0f,0f),
                new Vector3(defaultSpacing - (defaultSpacing/3),0f,0f),
                new Vector3(defaultSpacing,0f,0f)
                };
            modes = new BezierControlPointMode[] {
                BezierControlPointMode.Mirrored,
                BezierControlPointMode.Mirrored
                };

            RecalculateSplineDistance();
        }

        #endregion

        #region Control Point Getter/Setter Functions

        /// <summary>
        /// Return a control point stored. Return a error if index negative or greater than point array length.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The position of the control point</returns>
        public Vector3 GetControlPoint(int index)
        {
            return points[index];
        }

        /// <summary>
        /// Set a control point stored. Return a error if index negative or greater than point array length.
        /// </summary>
        /// <param name="index"></param>
        public void SetControlPoint(int index, Vector3 point)
        {
            // Check if control point is a curve extreme point
            if (index % 3 == 0)
            {
                Vector3 delta = point - points[index];
                if (loop)
                {
                    if (index == 0)
                    {
                        points[1] += delta;
                        points[points.Length - 2] += delta;
                        points[points.Length - 1] = point;
                    }
                    else if (index == points.Length - 1)
                    {
                        points[0] = point;
                        points[1] += delta;
                        points[index - 1] += delta;
                    }
                    else
                    {
                        points[index - 1] += delta;
                        points[index + 1] += delta;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        points[index - 1] += delta;
                    }
                    if (index + 1 < points.Length)
                    {
                        points[index + 1] += delta;
                    }
                }
            }
            points[index] = point;
            EnforceMode(index);

        }

        /// <summary>
        /// Get normalized direction of a control point.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 GetControlPointDirection(int index)
        {
            if (index >= points.Length - 1)
                return (points[index] - points[index - 1]).normalized;
            else
                return (points[index + 1] - points[index]).normalized;
        }

        /// <summary>
        /// Get Bezier control mode (Free/Mirrored/Aligned) of a control point.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public BezierControlPointMode GetControlPointMode(int index)
        {
            return modes[(index + 1) / 3];
        }

        /// <summary>
        /// Set Bezier control mode (Free/Mirrored/Aligned) of a control point.
        /// </summary>
        /// <param name="index"></param>
        public void SetControlPointMode(int index, BezierControlPointMode mode)
        {

            int modeIndex = (index + 1) / 3;
            modes[modeIndex] = mode;
            if (loop)
            {
                if (modeIndex == 0)
                {
                    modes[modes.Length - 1] = mode;
                }
                else if (modeIndex == modes.Length - 1)
                {
                    modes[0] = mode;
                }
            }
            EnforceMode(index);

        }

        /// <summary>
        /// Enforce control point mode to ensure aligned and mirrored points are properly updated
        /// </summary>
        /// <param name="index"></param>
        private void EnforceMode(int index)
        {
            int modeIndex = (index + 1) / 3;
            BezierControlPointMode mode = modes[modeIndex];
            if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length - 1))
            {
                return;
            }

            int middleIndex = modeIndex * 3;
            int fixedIndex, enforcedIndex;
            if (index <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                if (fixedIndex < 0)
                {
                    fixedIndex = points.Length - 2;
                }
                enforcedIndex = middleIndex + 1;
                if (enforcedIndex >= points.Length)
                {
                    enforcedIndex = 1;
                }
            }
            else
            {
                fixedIndex = middleIndex + 1;
                if (fixedIndex >= points.Length)
                {
                    fixedIndex = 1;
                }
                enforcedIndex = middleIndex - 1;
                if (enforcedIndex < 0)
                {
                    enforcedIndex = points.Length - 2;
                }
            }

            Vector3 middle = points[middleIndex];
            Vector3 enforcedTangent = middle - points[fixedIndex];

            if (mode == BezierControlPointMode.Aligned)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
            }
            points[enforcedIndex] = middle + enforcedTangent;

        }
        #endregion

        #region Spline Functions (Not Uniform)

        /// <summary>
        /// Access a point inside the curve using a linear interpolation. The spacing is not uniform along the spline but is fast to compute
        /// </summary>
        /// <param name="t">Should be between 0 and 1</param>
        /// <returns>a point inside the spline at t position</returns>
        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }

            return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
        }

        /// <summary>
        /// Return a velocity of a point inside the curve using a linear interpolation. The spacing is not uniform along the spline but is fast to compute
        /// </summary>
        /// <param name="t">Should be between 0 and 1</param>
        /// <returns>velocity of a point inside the spline at t position</returns>
        public Vector3 GetVelocity(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
        }

        /// <summary>
        /// Return a normalized velocity of a point inside the curve using a linear interpolation. The spacing is not uniform along the spline but is fast to compute
        /// </summary>
        /// <param name="t">Should be between 0 and 1</param>
        /// <returns>normalized velocity of a point inside the spline at t position</returns>
        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }
        #endregion

        #region Spline Functions (Uniform)

        /// <summary>
        /// Access a point inside the curve using a linear interpolation. The spacing is uniform along the spline but is more expensive to compute
        /// </summary>
        /// <param name="t">Should be between 0 and 1</param>
        /// <returns>a point inside the spline at t position</returns>
        public Vector3 GetPointUniform(float t)
        {
            float splinePosition = t * splineDistance;
            float sum = 0;
            int index = 0;

            for (int i = 0; i < curvesDistance.Length; i++)
            {

                if (sum <= splinePosition && splinePosition <= sum + curvesDistance[i])
                {
                    t = (splinePosition - sum) / curvesDistance[i];
                    index = i;
                }
                sum += curvesDistance[i];

            }
            index *= 3;

            return transform.TransformPoint(Bezier.GetPoint(points[index], points[index + 1], points[index + 2], points[index + 3], t));
        }

        /// <summary>
        /// Access a point velocity inside the curve using a linear interpolation. The spacing is uniform along the spline but is more expensive to compute
        /// </summary>
        /// <param name="t">Should be between 0 and 1</param>
        /// <returns>a point velocity inside the spline at t position</returns>
        public Vector3 GetVelocityUniform(float t)
        {
            float splinePosition = t * splineDistance;
            float sum = 0;
            int index = 0;

            for (int i = 0; i < curvesDistance.Length; i++)
            {

                if (sum <= splinePosition && splinePosition <= sum + curvesDistance[i])
                {
                    t = (splinePosition - sum) / curvesDistance[i];
                    index = i;
                }
                sum += curvesDistance[i];

            }
            index *= 3;

            return transform.TransformPoint(Bezier.GetFirstDerivative(points[index], points[index + 1], points[index + 2], points[index + 3], t)) - transform.position;
        }

        /// <summary>
        /// Access a normalized point velocity inside the curve using a linear interpolation. The spacing is uniform along the spline but is more expensive to compute
        /// </summary>
        /// <param name="t">Should be between 0 and 1</param>
        /// <returns>a normalized point velocity inside the spline at t position</returns>
        public Vector3 GetDirectionUniform(float t)
        {
            return GetVelocityUniform(t).normalized;
        }

        #endregion

        #region Distance Computation

        /// <summary>
        /// Compute and return the approximate length of the Bezier Spline
        /// </summary>
        /// <returns>The total length of the spline</returns>
        private float CalculateSplineDistance()
        {
            float length = 0;
            for (int i = 0; i < CurveCount; i += 1)
            {
                int index = i * 3;
                length += Bezier.GetLength(points[index], points[index + 1], points[index + 2], points[index + 3]);
            }
            return length;
        }

        /// <summary>
        /// Compute and return the approximate length of every curves inside of the Bezier Spline
        /// </summary>
        /// <returns>approximates lengths of each curve</returns>
        private float[] CalculateCurvesDistance()
        {
            float[] lengthRatio = new float[CurveCount];
            for (int i = 0; i < CurveCount; i += 1)
            {
                int index = i * 3;
                lengthRatio[i] = Bezier.GetLength(points[index], points[index + 1], points[index + 2], points[index + 3]);
            }
            return lengthRatio;
        }

        public void RecalculateSplineDistance()
        {
            splineDistance = CalculateSplineDistance();
            curvesDistance = CalculateCurvesDistance();
        }

        #endregion

		#region Spline Misc Utilities Methods
		public float GetNearestPointOnSpline(Vector3 point, int precision = 10)
		{
			float step = 1 / (float)SplineDistance;

			float Res = 0;
			float Ref = float.MaxValue;

			for (int i = 0; i < SplineDistance; i++)
			{
				float t = step*i;
				float L = (GetPointUniform(t)-point).sqrMagnitude;
				if (L < Ref)
				{
					Ref = L;
					Res = t;
				}
			}

			float min = Mathf.Clamp01 (Res - (step / 2));
			float max = Mathf.Clamp01 (Res + (step / 2));

			for (int i = 0; i < precision; i++) {
				float lMin = (GetPointUniform(min)-point).sqrMagnitude;				
				float lMax = (GetPointUniform(max)-point).sqrMagnitude;
				if (lMin > lMax)
					min += (max - min) / 2;
				else
					max -= (max - min) / 2;
			}

			return (min + max) / 2;

		}
		#endregion

        #region Spline Creation Utilities Methods

        /// <summary>
        /// Add a new curve and points at the end of the spline
        /// </summary>
        public void AddCurve()
        {
            Vector3 point = points[points.Length - 1];
            Vector3 dir = (points[points.Length - 1] - points[points.Length - 2]).normalized;
            Array.Resize(ref points, points.Length + 3);
            point += dir * (defaultSpacing / 3);
            points[points.Length - 3] = point;
            point += dir * (defaultSpacing - (defaultSpacing / 3));
            points[points.Length - 2] = point;
            point += dir * defaultSpacing;
            points[points.Length - 1] = point;

            Array.Resize(ref modes, modes.Length + 1);
            modes[modes.Length - 1] = modes[modes.Length - 2];
            EnforceMode(points.Length - 4);

            if (loop)
            {
                points[points.Length - 1] = points[0];
                modes[modes.Length - 1] = modes[0];
                EnforceMode(0);
            }
        }

        /// <summary>
        /// Add a new curve and points and insert it at selected position
        /// </summary>
        public void AddCurve(int selectedPoint)
        {
            Vector3 point = points[selectedPoint];
            Vector3 middlePoint =
                Bezier.GetPoint(points[selectedPoint], points[selectedPoint + 1], points[selectedPoint + 2], points[selectedPoint + 3], 0.5f);

            Vector3 dir = Bezier.GetFirstDerivative(points[selectedPoint], points[selectedPoint + 1], points[selectedPoint + 2], points[selectedPoint + 3], 0.5f).normalized;

            List<Vector3> newPoints = new List<Vector3>();

            point = middlePoint - dir * 0.5f;
            newPoints.Add(point);

            point = middlePoint;
            newPoints.Add(point);

            point = middlePoint + dir * 0.5f;
            newPoints.Add(point);

            List<Vector3> pointsList = new List<Vector3>();
            pointsList.AddRange(points);
            pointsList.InsertRange(selectedPoint + 2, newPoints);
            points = pointsList.ToArray();

            List<BezierControlPointMode> modesList = new List<BezierControlPointMode>();
            modesList.AddRange(modes);
            modesList.Insert(selectedPoint / 3, modes[selectedPoint / 3]);
            modes = modesList.ToArray();

            EnforceMode(selectedPoint + 2);

            if (loop)
            {
                points[points.Length - 1] = points[0];
                modes[modes.Length - 1] = modes[0];
                EnforceMode(0);
            }
        }

        /// <summary>
        /// Remove selected Curve 
        /// </summary>
        /// <param name="selectedPoint"></param>
        public void DeleteCurve(int selectedPoint)
        {
            int index;
            int count;

            if (selectedPoint == 0)
            {
                index = 0;
                count = 3;
            }
            else if (selectedPoint == points.Length - 1)
            {
                index = selectedPoint - 2;
                count = 3;
            }
            else
            {
                index = selectedPoint - 1;
                count = 3;
            }



            List<Vector3> pointsList = new List<Vector3>();
            pointsList.AddRange(points);
            pointsList.RemoveRange(index, count);
            points = pointsList.ToArray();

            List<BezierControlPointMode> modesList = new List<BezierControlPointMode>();
            modesList.AddRange(modes);
            modesList.RemoveAt(selectedPoint / 3);
            modes = modesList.ToArray();

        }

        #endregion
    }
}
